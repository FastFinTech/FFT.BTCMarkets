// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  using FFT.TimeStamps;

  /// <summary>
  /// Returned by the <see cref="BTCApiClient.GetServerTime"/> method.
  /// </summary>
  public sealed record ServerTime
  {
    /// <summary>
    /// The current server time.
    /// </summary>
    public TimeStamp Timestamp { get; init; }
  }
}
