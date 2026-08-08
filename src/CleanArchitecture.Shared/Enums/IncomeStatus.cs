using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<IncomeStatus>))]
public enum IncomeStatus
{
    Paid,
    Pending,
    Overdue,
    Partial
}
