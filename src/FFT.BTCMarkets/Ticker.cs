// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  using System.Diagnostics;
  using FFT.TimeStamps;

  [DebuggerDisplay("{MarketId: {BestBid} / {BestAsk}")]
  public sealed record Ticker
  {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Market id is used across the system.
    /// </summary>
    public string MarketId { get; init; }

    /// <summary>
    /// Best buy order price.
    /// </summary>
    public decimal BestBid { get; init; }

    /// <summary>
    /// Best sell order price.
    /// </summary>
    public decimal BestAsk { get; init; }

    /// <summary>
    /// Price of the last trade.
    /// </summary>
    public decimal LastPrice { get; init; }

    /// <summary>
    /// Represents total trading volume over the past 24 hours for the the given
    /// market.
    /// </summary>
    public decimal Volume24 { get; init; }

    /// <summary>
    /// Price change (difference between the first and last price over 24
    /// hours).
    /// </summary>
    public decimal Price24 { get; init; }

    /// <summary>
    /// Lowest price over the past 24 hours.
    /// </summary>
    public decimal Low24 { get; init; }

    /// <summary>
    /// Highest price over the past 24 hours.
    /// </summary>
    public decimal High24 { get; init; }

    /// <summary>
    /// The time the ticker record was emitted.
    /// </summary>
    public TimeStamp Timestamp { get; init; }
  }
}
