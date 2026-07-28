using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
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
