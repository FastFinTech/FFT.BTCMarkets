// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  using System;
  using System.Buffers;
  using System.Collections.Generic;
  using System.Collections.Immutable;
  using System.Globalization;
  using System.Linq;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Net.WebSockets;
  using System.Security.Cryptography;
  using System.Text;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using FFT.BTCMarkets.Serialization;
  using FFT.Disposables;
  using Nito.AsyncEx;

  /// <summary>
  /// Provides access to Binance market data. This is a single-use object. It
  /// disposes itself when the connection drops.
  /// </summary>
  public sealed partial class BTCApiClient : AsyncDisposeBase, IAsyncDisposable
  {
    /// <summary>
    /// Used for making rest api requests.
    /// </summary>
    private readonly HttpClient _client;

    /// <summary>
    /// Used to track completion of the Work method. Disposal triggers
    /// completion of the method via cancelling a cancellation token, then waits
    /// for the task to be completed.
    /// </summary>
    private readonly Task _workTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="BTCApiClient"/>
    /// class and triggers immediate connection to the streaming websocket.
    /// </summary>
    /// <param name="options">Configures the client, particularly for
    /// authentication. Can be left <c>null</c> if you are only accessing
    /// functions that do not require authentication.</param>
    public BTCApiClient(BTCApiClientOptions? options)
    {
      // This allows connection-reuse for multiple rest api calls. It requires
      // targeting net5 as discussed here:
      // https://github.com/dotnet/runtime/issues/24613
      _client = new(new SocketsHttpHandler());
      _client.BaseAddress = new("https://api.btcmarkets.net/v3/");
      _client.DefaultRequestHeaders.Add("Accept", "application /json");
      _client.DefaultRequestHeaders.Add("Accept-Charset", "UTF-8");

      if (options is not null)
      {
        options.ApiKey.EnsureNotNullOrWhiteSpace($"{nameof(options)}.{nameof(options.ApiKey)}");
        options.SecretKey.EnsureNotNullOrWhiteSpace($"{nameof(options)}.{nameof(options.SecretKey)}");
        Options = options;
        _client.DefaultRequestHeaders.Add("BM-AUTH-APIKEY", Options.ApiKey);
      }

      _workTask = Task.Run(Work);
    }

    /// <inheritdoc/>
    protected override ValueTask CustomDisposeAsync()
    {
      _client.Dispose();
      return new(_workTask);
    }
  }

  // Utility stuff
  public partial class BTCApiClient
  {
    /// <summary>
    /// Configures this <see cref="BTCApiClient"/> instance. May be
    /// <c>null</c> if this instance is not intended for use with functions that
    /// require authentication.
    /// </summary>
    public BTCApiClientOptions? Options { get; }

    private async Task<T> ParseResponse<T>(HttpResponseMessage response)
    {
      await RequestFailedException.ThrowIfNecessary(response);
      return (await response.Content.ReadFromJsonAsync<T>(SerializationOptions.Instance, DisposedToken))!;
    }

    private async Task AddSignature(HttpRequestMessage request)
    {
      // https://github.com/BTCMarkets/api-v3-client-dotnet/blob/master/BtcMarketsApiClient/BtcMarketsApiClient.Sample/ApiClient.cs
      long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
      var method = request.Method.ToString();
      var path = new UriBuilder(request.RequestUri!).Path;
      // TODO: Have not tested if we're able to read the content from here
      // before sending the request without messing up the request.
      var data = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
      var bytes = Encoding.UTF8.GetBytes(method + path + now + data);
      using var hash = new HMACSHA512(Convert.FromBase64String(Options!.SecretKey));
      var hashedInputBytes = hash.ComputeHash(bytes);
      var signature = Convert.ToBase64String(hashedInputBytes);
      request.Headers.Add("BM-AUTH-TIMESTAMP", now.ToString());
      request.Headers.Add("BM-AUTH-SIGNATURE", signature);
    }
  }

  // Market data (rest api)
  public partial class BTCApiClient
  {
    /// <summary>
    /// Gets the current time on the BTC Markets api server.
    /// </summary>
    public async Task<ServerTime> GetServerTime()
    {
      using var request = new HttpRequestMessage(HttpMethod.Get, "time");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<ServerTime>(response);
    }

    /// <summary>
    /// Retrieves list of active markets including configuration for each
    /// market.
    /// </summary>
    public async Task<List<ActiveMarket>> GetActiveMarkets()
    {
      using var request = new HttpRequestMessage(HttpMethod.Get, "markets");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<List<ActiveMarket>>(response);
    }

    /// <summary>
    /// Retrieves ticker for the given marketId.
    /// </summary>
    public async Task<Ticker> GetTicker(string marketId)
    {
      marketId.EnsureNotNullOrWhiteSpace(nameof(marketId));
      using var request = new HttpRequestMessage(HttpMethod.Get, $"markets/{marketId}/ticker");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<Ticker>(response);
    }

    /// <summary>
    /// Retrieves tickers for the given marketIds.
    /// </summary>
    public Task<List<Ticker>> GetTickers(params string[] marketIds)
      => GetTickers((IEnumerable<string>)marketIds);

    /// <summary>
    /// Retrieves tickers for the given marketIds.
    /// </summary>
    public async Task<List<Ticker>> GetTickers(IEnumerable<string> marketIds)
    {
      marketIds
        .EnsureNotNull(nameof(marketIds))
        //.EnsureHasValues(nameof(marketIds))
        .EnsureNoWhitespaceValues(nameof(marketIds));

      var url = $"markets/tickers?marketId=" + string.Join("&marketId=", marketIds);
      using var request = new HttpRequestMessage(HttpMethod.Get, url);
      using var response = await _client.SendAsync(request);
      return await ParseResponse<List<Ticker>>(response);
    }

    public async Task<Book> GetOrderBook(string marketId)
    {
      marketId.EnsureNotNullOrWhiteSpace(nameof(marketId));
      using var request = new HttpRequestMessage(HttpMethod.Get, $"markets/{marketId}/orderbook");
      using var response = await _client.SendAsync(request);
      return await ParseResponse<Book>(response);
    }
  }

  // Streams
  public partial class BTCApiClient
  {
    /// <summary>
    /// Signals the addition or removal of subscriptions.
    /// </summary>
    private readonly AsyncAutoResetEvent _signalEvent = new(false);

    /// <summary>
    /// Used to marshal new subscription requests into a thread-safe context.
    /// Null when disposal has begun.
    /// </summary>
    private ImmutableList<Subscription>? _newSubscriptions = ImmutableList<Subscription>.Empty;

    /// <summary>
    /// Used to marshal subscription cancellations into a thread-safe context.
    /// Null when disposal has begun.
    /// </summary>
    private ImmutableList<Subscription>? _cancelSubscriptions = ImmutableList<Subscription>.Empty;

    /// <summary>
    /// Gets a subscription to the given <paramref name="streamInfo"/>.
    /// </summary>
    public async ValueTask<ISubscription> Subscribe(StreamInfo streamInfo)
    {
      var subscription = new Subscription(this, streamInfo);
      while (true)
      {
        var original = Interlocked.CompareExchange(ref _newSubscriptions, null, null);
        if (original is null)
        {
          // We have been disposed. Disposing the subscription before we return
          // it will "complete" the message channel within it, signalling to the
          // user code that the subscription will not yield data and a new
          // subscription should be requested (from a new
          // BinanceMarketDataClient)
          await subscription.DisposeAsync();
          return subscription;
        }
        else
        {
          var @new = original.Add(subscription);
          var result = Interlocked.CompareExchange(ref _newSubscriptions, @new, original);
          if (ReferenceEquals(original, result))
          {
            // Signal the presence of a new subscription to the Work method.
            _signalEvent.Set();
            return subscription;
          }
        }

        // Threadrace. We lost. Start again.
      }
    }

    /// <summary>
    /// Called by the Subscription object when it is disposed.
    /// </summary>
    private void Remove(Subscription subscription)
    {
      while (true)
      {
        var original = Interlocked.CompareExchange(ref _cancelSubscriptions, null, null);
        if (original is null)
        {
          // We have been disposed. There's nothing to do so just return.
          // Execution reaches here This happens when, during disposal, at the
          // end of the "Work" method, we dispose all the subscription objects.
          return;
        }

        // If execution reaches here, the subscription was disposed by user code
        // that no longer wants to receive subscription data.
        var @new = original.Add(subscription);
        if (ReferenceEquals(@new, original)) return;
        var result = Interlocked.CompareExchange(ref _cancelSubscriptions, @new, original);
        if (ReferenceEquals(original, result))
        {
          // Signal the presence of a subscription cancellation to the Work method.
          _signalEvent.Set();
          return;
        }

        // Threadrace. We lost. Start again.
      }
    }

    private async Task Work()
    {
      // Setup storage for parsing incoming messages
      const int BUFFER_SIZE = 1024 * 1024;
      var buffer = new ArrayBufferWriter<byte>(BUFFER_SIZE);

      // Some helper variables
      var streams = new Dictionary<string, Stream>();

      // The actual websocket
      using var ws = new ClientWebSocket();

      try
      {
        await ws.ConnectAsync(new Uri("wss://socket.btcmarkets.net/v2"), DisposedToken);
        var readTask = ReadMessage(ws, buffer, DisposedToken);

        while (true)
        {
          // Insert all new subscriptions.
          var newSubscriptions = Interlocked.Exchange(ref _newSubscriptions, ImmutableList<Subscription>.Empty)!;
          foreach (var subscription in newSubscriptions)
          {
            if (!streams.TryGetValue(subscription.StreamInfo.Key, out var stream))
            {
              stream = subscription.StreamInfo.Type switch
              {
                StreamType.OrderBookUpdate => new OrderBookUpdate(subscription.StreamInfo),
                _ => throw new NotImplementedException(),
              };
              await stream.Initiate(this);
              streams[subscription.StreamInfo.Key] = stream;
              var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
              {
                marketIds = new[] { subscription.StreamInfo.Symbol },
                channels = new[] { subscription.StreamInfo.Channel },
                messageType = streams.Count == 1 ? "subscribe" : "addSubscription",
                clientType = "api",
              }));
              await ws.SendAsync(messageBytes, WebSocketMessageType.Text, true, DisposedToken);
            }

            await stream.Add(subscription);
          }

          // Remove all canceled subscriptions
          var canceledSubscriptions = Interlocked.Exchange(ref _cancelSubscriptions, ImmutableList<Subscription>.Empty)!;
          foreach (var subscription in canceledSubscriptions)
          {
            // NB: There's no need to dispose the "subscription" object. It's
            // already disposed -- that's how execution reaches here.
            if (streams.TryGetValue(subscription.StreamInfo.Key, out var stream))
            {
              await stream.Remove(subscription);
              if (stream.SubscriptionCount == 0)
              {
                streams.Remove(subscription.StreamInfo.Key);
                var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                {
                  marketIds = new[] { subscription.StreamInfo.Symbol },
                  channels = new[] { subscription.StreamInfo.Channel },
                  messageType = streams.Count == 0 ? "unsubscribe" : "removeSubscription",
                  clientType = "api",
                }));
                await ws.SendAsync(messageBytes, WebSocketMessageType.Text, true, DisposedToken);
              }
            }
          }

          // Now go ahead and receive messages until we are either a) disposed,
          // or b) receive a signal that subscriptions have been added or
          // canceled.

          var signalTask = _signalEvent.WaitAsync(DisposedToken);
          var completedTask = await Task.WhenAny(signalTask, readTask);
          while (completedTask == readTask)
          {
            await readTask;

            // streamName will turn out null when we receive a control message
            // that is not part of an actual subscription.
#if DEBUG
            var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
#endif
            var messageType = JsonSerializer.Deserialize<MessageTypeDTO>(buffer.WrittenSpan, SerializationOptions.Instance);
            var streamKey = $"{messageType.MarketId}_{messageType.MessageType}";
            if (!string.IsNullOrWhiteSpace(streamKey))
            {
              // We can still have messages in the receive buffer that belong
              // to streams we unsubscribed from.
              if (streams.TryGetValue(streamKey, out var stream))
                await stream.Handle(buffer.WrittenMemory);
            }

            readTask = ReadMessage(ws, buffer, DisposedToken);
            completedTask = await Task.WhenAny(signalTask, readTask);
          }

          await signalTask;
        }
      }
      catch (Exception x)
      {
        // Since disposal actually waits for this method to complete, we need to
        // kickoff disposal in a background task.
        _ = DisposeAsync(x);
      }
      finally
      {
        // Cleanup our operations.

        // Important to null this so that new subscription requests are rejected
        // by returning disposed and completed subscription objects.
        var newSubscriptions = Interlocked.Exchange(ref _newSubscriptions, null)!;

        // For each new subscription waiting in the queue, we dispose them to
        // signal they won't be getting any more data.
        foreach (var subscription in newSubscriptions)
          await subscription.DisposeAsync();

        // Important to null this before disposing the subscriptions below
        var removedSubscriptions = Interlocked.Exchange(ref _cancelSubscriptions, null)!;

        // Disposing all the current subscription objects will cause them to
        // send "completed" signal to the user code via the channel
        // writer/reader. It will also cause the subscriptions to make calls
        // into the "RemoveSubscription" method above, but that won't do
        // anything "bad" because we have already nulled the
        // "removedSubscriptions" list above.
        foreach (var kv in streams)
          await kv.Value.DisposeAsync();
      }

      static async Task ReadMessage(ClientWebSocket ws, ArrayBufferWriter<byte> buffer, CancellationToken cancellationToken)
      {
        buffer.Clear();

        var result = await ws.ReceiveAsync(buffer.GetMemory(BUFFER_SIZE), cancellationToken);
        buffer.Advance(result.Count);

        while (!result.EndOfMessage)
        {
          result = await ws.ReceiveAsync(buffer.GetMemory(BUFFER_SIZE), cancellationToken);
          buffer.Advance(result.Count);
        }
      }
    }
  }
}
