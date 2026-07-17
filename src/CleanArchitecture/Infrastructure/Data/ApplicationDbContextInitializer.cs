using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Data;

public class ApplicationDbContextInitializer(ApplicationDbContext context, ILoggerFactory logger)
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger _logger = logger.CreateLogger<ApplicationDbContextInitializer>();

    public async Task InitializeAsync()
    {
        try
        {
            // Apply migrations (in-memory db ignores migrations but will still run SeedUser in local dev mode)
            if (_context.Database.IsRelational())
            {
                await _context.Database.MigrateAsync();
            }
            await SeedUser();
        }
        catch (Exception exception)
        {
            _logger.LogError("Migration/seeding error {exception}", exception);
            throw;
        }
    }

    public async Task SeedUser()
    {
        if (!await _context.Users.AnyAsync())
        {
            await _context.Users.AddRangeAsync(
                new List<User>
                {
                    new User
                    {
                        Id = Guid.NewGuid(),
                        Name = "Admin",
                        Email = "admin@gmail.com",
                        Phone = "1234567890",
                        PasswordHash = "P@ssw0rd".Hash(),
                        Role = UserRole.admin,
                        Address = "Main Admin Address",
                        Permissions = "dashboard,property,finance,operations,admin",
                        AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        Id = Guid.NewGuid(),
                        Name = "Property Manager",
                        Email = "manager@gmail.com",
                        Phone = "0987654321",
                        PasswordHash = "P@ssw0rd".Hash(),
                        Role = UserRole.property_manager,
                        Address = "Property Manager Office",
                        Permissions = "dashboard,property",
                        AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                }
            );
            await _context.SaveChangesAsync();
        }
    }
}
