using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MaintenanceCategory
{
    Plumbing,
    Electrical,
    Hvac,
    Painting,
    Carpentry,
    Cleaning,
    [JsonPropertyName("Pest Control")]
    PestControl,
    General,
    [JsonPropertyName("Others type here....")]
    Other
}
