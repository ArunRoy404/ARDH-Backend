using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<AmcPaymentTerm>))]
public enum AmcPaymentTerm
{
    [JsonPropertyName("Bank Transfer")]
    BankTransfer,
    Cheque,
    Cash,
    [JsonPropertyName("UPI / Digital Transfer")]
    UpiDigitalTransfer,
    [JsonPropertyName("Quarterly Advance")]
    QuarterlyAdvance,
    [JsonPropertyName("Monthly Postpaid")]
    MonthlyPostpaid,
    Other
}
