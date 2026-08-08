using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<IncomeEntity>))]
public enum IncomeEntity
{
    ApartmentWise,
    GeneralOthers
}
