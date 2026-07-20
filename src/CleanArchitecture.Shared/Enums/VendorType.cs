using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VendorType
{
    Plumbing,
    Electrical,
    [JsonPropertyName("HVAC / AC")]
    HvacAc,
    Painting,
    Cleaning,
    [JsonPropertyName("Pest Control")]
    PestControl,
    Security,
    Landscaping,
    [JsonPropertyName("Lift / Elevator")]
    LiftElevator,
    [JsonPropertyName("CCTV / Surveillance")]
    CctvSurveillance,
    [JsonPropertyName("Generator / Power")]
    GeneratorPower,
    [JsonPropertyName("Fire Safety")]
    FireSafety,
    [JsonPropertyName("Water System")]
    WaterSystem,
    [JsonPropertyName("Civil / Construction")]
    CivilConstruction,
    General,
    [JsonPropertyName("Others type here....")]
    Other
}
