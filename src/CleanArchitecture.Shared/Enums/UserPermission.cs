using System.Text.Json.Serialization;

namespace CleanArchitecture.Shared.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserPermission
{
    // Covers /api/dashboard and /api/activities. /api/notifications is open to all
    // authenticated users regardless of permission (see PermissionAuthorizationFilter).
    dashboard,

    // Properties modules
    buildings,
    owners,
    apartments,
    tenants,

    // Operations modules
    vendors,
    equipment,
    amc_contracts,
    maintenance,

    // Finance modules
    income,
    reports,
    expenses,
    occupancy_reports,

    // Full admin access: /api/users, /api/settings, /api/deleted-history
    admin
}
