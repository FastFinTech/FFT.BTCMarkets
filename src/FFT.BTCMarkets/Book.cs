// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  using System;
  using System.Collections.Generic;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  using System.Collections.Immutable;
  using System.Linq;
  using System.Runtime.InteropServices;
  using FFT.TimeStamps;

  public sealed record Book
  {
    public string MarketId { get; init; }

    public long SnapshotId { get; init; }

    public TimeStamp Timestamp { get; init; }

    public ImmutableDictionary<decimal, decimal> Bids { get; init; }

    public ImmutableDictionary<decimal, decimal> Asks { get; init; }

    public static Book FromSnapshot(ref BookUpdate update)
    {
      if (!update.Snapshot) throw new Exception("not a snapshot");
      return new Book
      {
        MarketId = update.MarketId,
        SnapshotId = update.SnapshotId,
        Timestamp = update.Timestamp,
        Bids = update.Bids.ToImmutableDictionary(b => b.Price, b => b.Qty),
        Asks = update.Asks.ToImmutableDictionary(b => b.Price, b => b.Qty),
      };
    }

    public Book Apply(ref BookUpdate update)
    {
      return this with
      {
        Timestamp = update.Timestamp,
        Bids = Bids
          .RemoveRange(update.Bids.Where(b => b.Qty == 0).Select(b => b.Price))
          .SetItems(update.Bids.Where(b => b.Qty > 0).Select(b => new KeyValuePair<decimal, decimal>(b.Price, b.Qty))),
        Asks = Asks
        .RemoveRange(update.Bids.Where(b => b.Qty == 0).Select(b => b.Price))
        .SetItems(update.Bids.Where(b => b.Qty > 0).Select(b => new KeyValuePair<decimal, decimal>(b.Price, b.Qty))),
      };
    }
  }
}
