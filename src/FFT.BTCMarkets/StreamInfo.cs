// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  public sealed record StreamInfo
  {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public string Symbol { get; init; }

    public StreamType Type { get; init; }

    public string Channel { get; init; }

    public string Key => $"{Symbol}_{Channel}";

    public static StreamInfo OrderBookUpdate(string symbol)
    {
      return new StreamInfo
      {
        Symbol = symbol,
        Channel = "orderBookUpdate",
        Type = StreamType.OrderBookUpdate,
      };
    }
  }
}
