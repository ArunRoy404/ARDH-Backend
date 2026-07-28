using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IncomePaymentMethod
{
    TransferFromNestaway,
    DirectFromTenant,
    Cash,
    BankTransfer,
    Cheque,
    Others
}
