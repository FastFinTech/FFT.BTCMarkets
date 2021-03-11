//// Copyright (c) True Goodwill. All rights reserved.
//// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//namespace FFT.BTCMarkets
//{
//  using System;
//  using System.Text.Json;
//  using System.Threading.Tasks;
//  using FFT.BTCMarkets.Serialization;

//  public sealed partial class BTCApiClient
//  {
//    private sealed class BookTickerStream : Stream
//    {
//      public BookTickerStream(StreamInfo streamInfo)
//        : base(streamInfo)
//      {
//      }

//      public override ValueTask Handle(ReadOnlyMemory<byte> data)
//      {
//        var bookTicker = JsonSerializer.Deserialize<MessageEnvelope<BookTicker>>(data.Span, SerializationOptions.Instance).Data!;
//        foreach (var subscriber in _subscriptions)
//          subscriber.Handle(bookTicker);
//        return default;
//      }
//    }
//  }
//}
