using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<IncomePaymentMethod>))]
public enum IncomePaymentMethod
{
    TransferFromNestaway,
    DirectFromTenant,
    Cash,
    BankTransfer,
    Cheque,
    Others
}
