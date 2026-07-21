using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExpenseStatus
{
    Draft,
    PendingPayment,
    Paid
}
