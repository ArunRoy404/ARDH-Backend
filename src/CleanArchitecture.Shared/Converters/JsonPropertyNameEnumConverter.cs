using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Converters;

/// <summary>
/// Enum JSON converter used by the AMC contract enums.
/// On read it accepts, case-insensitively:
///   1. the raw enum member names (e.g. "QuarterlyAdvance", "NonComprehensive", "HalfYearly"), and
///   2. the friendly display values declared via [JsonPropertyName] (e.g. "Quarterly Advance", "Non-Comprehensive", "Half-Yearly"), and
///   3. integer values that are within the enum's defined range.
/// On write it always emits the raw enum member name, so the API response
/// format is unchanged from the previous JsonStringEnumConverter behaviour.
/// When a value cannot be converted, it throws a readable JsonException that lists
/// the exact valid values instead of the default "JSON value could not be converted" text.
/// </summary>
public sealed class JsonPropertyNameEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    private static readonly Dictionary<string, TEnum> Lookup = BuildLookup();
    private static readonly string ValidValues = string.Join(", ", Enum.GetNames<TEnum>());
    private static readonly string FriendlyFieldName = GetFriendlyFieldName();

    private static Dictionary<string, TEnum> BuildLookup()
    {
        var map = new Dictionary<string, TEnum>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in Enum.GetValues<TEnum>())
        {
            map[value.ToString()] = value;

            var jsonName = typeof(TEnum)
                .GetField(value.ToString())
                ?.GetCustomAttribute<JsonPropertyNameAttribute>()
                ?.Name;

            if (!string.IsNullOrWhiteSpace(jsonName))
            {
                map[jsonName] = value;
            }
        }
        return map;
    }

    private static string GetFriendlyFieldName()
        => typeof(TEnum).Name switch
        {
            nameof(AmcPaymentTerm) => "paymentTerms",
            nameof(AmcContractType) => "contractType",
            nameof(AmcServiceFrequency) => "serviceFrequency",
            nameof(AmcStatus) => "status",
            nameof(MaintenanceRecurrenceFrequency) => "recurrenceFrequency",
            nameof(MaintenanceStatus) => "status",
            nameof(MaintenancePriority) => "priority",
            nameof(IncomeEntity) => "incomeEntity",
            nameof(IncomeType) => "incomeType",
            nameof(IncomePaymentMethod) => "paymentMethod",
            nameof(IncomeStatus) => "status",
            nameof(ExpenseNature) => "nature",
            nameof(ExpenseEntity) => "entity",
            nameof(ExpenseStatus) => "status",
            nameof(ExpenseCategory) => "category",
            _ => typeof(TEnum).Name
        };

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString()?.Trim();
            if (!string.IsNullOrEmpty(text) && Lookup.TryGetValue(text, out var parsed))
            {
                return parsed;
            }

            throw new JsonException($"The value '{text}' is not valid for {FriendlyFieldName}. Valid values: {ValidValues}.");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out var number))
            {
                if (number >= 0 && number <= int.MaxValue && Enum.IsDefined(typeof(TEnum), (int)number))
                {
                    return (TEnum)(object)(int)number;
                }

                throw new JsonException($"The value '{number}' is not valid for {FriendlyFieldName}. Valid values: {ValidValues}.");
            }

            throw new JsonException($"The value is not a valid integer for {FriendlyFieldName}. Valid values: {ValidValues}.");
        }

        throw new JsonException($"A value for {FriendlyFieldName} is required. Valid values: {ValidValues}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
