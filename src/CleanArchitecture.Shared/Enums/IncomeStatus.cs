using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IncomeStatus
{
    Paid,
    Pending,
    Overdue,
    Partial
}
