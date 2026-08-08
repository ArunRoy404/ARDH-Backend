using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<ExpenseStatus>))]
public enum ExpenseStatus
{
    Draft,
    PendingPayment,
    Paid
}
