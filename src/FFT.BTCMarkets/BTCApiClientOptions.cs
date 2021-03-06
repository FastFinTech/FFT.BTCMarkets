// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  /// <summary>
  /// Configures the <see cref="BTCApiClient"/>.
  /// </summary>
  public sealed record BTCApiClientOptions
  {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Used for authenticating requests that require security of <see
    /// cref="EndpointSecurityType.USER_STREAM"/> or <see
    /// cref="EndpointSecurityType.MARKET_DATA"/>.
    /// </summary>
    public string ApiKey { get; init; }

    /// <summary>
    /// Used in addition to <see cref="ApiKey"/> for signing requests that
    /// require security of <see cref="EndpointSecurityType.TRADE"/> or <see
    /// cref="EndpointSecurityType.USER_DATA"/>.
    /// </summary>
    public string SecretKey { get; init; }
  }
}
