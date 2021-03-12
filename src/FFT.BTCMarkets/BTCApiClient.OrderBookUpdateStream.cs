// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  using System;
  using System.Text.Json;
  using System.Threading.Tasks;
  using FFT.BTCMarkets.Serialization;

  public sealed partial class BTCApiClient
  {
    private sealed class OrderBookUpdate : Stream
    {
      private Book? _book = null;

      public OrderBookUpdate(StreamInfo streamInfo)
        : base(streamInfo)
      {
        streamInfo.Type.EnsureEquals(StreamType.OrderBookUpdate, $"{nameof(streamInfo)}.{nameof(streamInfo.Type)}");
      }

      public override ValueTask Handle(ReadOnlyMemory<byte> data)
      {
        var update = JsonSerializer.Deserialize<BookUpdate>(data.Span, SerializationOptions.Instance);
        if (update.Snapshot)
        {
          _book = Book.FromSnapshot(ref update);
        }
        else if (_book is not null)
        {
          _book = _book.Apply(ref update);
        }

        if (_book is not null)
        {
          foreach (var subscriber in _subscriptions)
            subscriber.Handle(_book);
        }

        return default;
      }
    }
  }
}
