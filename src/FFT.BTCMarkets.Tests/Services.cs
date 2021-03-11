// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets.Tests
{
  using System;

  internal static class Services
  {
    static Services()
    {
      BTCApiClientOptions = new BTCApiClientOptions
      {
        ApiKey = Environment.GetEnvironmentVariable("BTC_ApiKey")!,
        SecretKey = Environment.GetEnvironmentVariable("BTC_ApiSecret")!,
      };

      Client = new BTCApiClient(BTCApiClientOptions);
    }

    public static BTCApiClientOptions BTCApiClientOptions { get; }

    public static BTCApiClient Client { get; }
  }
}
