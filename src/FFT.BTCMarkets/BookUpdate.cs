// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  using System.Collections.Generic;
  using FFT.TimeStamps;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable SA1507 // Code should not contain multiple blank lines in a row
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

  public readonly struct BookUpdate
  {
    public string MessageType { get; init; } // always "orderbookUpdate"

    public TimeStamp Timestamp { get; init; }

    public string MarketId { get; init; }

    public bool Snapshot { get; init; }

    public long SnapshotId { get; init; }

    public List<BookItem> Bids { get; init; }

    public List<BookItem> Asks { get; init; }
  }
}
