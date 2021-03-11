// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  public partial class BTCApiClient
  {
    private struct MessageTypeDTO
    {
      public string MarketId { get; set; }
      public string MessageType { get; set; }
    }
  }
}
