using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<MaintenanceStatus>))]
public enum MaintenanceStatus
{
    Open,
    InProgress,
    Complete,
    Cancelled
}
