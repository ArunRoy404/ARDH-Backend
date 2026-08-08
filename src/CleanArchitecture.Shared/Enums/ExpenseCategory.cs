using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<ExpenseCategory>))]
public enum ExpenseCategory
{
    Utility,
    Operational,
    Maintenance,
    Tax,
    Capital
}
