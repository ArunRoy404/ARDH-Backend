using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EquipmentStatus
{
    Operational,
    [JsonPropertyName("Optional")]
    Optional,
    [JsonPropertyName("Under Maintenance")]
    UnderMaintenance,
    [JsonPropertyName("Out of Order")]
    OutOfOrder,
    Decommissioned
}
