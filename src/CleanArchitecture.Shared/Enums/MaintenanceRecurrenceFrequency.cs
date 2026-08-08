using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<MaintenanceRecurrenceFrequency>))]
public enum MaintenanceRecurrenceFrequency
{
    Daily,
    Weekly,
    [JsonPropertyName("Bi-Weekly")]
    BiWeekly,
    Monthly,
    [JsonPropertyName("Bi-Monthly")]
    BiMonthly,
    Quarterly,
    [JsonPropertyName("Half Yearly")]
    HalfYearly,
    Yearly,
    [JsonPropertyName("Bi-Yearly")]
    BiYearly
}
