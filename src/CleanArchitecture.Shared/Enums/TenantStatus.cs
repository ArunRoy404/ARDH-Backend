using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantStatus
{
    Active,
    [JsonPropertyName("Moved Out")]
    MovedOut
}
