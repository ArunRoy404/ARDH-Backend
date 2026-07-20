using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApartmentType
{
    [JsonPropertyName("1BHK")]
    OneBHK,

    [JsonPropertyName("2BHK")]
    TwoBHK,

    [JsonPropertyName("3BHK")]
    ThreeBHK,

    [JsonPropertyName("4BHK")]
    FourBHK,

    Penthouse,

    Studio
}
