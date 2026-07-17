using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserPermission
{
    dashboard,
    properties,
    finance,
    operations,
    admin
}
