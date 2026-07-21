using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExpenseCategory
{
    Utility,
    Operational,
    Maintenance,
    Tax,
    Capital
}
