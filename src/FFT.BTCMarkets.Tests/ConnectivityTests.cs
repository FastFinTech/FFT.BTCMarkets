// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets.Tests
{
  using System.Buffers;
  using System.Collections.Generic;
  using System.Diagnostics;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  using System.Linq;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Net.WebSockets;
  using System.Text;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using FFT.BTCMarkets.Serialization;
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using static System.Math;
  using static System.MidpointRounding;
  //using static FFT.BTCMarkets.Tests.Services;

  [TestClass]
  public class ConnectivityTests
  {
    //[TestMethod]
    //public async Task Connectivity()
    //{
    //  await Client.TestConnectivity();
    //  var time = await Client.GetServerTime();
    //  var orderBooks = await Client.GetTopOrderBooks();
    //  var btcOrderBooks = orderBooks.Where(b => b.Symbol.StartsWith("BTC")).ToList();
    //  var btcUSDOrderBook = orderBooks.Single(b => b.Symbol == "BTCUSDT");
    //  var btcBidDollars = Round(btcUSDOrderBook.BidQty * btcUSDOrderBook.BidPrice, 2, AwayFromZero);
    //  var btcAskDollars = Round(btcUSDOrderBook.AskQty * btcUSDOrderBook.AskPrice, 2, AwayFromZero);
    //}

    //[TestMethod]
    //public async Task DepthStream()
    //{
    //  var count = 0;
    //  using var cts = new CancellationTokenSource(10000);
    //  var subscription = await Client.Subscribe(StreamInfo.FullDepth("BTCUSDT", false));
    //  await foreach (var book in subscription.Reader.ReadAllAsync(cts.Token))
    //  {
    //    if (++count == 2)
    //      return;
    //  }
    //}

    [TestMethod]
    public async Task SimpleTest()
    {
      using var client = new HttpClient(new SocketsHttpHandler())
      {
        BaseAddress = new("https://api.btcmarkets.net/v3/"),
      };
      using var request = new HttpRequestMessage(HttpMethod.Get, "markets");
      using var response = await client.SendAsync(request);
      var activeMarkets = (await response.Content.ReadFromJsonAsync<List<ActiveMarket>>(SerializationOptions.Instance))!;
      var btcMarkets = activeMarkets.Where(m => m.BaseAssetName == "BTC").ToList();
      // BTC-AUD

      var buffer = new ArrayBufferWriter<byte>(1024 * 1024);
      using var ws = new ClientWebSocket();
      await ws.ConnectAsync(new("wss://socket.btcmarkets.net/v2"), default);
      await Send(ws, new
      {
        marketIds = new[] { "ETH-AUD" },
        channels = new[] { "orderbookUpdate" },
        messageType = "subscribe",
        clientType = "api",
      });
      await Send(ws, new
      {
        marketIds = new[] { "BTC-AUD" },
        channels = new[] { "orderbookUpdate" },
        messageType = "addSubscription",
        clientType = "api",
      });

      var symbolMessageCount = new Dictionary<string, int>();
      var books = new Dictionary<string, Book>();
      for (var i = 0; i < 100; i++)
      {
        await ReadMessage(ws, buffer);
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        var messageType = JsonSerializer.Deserialize<MessageTypeDTO>(buffer.WrittenSpan, SerializationOptions.Instance);
        var streamKey = $"{messageType.MarketId}_{messageType.MessageType}";
        if (messageType.MessageType == "orderbookUpdate")
        {
          var update = JsonSerializer.Deserialize<BookUpdate>(buffer.WrittenSpan, SerializationOptions.Instance);
          if (update.Snapshot)
          {
            books[streamKey] = Book.FromSnapshot(ref update);
          }
          else if (books.TryGetValue(streamKey, out var book))
          {
            books[streamKey] = book.Apply(ref update);
          }
        }
        else
        {
          Debugger.Break();
        }
      }

      Debugger.Break();
    }

    private static Task Send(ClientWebSocket ws, object value)
      => ws.SendAsync(ToUtf8Bytes(value), WebSocketMessageType.Text, true, default);

    private static byte[] ToUtf8Bytes(object value)
      => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));

    static async Task ReadMessage(ClientWebSocket ws, ArrayBufferWriter<byte> buffer)
    {
      buffer.Clear();
      var result = await ws.ReceiveAsync(buffer.GetMemory(buffer.Capacity), default);
      buffer.Advance(result.Count);
      while (!result.EndOfMessage)
      {
        if (buffer.FreeCapacity > 1024)
        {
          result = await ws.ReceiveAsync(buffer.GetMemory(buffer.FreeCapacity), default);
          buffer.Advance(result.Count);
        }
        else
        {
          result = await ws.ReceiveAsync(buffer.GetMemory(1024 * 1024), default);
          buffer.Advance(result.Count);
        }
      }
    }
  }

  internal struct MessageTypeDTO
  {
    public string MarketId { get; set; }
    public string MessageType { get; set; }
  }
}
