using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Converters;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonPropertyNameEnumConverter<AmcStatus>))]
public enum AmcStatus
{
    Active,
    Expiring,
    Expired,
    Cancelled
}
