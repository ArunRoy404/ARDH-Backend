using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<ExpenseNature>))]
public enum ExpenseNature
{
    Service,
    Material,
    Others
}
