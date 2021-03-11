// Copyright (c) True Goodwill. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FFT.BTCMarkets
{
  using System;
  using System.Globalization;
  using System.Text.Json;
  using System.Text.Json.Serialization;

  [JsonConverter(typeof(Converter))]
  public readonly struct BookItem
  {
    public decimal Price { get; init; }

    public decimal Qty { get; init; }

    public int Count { get; init; }

    public class Converter : JsonConverter<BookItem>
    {
      public override BookItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
        reader.Read();

        if (reader.TokenType != JsonTokenType.String) throw new JsonException();
        var price = decimal.Parse(reader.GetString()!, NumberStyles.Any, CultureInfo.InvariantCulture);
        reader.Read();

        if (reader.TokenType != JsonTokenType.String) throw new JsonException();
        var qty = decimal.Parse(reader.GetString()!, NumberStyles.Any, CultureInfo.InvariantCulture);
        reader.Read();

        if (reader.TokenType != JsonTokenType.Number) throw new JsonException();
        var count = reader.GetInt32();
        reader.Read();

        if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException();
        return new BookItem
        {
          Price = price,
          Qty = qty,
          Count = count,
        };
      }

      public override void Write(Utf8JsonWriter writer, BookItem value, JsonSerializerOptions options)
        => throw new NotImplementedException();
    }
  }
}
