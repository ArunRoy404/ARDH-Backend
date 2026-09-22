using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Data;

/// <summary>
/// Applies migrations on startup. No seed data and no auto-created accounts -
/// the database (including its first admin user and Settings row) is expected
/// to already exist via migration.
/// </summary>
public class ApplicationDbContextInitializer(ApplicationDbContext context, ILoggerFactory logger)
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger _logger = logger.CreateLogger<ApplicationDbContextInitializer>();

    public async Task InitializeAsync()
    {
        try
        {
            if (_context.Database.IsRelational())
            {
                await _context.Database.MigrateAsync();
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(@"
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'bulk_uploads')
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('bulk_uploads') AND name = 'progress_percentage')
                            BEGIN
                                ALTER TABLE bulk_uploads ADD progress_percentage int NOT NULL DEFAULT 0;
                            END
                        END");
                }
                catch { }
            }

            await MigrateLegacyPermissionsAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError("Migration error {exception}", exception);
            throw;
        }
    }

    // Mirrors UserService's private DefaultRolePermissions. Duplicated here (rather than
    // referencing UserService) because this runs at startup, before the DI-scoped service
    // layer is relevant - it is only used to repair legacy data below.
    private static readonly Dictionary<UserRole, UserPermission[]> DefaultRolePermissionsForMigration = new()
    {
        [UserRole.admin] = Enum.GetValues<UserPermission>(),
        [UserRole.viewer] = [],
        [UserRole.property_manager] = [UserPermission.dashboard, UserPermission.vendors, UserPermission.equipment, UserPermission.amc_contracts, UserPermission.maintenance, UserPermission.expenses],
        [UserRole.accountant] = [UserPermission.dashboard, UserPermission.income, UserPermission.reports, UserPermission.expenses],
    };

    /// <summary>
    /// One-off repair for accounts created before the permissions model changed from 5 coarse
    /// buckets (dashboard/properties/finance/operations/admin) to granular per-module
    /// permissions. Drops any token that is no longer a valid UserPermission (e.g. the old
    /// "properties"/"operations"/"finance" bucket words), re-unions with the user's current
    /// role defaults, and only writes back rows whose resolved string actually changed - so on
    /// every subsequent startup this is a cheap read with nothing left to update.
    /// </summary>
    private async Task MigrateLegacyPermissionsAsync()
    {
        var users = await _context.Users.IgnoreQueryFilters().Where(x => !x.IsDeleted).ToListAsync();
        var updated = new List<User>();

        foreach (var user in users)
        {
            var validTokens = (user.Permissions ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(p => Enum.TryParse<UserPermission>(p, true, out _))
                .Select(p => Enum.Parse<UserPermission>(p, true));

            var permissions = new HashSet<UserPermission>(
                DefaultRolePermissionsForMigration.TryGetValue(user.Role, out var defaults) ? defaults : []);
            permissions.UnionWith(validTokens);

            var resolved = string.Join(",", Enum.GetValues<UserPermission>().Where(permissions.Contains));

            if (resolved != (user.Permissions ?? string.Empty))
            {
                user.Permissions = resolved;
                updated.Add(user);
            }
        }

        if (updated.Count > 0)
        {
            _context.Users.UpdateRange(updated);
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Migrated {count} user(s) from legacy bucket-based permissions to the new granular module permissions.",
                updated.Count);
        }
    }
}
