// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets.Serialization
{
  using System;
  using System.Text.Json;
  using System.Text.Json.Serialization;
  using FFT.TimeStamps;
  using static System.Globalization.CultureInfo;

  internal sealed class TimeStampConverter : JsonConverter<TimeStamp>
  {
    public override TimeStamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      // 2020-01-08T19:47:13.986Z
      var utc = DateTime.ParseExact(reader.GetString()!, "yyyy-MM-ddTHH:mm:ss.fffZ", InvariantCulture);
      return new TimeStamp(utc.Ticks);
    }

    public override void Write(Utf8JsonWriter writer, TimeStamp value, JsonSerializerOptions options)
      => writer.WriteNumberValue(value.ToUnixMillieconds());
  }
}
