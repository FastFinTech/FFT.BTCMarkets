// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  using System.Diagnostics;

  [DebuggerDisplay("{MarketId}")]
  public sealed record ActiveMarket
  {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Market id is used across the system.
    /// </summary>
    public string MarketId { get; init; }

    /// <summary>
    /// The asset being purchased or sold. In the case of ETH-AUD the base asset
    /// is ETH.
    /// </summary>
    public string BaseAssetName { get; init; }

    /// <summary>
    /// The asset that is used to price the base asset. In the case of ETH_AUD
    /// quote asset is AUD.
    /// </summary>
    public string QuoteAssetName { get; init; }

    /// <summary>
    /// Minimum amount for an order.
    /// </summary>
    public decimal MinOrderAmount { get; init; }

    /// <summary>
    /// Maximum amount for an order.
    /// </summary>
    public decimal MaxOrderAmount { get; init; }

    /// <summary>
    /// Maximum number of decimal places can be used for amounts.
    /// </summary>
    public int AmountDecimals { get; init; }

    /// <summary>
    /// Represents number of decimal places can be used for price when placing
    /// orders. For instance for BTC-AUD market priceDecimals is 2 meaning that
    /// price of 100.12 is valid but 100.123 is not.
    /// </summary>
    public int PriceDecimals { get; init; }
  }
}
