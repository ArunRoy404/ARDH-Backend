using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<AmcServiceFrequency>))]
public enum AmcServiceFrequency
{
    Monthly,
    Quarterly,
    [JsonPropertyName("Half-Yearly")]
    HalfYearly,
    Yearly,
    [JsonPropertyName("One Time")]
    OneTime
}
