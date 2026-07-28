using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EquipmentType
{
    Lift,
    [JsonPropertyName("CCTV")]
    Cctv,
    Inverter,
    Pump,
    Generator,
    [JsonPropertyName("Fire Safety")]
    FireSafety,
    [JsonPropertyName("Water System")]
    WaterSystem,
    [JsonPropertyName("Others type here....")]
    Other
}
