using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Data;

/// <summary>
/// Applies migrations and, on a brand-new empty database, bootstraps the single
/// admin account needed to log in for the first time. No demo/sample data.
/// </summary>
public class ApplicationDbContextInitializer(ApplicationDbContext context, ILoggerFactory logger, AppSettings appSettings)
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger _logger = logger.CreateLogger<ApplicationDbContextInitializer>();
    private readonly AppSettings _appSettings = appSettings;

    private static readonly Guid AdminUserId = Guid.Parse("7ca6dfd0-bfd8-4f10-977b-608b8b4081c7");
    private static readonly Guid SettingId = Guid.Parse("3ea2b822-29c4-52a8-ad29-c8be5d491f24");

    private static readonly DateTime T0 = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    public async Task InitializeAsync()
    {
        try
        {
            // Apply migrations (in-memory db ignores migrations but will still run seeding in local dev mode)
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

            if (!await _context.Users.AnyAsync())
            {
                await _context.Users.AddAsync(CreateAdminUser());
                await _context.SaveChangesAsync();
            }

            await SeedSettings();
        }
        catch (Exception exception)
        {
            _logger.LogError("Migration/seeding error {exception}", exception);
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

    private User CreateAdminUser() => new()
    {
        Id = AdminUserId,
        Name = "Super Admin",
        Email = _appSettings.AdminSettings?.BootstrapEmail is { Length: > 0 } email ? email : "admin@example.com",
        Phone = "+1234567890",
        PasswordHash = (_appSettings.AdminSettings?.BootstrapPassword is { Length: > 0 } password ? password : "ChangeMe123!").Hash(),
        Role = UserRole.admin,
        Address = "123 Main St",
        Permissions = "dashboard,buildings,owners,apartments,tenants,vendors,equipment,amc_contracts,maintenance,income,reports,expenses,occupancy_reports,admin",
        AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde",
        IsActive = true,
        ReceiveEmailNotifications = true,
        CreatedAt = T0,
        UpdatedAt = T0
    };

    private async Task SeedSettings()
    {
        if (await _context.Settings.AnyAsync()) return;

        await _context.Settings.AddAsync(new Setting
        {
            Id = SettingId,
            CompanyName = "Ardh Property Management",
            CompanyEmail = "info@ardh.com",
            Phone = "+91 1234567890",
            Address = "123 Main Street, Bangalore, India",
            Icon = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde",
            Fav = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde",
            AdminPassword = (_appSettings.AdminSettings?.Password ?? "adminpassword").Hash(),
            UpdatedBy = AdminUserId,
            UpdatedAt = T0
        });
        await _context.SaveChangesAsync();
    }

}
