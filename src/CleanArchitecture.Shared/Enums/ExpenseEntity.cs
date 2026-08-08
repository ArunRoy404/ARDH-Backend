using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<ExpenseEntity>))]
public enum ExpenseEntity
{
    General,
    ApartmentSpecific,
    BuildingLevel
}
