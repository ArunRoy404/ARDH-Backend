using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<MaintenanceStatus>))]
public enum MaintenanceStatus
{
    Open,

    /// <summary>
    /// A recurring maintenance request whose scheduled cycle has come due. Set automatically by
    /// the reminder background job (never by a user) and drives the daily reminder digest until
    /// the request is moved to InProgress/Complete/Cancelled.
    /// </summary>
    Pending,
    InProgress,
    Complete,
    Cancelled
}
