using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<IncomeType>))]
public enum IncomeType
{
    Rent,
    Maintenance,
    SecurityDeposit,
    WaterCharge,
    Others
}
