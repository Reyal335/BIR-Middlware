// Middleware.Core/Eis/EisJson.cs
//
// The single, fixed set of rules for turning an EisInvoicePayload into JSON.
// Every invoice is serialized with these options, so the same invoice always
// produces the same bytes - and those bytes are what the JWS signs.
//
// Rules:
//   - Property names are written exactly as declared on the payload classes.
//     Use [JsonPropertyName] on EisInvoicePayload if a C# name differs from the spec.
//   - Properties are written in declaration order. Use [JsonPropertyOrder] if the
//     spec ever requires a specific order.
//   - No whitespace or indentation.
//   - Nulls are written out, never omitted (BIR's null convention: strings take
//     null/blank, number fields take 0.00 - so payload number fields should be
//     plain decimal, not decimal?).
//   - Non-ASCII text (e.g. "Peñaranda") and characters like & are written as-is,
//     not as \u escapes. Safe here because the JSON never goes into HTML.
//   - Amounts are written with exactly two decimal places. An amount that isn't
//     already rounded to 2 places is an error (the TaxEngine should have rounded
//     it), so serialization fails loudly instead of silently re-rounding.
//   - Dates are NOT handled here: the payload carries them as preformatted
//     strings (e.g. IssueDtm "20261008"), built by EisMapper.

using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Middleware.Core.Eis;

public static class EisJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Serializes an EIS payload to a JSON string using the fixed EIS rules.</summary>
    public static string Serialize<T>(T payload) =>
        JsonSerializer.Serialize(payload, Options);

    /// <summary>Serializes an EIS payload straight to UTF-8 bytes (the form the JWS payload is built from).</summary>
    public static byte[] SerializeToUtf8Bytes<T>(T payload) =>
        JsonSerializer.SerializeToUtf8Bytes(payload, Options);

    /// <summary>Reads EIS JSON (e.g. a decrypted receipt) using the same rules.</summary>
    public static T? Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,                          // keep names exactly as declared
            WriteIndented = false,                                // no whitespace
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,   // always write nulls
            NumberHandling = JsonNumberHandling.Strict,           // numbers as numbers, never strings
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = false,                  // exact names when reading too
        };

        // Applies to decimal and decimal? everywhere. A property can override it with
        // its own [JsonConverter(...)] attribute, e.g. if the spec allows Qty to carry
        // more than two decimal places.
        options.Converters.Add(new EisAmountConverter());

        // Lock the options so nothing can change the rules at runtime.
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}

/// <summary>
/// Writes decimals with exactly two decimal places (30 → 30.00, 13.4 → 13.40)
/// and rejects values with more precision than that.
/// </summary>
public sealed class EisAmountConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDecimal();

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        if (decimal.Round(value, 2) != value)
            throw new JsonException(
                $"Amount {value.ToString(CultureInfo.InvariantCulture)} has more than two decimal places. " +
                "Round it in the TaxEngine before mapping to the EIS payload.");

        if (value == 0m) value = 0m; // normalise negative zero so it never prints as -0.00

        writer.WriteRawValue(value.ToString("0.00", CultureInfo.InvariantCulture), skipInputValidation: true);
    }
}