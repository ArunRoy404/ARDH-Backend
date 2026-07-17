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
            await SeedBuildings();
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

    public async Task SeedBuildings()
    {
        if (!await _context.Buildings.AnyAsync())
        {
            await _context.Buildings.AddRangeAsync(
                new List<Building>
                {
                    new Building
                    {
                        Id = Guid.Parse("b1f7b822-29c4-52a8-ad29-c8be5d491f24"),
                        BuildingName = "Grand Plaza Towers",
                        Address = "123 Skyline Boulevard",
                        City = "New York",
                        State = "NY",
                        Country = "USA",
                        GoogleMapLink = "https://maps.google.com/?q=123+Skyline+Boulevard+New+York",
                        TotalFloors = 24,
                        ParkingDetails = "Basement Level 1 & 2, 150 Slots Available",
                        Status = BuildingStatus.active,
                        Description = "Premium luxury residence building in Manhattan with scenic city views and standard amenities.",
                        ImageUrl = "https://images.unsplash.com/photo-1545324418-cc1a3fa10c00",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Building
                    {
                        Id = Guid.Parse("c2f7b822-29c4-52a8-ad29-c8be5d491f25"),
                        BuildingName = "Oakridge Residency",
                        Address = "456 Pinecrest Heights",
                        City = "Seattle",
                        State = "WA",
                        Country = "USA",
                        GoogleMapLink = "https://maps.google.com/?q=456+Pinecrest+Heights+Seattle",
                        TotalFloors = 10,
                        ParkingDetails = "Ground floor open parking, 50 Slots Available",
                        Status = BuildingStatus.active,
                        Description = "Cozy residential complex situated in a quiet suburban neighborhood with nearby parks.",
                        ImageUrl = "https://images.unsplash.com/photo-1564013799919-ab600027ffc6",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                }
            );
            await _context.SaveChangesAsync();
        }
    }
}
