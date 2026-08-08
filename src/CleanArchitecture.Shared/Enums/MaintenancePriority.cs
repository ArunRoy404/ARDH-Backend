using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<MaintenancePriority>))]
public enum MaintenancePriority
{
    Low,
    Medium,
    High,
    Critical
}
