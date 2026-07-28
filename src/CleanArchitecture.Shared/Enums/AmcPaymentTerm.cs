using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
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
