using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Data;

/// <summary>
/// Applies migrations and seeds a fully-aligned demo dataset for every module.
/// All record IDs are fixed and match the examples documented in the
/// Ardh_Postman_Collection.json, so the Postman examples work against the seed data.
///
/// To wipe all existing data and re-seed from scratch, set the environment variable
/// SEED_MODE=reset before starting the application (e.g. `SEED_MODE=reset dotnet run`).
/// </summary>
public class ApplicationDbContextInitializer(ApplicationDbContext context, ILoggerFactory logger)
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger _logger = logger.CreateLogger<ApplicationDbContextInitializer>();

    // ── Canonical IDs (must match the Postman collection) ──────────────────────
    private static readonly Guid AdminUserId = Guid.Parse("7ca6dfd0-bfd8-4f10-977b-608b8b4081c7");
    private static readonly Guid ManagerUserId = Guid.Parse("8ca6dfd0-bfd8-4f10-977b-608b8b4081c8");
    private static readonly Guid AccountantUserId = Guid.Parse("9ca6dfd0-bfd8-4f10-977b-608b8b4081c9");

    private static readonly Guid BuildingGptId = Guid.Parse("b1f7b822-29c4-52a8-ad29-c8be5d491f24");   // Grand Plaza Towers
    private static readonly Guid BuildingOakId = Guid.Parse("c2f7b822-29c4-52a8-ad29-c8be5d491f25");   // Oakridge Residency

    private static readonly Guid OwnerRahulId = Guid.Parse("o1f3b822-29c4-52a8-ad29-c8be5d491f24");   // Rahul Verma
    private static readonly Guid OwnerAmitId = Guid.Parse("f1a3b822-29c4-52a8-ad29-c8be5d491f24");    // Amit Sharma

    private static readonly Guid Apt302Id = Guid.Parse("a1f3b822-29c4-52a8-ad29-c8be5d491f24");       // Flat 302 (GPT)
    private static readonly Guid Apt1204Id = Guid.Parse("e2a3b822-29c4-52a8-ad29-c8be5d491f24");      // Flat 1204-B (GPT)
    private static readonly Guid Apt101Id = Guid.Parse("a5c7b822-29c4-52a8-ad29-c8be5d491f32");       // Flat 101 (GPT)
    private static readonly Guid AptOak301Id = Guid.Parse("d2f3b822-29c4-52a8-ad29-c8be5d491f24");    // Flat 301 (Oakridge)
    private static readonly Guid AptOak102Id = Guid.Parse("d3f3b822-29c4-52a8-ad29-c8be5d491f24");    // Flat 102 (Oakridge)

    private static readonly Guid TenantArjunId = Guid.Parse("t1f3b822-29c4-52a8-ad29-c8be5d491f24");  // Arjun Mehta
    private static readonly Guid TenantJohnId = Guid.Parse("c1f7b822-29c4-52a8-ad29-c8be5d491f24");   // John Tenant

    private static readonly Guid VendorSunilId = Guid.Parse("v1a3b822-29c4-52a8-ad29-c8be5d491f24");  // Sunil Plumbing
    private static readonly Guid VendorRajeshId = Guid.Parse("d1a3b822-29c4-52a8-ad29-c8be5d491f24"); // Sharma Elevator
    private static readonly Guid VendorBescomId = Guid.Parse("v4c7b822-29c4-52a8-ad29-c8be5d491f41"); // BESCOM

    private static readonly Guid EquipLiftId = Guid.Parse("e1a3b822-29c4-52a8-ad29-c8be5d491f24");    // OTIS Elevator Block A
    private static readonly Guid EquipPumpId = Guid.Parse("e2c7b822-29c4-52a8-ad29-c8be5d491f24");    // Water Pump Block A
    private static readonly Guid EquipGenId = Guid.Parse("e3c7b822-29c4-52a8-ad29-c8be5d491f25");     // Diesel Generator

    private static readonly Guid AmcElevatorId = Guid.Parse("f2a3b822-29c4-52a8-ad29-c8be5d491f24");

    private static readonly Guid MaintLeakId = Guid.Parse("m1f3b822-29c4-52a8-ad29-c8be5d491f24");
    private static readonly Guid MaintAcId = Guid.Parse("m2f3b822-29c4-52a8-ad29-c8be5d491f24");
    private static readonly Guid MaintParkingId = Guid.Parse("m3f3b822-29c4-52a8-ad29-c8be5d491f24");

    private static readonly Guid Income302Id = Guid.Parse("f4b3b822-29c4-52a8-ad29-c8be5d491f24");
    private static readonly Guid DeletedHistoryBuildingId = Guid.Parse("4da2b822-29c4-52a8-ad29-c8be5d491f24");
    private static readonly Guid DeletedHistoryTenantId = Guid.Parse("4da2b822-29c4-52a8-ad29-c8be5d491f25");
    private static readonly Guid Income1204Id = Guid.Parse("f4b3b822-29c4-52a8-ad29-c8be5d491f25");
    private static readonly Guid IncomeWaterId = Guid.Parse("f4b3b822-29c4-52a8-ad29-c8be5d491f26");
    private static readonly Guid IncomePendingId = Guid.Parse("f4b3b822-29c4-52a8-ad29-c8be5d491f27");

    private static readonly Guid ExpenseElectricId = Guid.Parse("e4c7b822-29c4-52a8-ad29-c8be5d491f40");
    private static readonly Guid ExpenseHouseId = Guid.Parse("e4c7b822-29c4-52a8-ad29-c8be5d491f41");
    private static readonly Guid ExpenseTankerId = Guid.Parse("e4c7b822-29c4-52a8-ad29-c8be5d491f42");

    private static readonly Guid MoveOutJohnId = Guid.Parse("b1a3b822-29c4-52a8-ad29-c8be5d491f24");
    private static readonly Guid SettingId = Guid.Parse("3ea2b822-29c4-52a8-ad29-c8be5d491f24");

    // Fixed timestamps so seed data is stable and matches the Postman examples
    private static readonly DateTime T0 = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    public async Task InitializeAsync()
    {
        try
        {
            // Apply migrations (in-memory db ignores migrations but will still run seeding in local dev mode)
            if (_context.Database.IsRelational())
            {
                await _context.Database.MigrateAsync();
            }

            // Optional full wipe: set SEED_MODE=reset to remove all data and re-seed from scratch
            var resetMode = Environment.GetEnvironmentVariable("SEED_MODE")?.Trim().ToLowerInvariant() == "reset";
            if (resetMode)
            {
                await ResetDatabaseAsync();
            }

            await SeedUsers();
            await SeedBuildings();
            await SeedOwners();
            await SeedVendors();
            await SeedApartments();
            await SeedTenants();
            await SeedMoveOuts();
            await SeedEquipment();
            await SeedAmcContracts();
            await SeedMaintenanceRequests();
            await SeedIncomeRecords();
            await SeedExpenseRecords();
            await SeedSettings();
            await SeedNotifications();
            await SeedActivities();
            await SeedDeletedHistory();
        }
        catch (Exception exception)
        {
            _logger.LogError("Migration/seeding error {exception}", exception);
            throw;
        }
    }

    /// <summary>Deletes every row (FK-safe order) before re-seeding.</summary>
    private async Task ResetDatabaseAsync()
    {
        _context.NotificationRecipients.RemoveRange(_context.NotificationRecipients);
        _context.Notifications.RemoveRange(_context.Notifications);
        _context.Activities.RemoveRange(_context.Activities);
        _context.DeletedHistories.RemoveRange(_context.DeletedHistories);
        _context.TenantMoveOutRecords.RemoveRange(_context.TenantMoveOutRecords);
        _context.IncomeRecords.RemoveRange(_context.IncomeRecords);
        _context.ExpenseRecords.RemoveRange(_context.ExpenseRecords);
        _context.MaintenanceRequests.RemoveRange(_context.MaintenanceRequests);
        _context.AmcContracts.RemoveRange(_context.AmcContracts);
        _context.Equipment.RemoveRange(_context.Equipment);
        _context.Tenants.RemoveRange(_context.Tenants);
        _context.Apartments.RemoveRange(_context.Apartments);
        _context.Owners.RemoveRange(_context.Owners);
        _context.Vendors.RemoveRange(_context.Vendors);
        _context.Buildings.RemoveRange(_context.Buildings);
        _context.ForgotPassword.RemoveRange(_context.ForgotPassword);
        _context.Users.RemoveRange(_context.Users);
        _context.Settings.RemoveRange(_context.Settings);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Database reset: all existing data removed.");
    }

    private async Task SeedUsers()
    {
        if (await _context.Users.AnyAsync()) return;

        await _context.Users.AddRangeAsync(
            new List<User>
            {
                new()
                {
                    Id = AdminUserId,
                    Name = "Super Admin",
                    Email = "admin@gmail.com",
                    Phone = "+1234567890",
                    PasswordHash = "P@ssw0rd".Hash(),
                    Role = UserRole.admin,
                    Address = "123 Main St",
                    Permissions = "dashboard,properties,finance,operations,admin",
                    AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde",
                    IsActive = true,
                    CreatedAt = T0,
                    UpdatedAt = T0
                },
                new()
                {
                    Id = ManagerUserId,
                    Name = "Property Manager",
                    Email = "manager@gmail.com",
                    Phone = "+0987654321",
                    PasswordHash = "P@ssw0rd".Hash(),
                    Role = UserRole.property_manager,
                    Address = "Property Manager Office",
                    Permissions = "dashboard,properties,operations",
                    AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde",
                    IsActive = true,
                    CreatedAt = T0,
                    UpdatedAt = T0
                },
                new()
                {
                    Id = AccountantUserId,
                    Name = "Accountant",
                    Email = "accountant@gmail.com",
                    Phone = "+0112233445",
                    PasswordHash = "P@ssw0rd".Hash(),
                    Role = UserRole.accountant,
                    Address = "Accounts Office",
                    Permissions = "dashboard,finance",
                    AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde",
                    IsActive = true,
                    CreatedAt = T0,
                    UpdatedAt = T0
                }
            });
        await _context.SaveChangesAsync();
    }

    private async Task SeedBuildings()
    {
        if (await _context.Buildings.AnyAsync()) return;

        await _context.Buildings.AddRangeAsync(
            new List<Building>
            {
                new()
                {
                    Id = BuildingGptId,
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
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = BuildingOakId,
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
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                }
            });
        await _context.SaveChangesAsync();
    }

    private async Task SeedOwners()
    {
        if (await _context.Owners.AnyAsync()) return;

        await _context.Owners.AddRangeAsync(
            new List<Owner>
            {
                new()
                {
                    Id = OwnerRahulId,
                    FullName = "Rahul Verma",
                    Phone = "+91 9876500011",
                    Email = "rahul.verma@example.com",
                    City = "Mumbai",
                    Address = "45, Marine Drive",
                    IdType = OwnerIdType.Aadhar,
                    IdNumber = "1234-5678-9012",
                    BankName = "HDFC Bank",
                    AccountNumber = "50100123456789",
                    IfscCode = "HDFC0000123",
                    Status = OwnerStatus.Active,
                    Notes = "Primary owner for block A.",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = OwnerAmitId,
                    FullName = "Amit Sharma",
                    Phone = "+91 9988776655",
                    Email = "amit.sharma@example.com",
                    City = "Mumbai",
                    Address = "45, Marine Drive",
                    IdType = OwnerIdType.Aadhar,
                    IdNumber = "2233-4455-6677",
                    BankName = "HDFC Bank",
                    AccountNumber = "50100223344556",
                    IfscCode = "HDFC0000123",
                    Status = OwnerStatus.Active,
                    Notes = "Primary owner for block B.",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                }
            });
        await _context.SaveChangesAsync();
    }

    private async Task SeedVendors()
    {
        if (await _context.Vendors.AnyAsync()) return;

        await _context.Vendors.AddRangeAsync(
            new List<Vendor>
            {
                new()
                {
                    Id = VendorSunilId,
                    Name = "Sunil Kumar",
                    CompanyName = "Sunil Plumbing Services",
                    Phone = "+919888776655",
                    Email = "sunil@plumbing.com",
                    VendorType = "Plumbing",
                    GstNumber = "27BBBBB2222B1Z2",
                    Address = "Shop 12, Market Road",
                    Status = VendorStatus.Active,
                    Notes = "Preferred plumber for all building units",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = VendorRajeshId,
                    Name = "Rajesh Sharma",
                    CompanyName = "Sharma Elevator Services",
                    Phone = "+919999888877",
                    Email = "rajesh@solutions.com",
                    VendorType = "Elevator Service",
                    GstNumber = "27AAAAA1111A1Z1",
                    Address = "Sector 5, Nerul",
                    Status = VendorStatus.Active,
                    Notes = "Preferred elevator vendor for all building units",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = VendorBescomId,
                    Name = "BESCOM",
                    CompanyName = "Bangalore Electricity",
                    Phone = "+91 9876500112",
                    Email = "billing@bescom.example.com",
                    VendorType = "Electricity",
                    GstNumber = "29AAAAA1111A1Z2",
                    Address = "12 MG Road, Bangalore",
                    Status = VendorStatus.Active,
                    Notes = "Electricity supplier for common areas",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                }
            });
        await _context.SaveChangesAsync();
    }

    private async Task SeedApartments()
    {
        if (await _context.Apartments.AnyAsync()) return;

        await _context.Apartments.AddRangeAsync(
            new List<Apartment>
            {
                new()
                {
                    Id = Apt302Id,
                    BuildingId = BuildingGptId,
                    OwnerId = OwnerRahulId,
                    NestawayId = "NEST-302-AA",
                    FlatNumber = "302",
                    Floor = 3,
                    ApartmentType = "2 BHK",
                    AreaSqft = 1050,
                    Bedrooms = 2,
                    Bathrooms = 2,
                    HasBalcony = true,
                    ParkingSlot = "P-12",
                    ExpectedRent = 85000,
                    MaintenanceCharge = 4500,
                    WaterCharge = 800,
                    Notes = "Corner unit, good sunlight.",
                    CurrentTenantId = TenantArjunId,
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = Apt1204Id,
                    BuildingId = BuildingGptId,
                    OwnerId = OwnerAmitId,
                    NestawayId = "NST-7719",
                    FlatNumber = "1204-B",
                    Floor = 12,
                    ApartmentType = "3 BHK",
                    AreaSqft = 1450,
                    Bedrooms = 3,
                    Bathrooms = 3,
                    HasBalcony = true,
                    ParkingSlot = "P-15",
                    ExpectedRent = 45000,
                    MaintenanceCharge = 3500,
                    WaterCharge = 500,
                    CurrentTenantId = TenantJohnId,
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = Apt101Id,
                    BuildingId = BuildingGptId,
                    OwnerId = OwnerRahulId,
                    NestawayId = "NEST-101-AA",
                    FlatNumber = "101",
                    Floor = 1,
                    ApartmentType = "1 BHK",
                    AreaSqft = 600,
                    Bedrooms = 1,
                    Bathrooms = 1,
                    HasBalcony = false,
                    ParkingSlot = "P-01",
                    ExpectedRent = 20000,
                    MaintenanceCharge = 1500,
                    WaterCharge = 300,
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = AptOak301Id,
                    BuildingId = BuildingOakId,
                    OwnerId = OwnerAmitId,
                    NestawayId = "NEST-301-BB",
                    FlatNumber = "301",
                    Floor = 3,
                    ApartmentType = "2 BHK",
                    AreaSqft = 950,
                    Bedrooms = 2,
                    Bathrooms = 2,
                    HasBalcony = true,
                    ParkingSlot = "P-03",
                    ExpectedRent = 32000,
                    MaintenanceCharge = 2200,
                    WaterCharge = 400,
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = AptOak102Id,
                    BuildingId = BuildingOakId,
                    OwnerId = OwnerRahulId,
                    NestawayId = "NEST-102-BB",
                    FlatNumber = "102",
                    Floor = 1,
                    ApartmentType = "1 BHK",
                    AreaSqft = 550,
                    Bedrooms = 1,
                    Bathrooms = 1,
                    HasBalcony = false,
                    ParkingSlot = "P-04",
                    ExpectedRent = 18000,
                    MaintenanceCharge = 1200,
                    WaterCharge = 250,
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                }
            });
        await _context.SaveChangesAsync();
    }

    private async Task SeedTenants()
    {
        if (await _context.Tenants.AnyAsync()) return;

        await _context.Tenants.AddRangeAsync(
            new List<Tenant>
            {
                new()
                {
                    Id = TenantArjunId,
                    BuildingId = BuildingGptId,
                    ApartmentId = Apt302Id,
                    FullName = "Arjun Mehta",
                    Phone = "+91 9876543210",
                    Email = "arjun.mehta@example.com",
                    IdType = OwnerIdType.Aadhar,
                    IdNumber = "9988-7766-5544",
                    IdProofAttachmentUrl = "https://example.com/uploads/idproof.pdf",
                    MoveInDate = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                    LeaseStartDate = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                    LeaseEndDate = new DateTime(2027, 4, 30, 12, 0, 0, DateTimeKind.Utc),
                    MonthlyRent = 85000,
                    SecurityDeposit = 170000,
                    EmergencyContactName = "Priya Mehta",
                    EmergencyContactPhone = "+91 9876543200",
                    Status = TenantStatus.Active,
                    Notes = "Responsible tenant",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = TenantJohnId,
                    BuildingId = BuildingGptId,
                    ApartmentId = Apt1204Id,
                    FullName = "John Tenant",
                    Phone = "+15550199",
                    Email = "john.t@gmail.com",
                    IdType = OwnerIdType.Passport,
                    IdNumber = "Z1234567",
                    IdProofAttachmentUrl = "https://example.com/uploads/john_idproof.pdf",
                    MoveInDate = T0,
                    LeaseStartDate = T0,
                    LeaseEndDate = new DateTime(2027, 7, 20, 12, 0, 0, DateTimeKind.Utc),
                    MonthlyRent = 45000,
                    SecurityDeposit = 90000,
                    EmergencyContactName = "Jane Tenant",
                    EmergencyContactPhone = "+15550200",
                    Status = TenantStatus.Active,
                    Notes = "Responsible tenant",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                }
            });
        await _context.SaveChangesAsync();
    }

    private async Task SeedMoveOuts()
    {
        if (await _context.TenantMoveOutRecords.AnyAsync()) return;

        await _context.TenantMoveOutRecords.AddAsync(new TenantMoveOutRecord
        {
            Id = MoveOutJohnId,
            TenantId = TenantJohnId,
            ApartmentId = Apt1204Id,
            MoveOutDate = new DateTime(2027, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            PendingRent = 0,
            DamageAmount = 5000,
            RefundAmount = 85000,
            IdNumber = "Z1234567",
            HandoverNote = "Keys returned, some minor wall scratches.",
            ProcessedBy = AdminUserId,
            CreatedAt = T0,
            UpdatedAt = T0,
            CreatedBy = AdminUserId,
            UpdatedBy = AdminUserId
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedEquipment()
    {
        if (await _context.Equipment.AnyAsync()) return;

        await _context.Equipment.AddRangeAsync(
            new List<Equipment>
            {
                new()
                {
                    Id = EquipLiftId,
                    BuildingId = BuildingGptId,
                    Name = "OTIS Elevator Block A",
                    Type = "Lift",
                    Brand = "OTIS",
                    Model = "Gen2",
                    SerialNumber = "OTIS-55291-A",
                    InstallDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                    WarrantyExpiryDate = new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                    Status = "Operational",
                    Notes = "Working perfectly.",
                    AttachmentUrl = "https://example.com/uploads/manual.pdf",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = EquipPumpId,
                    BuildingId = BuildingGptId,
                    Name = "Water Pump Block A",
                    Type = "Pump",
                    Brand = "Kirloskar",
                    Model = "KDS-1520",
                    SerialNumber = "KIR-88123",
                    InstallDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                    WarrantyExpiryDate = new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                    Status = "Operational",
                    Notes = "Main water pump for Block A",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = EquipGenId,
                    BuildingId = BuildingGptId,
                    Name = "Diesel Generator",
                    Type = "Generator",
                    Brand = "Cummins",
                    Model = "C30D5",
                    SerialNumber = "CUM-99120",
                    InstallDate = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    WarrantyExpiryDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    Status = "Operational",
                    Notes = "Backup power for common areas",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                }
            });
        await _context.SaveChangesAsync();
    }

    private async Task SeedAmcContracts()
    {
        if (await _context.AmcContracts.AnyAsync()) return;

        await _context.AmcContracts.AddAsync(new AmcContract
        {
            Id = AmcElevatorId,
            AmcCode = "AMC-2026-001",
            ContractNumber = "CN-OTIS-2026-881",
            ContractTitle = "Annual Elevator Comprehensive Maintenance 2026",
            ContractType = AmcContractType.Comprehensive,
            EquipmentId = EquipLiftId,
            VendorId = VendorRajeshId,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            ContractAmount = 120000,
            PaymentTerms = AmcPaymentTerm.QuarterlyAdvance,
            ServiceFrequency = AmcServiceFrequency.Quarterly,
            CoverageDetails = "All spare parts and labour included.",
            Exclusions = "Damage due to natural disasters.",
            DocumentLink = "https://example.com/uploads/contract.pdf",
            Remarks = "Renewed for 1 year.",
            Status = AmcStatus.Active,
            CreatedAt = T0,
            UpdatedAt = T0,
            CreatedBy = AdminUserId,
            UpdatedBy = AdminUserId
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedMaintenanceRequests()
    {
        if (await _context.MaintenanceRequests.AnyAsync()) return;

        await _context.MaintenanceRequests.AddRangeAsync(
            new List<MaintenanceRequest>
            {
                new()
                {
                    Id = MaintLeakId,
                    Title = "Water leakage in Flat 302",
                    Description = "Ceiling leak in the master bathroom. Immediate attention required.",
                    Category = "Plumbing",
                    Priority = MaintenancePriority.High,
                    Status = MaintenanceStatus.Open,
                    VendorId = VendorSunilId,
                    EquipmentId = EquipPumpId,
                    BuildingId = BuildingGptId,
                    ApartmentId = Apt302Id,
                    EstimatedCost = 15000,
                    AnnualCost = 180000,
                    ScheduledDate = new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc),
                    StartDate = new DateTime(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc),
                    RecurrenceFrequency = MaintenanceRecurrenceFrequency.Weekly,
                    Notes = "Tenant reported leak on 18 July.",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = MaintAcId,
                    Title = "AC not cooling",
                    Description = "Living room AC in Flat 101 not cooling properly.",
                    Category = "HVAC",
                    Priority = MaintenancePriority.Medium,
                    Status = MaintenanceStatus.InProgress,
                    BuildingId = BuildingGptId,
                    ApartmentId = Apt101Id,
                    EstimatedCost = 8000,
                    ScheduledDate = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc),
                    StartDate = new DateTime(2026, 7, 22, 10, 0, 0, DateTimeKind.Utc),
                    Notes = "Tenant requested service visit.",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = MaintParkingId,
                    Title = "Parking lot lights broken",
                    Description = "2 lights on ground floor parking not working.",
                    Category = "Electrical",
                    Priority = MaintenancePriority.Medium,
                    Status = MaintenanceStatus.Open,
                    BuildingId = BuildingGptId,
                    EstimatedCost = 4500,
                    ScheduledDate = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc),
                    Notes = "Common area issue.",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                }
            });
        await _context.SaveChangesAsync();
    }

    private async Task SeedIncomeRecords()
    {
        if (await _context.IncomeRecords.AnyAsync()) return;

        await _context.IncomeRecords.AddRangeAsync(
            new List<IncomeRecord>
            {
                new()
                {
                    Id = Income302Id,
                    IncomeEntity = IncomeEntity.ApartmentWise,
                    IncomeType = IncomeType.Rent,
                    Amount = 85000,
                    BuildingId = BuildingGptId,
                    ApartmentId = Apt302Id,
                    PaymentDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    PaymentMethod = IncomePaymentMethod.BankTransfer,
                    TransactionReference = "TXN-88231",
                    Status = IncomeStatus.Paid,
                    Notes = "Rent for Flat 302.",
                    AttachmentUrl = "https://example.com/uploads/receipt.pdf",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = Income1204Id,
                    IncomeEntity = IncomeEntity.ApartmentWise,
                    IncomeType = IncomeType.Rent,
                    Amount = 45000,
                    BuildingId = BuildingGptId,
                    ApartmentId = Apt1204Id,
                    PaymentDate = new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc),
                    PaymentMethod = IncomePaymentMethod.DirectFromTenant,
                    Status = IncomeStatus.Paid,
                    Notes = "Rent for Flat 1204-B.",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = IncomeWaterId,
                    IncomeEntity = IncomeEntity.ApartmentWise,
                    IncomeType = IncomeType.WaterCharge,
                    Amount = 800,
                    BuildingId = BuildingGptId,
                    ApartmentId = Apt302Id,
                    PaymentDate = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
                    PaymentMethod = IncomePaymentMethod.Cash,
                    Status = IncomeStatus.Paid,
                    Notes = "Water charge for Flat 302.",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = IncomePendingId,
                    IncomeEntity = IncomeEntity.ApartmentWise,
                    IncomeType = IncomeType.Rent,
                    Amount = 45000,
                    BuildingId = BuildingGptId,
                    ApartmentId = Apt1204Id,
                    PaymentDate = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                    PaymentMethod = IncomePaymentMethod.BankTransfer,
                    Status = IncomeStatus.Pending,
                    Notes = "Rent for Flat 1204-B - awaiting payment.",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                }
            });
        await _context.SaveChangesAsync();
    }

    private async Task SeedExpenseRecords()
    {
        if (await _context.ExpenseRecords.AnyAsync()) return;

        await _context.ExpenseRecords.AddRangeAsync(
            new List<ExpenseRecord>
            {
                new()
                {
                    Id = ExpenseElectricId,
                    Category = ExpenseCategory.Utility,
                    ExpenseHead = "Electricity Bill",
                    SpecificItem = "Common area lights",
                    VendorId = VendorBescomId,
                    Nature = ExpenseNature.Service,
                    Amount = 12500,
                    Entity = ExpenseEntity.BuildingLevel,
                    BuildingId = BuildingGptId,
                    ExpenseDate = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
                    PaymentMethod = "BankTransfer",
                    Status = ExpenseStatus.Paid,
                    Reference = "TXN123456789",
                    Description = "April bill",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = ExpenseHouseId,
                    Category = ExpenseCategory.Operational,
                    ExpenseHead = "Housekeeping",
                    SpecificItem = "Common area cleaning",
                    Nature = ExpenseNature.Service,
                    Amount = 22000,
                    Entity = ExpenseEntity.BuildingLevel,
                    BuildingId = BuildingGptId,
                    ExpenseDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    PaymentMethod = "Cash",
                    Status = ExpenseStatus.Paid,
                    Description = "Monthly housekeeping",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                },
                new()
                {
                    Id = ExpenseTankerId,
                    Category = ExpenseCategory.Utility,
                    ExpenseHead = "Water Tank Supply",
                    SpecificItem = "Water tanker delivery",
                    VendorId = VendorSunilId,
                    Nature = ExpenseNature.Service,
                    Amount = 900,
                    Entity = ExpenseEntity.BuildingLevel,
                    BuildingId = BuildingGptId,
                    ExpenseDate = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
                    PaymentMethod = "Cash",
                    Status = ExpenseStatus.Paid,
                    TankerNumber = "KA-09-1234",
                    TimeOfDelivery = new DateTime(2026, 8, 5, 9, 30, 0, DateTimeKind.Utc),
                    DeliveryDriverName = "Ramesh",
                    ManagerInAttendance = "Arun Roy",
                    LitersFilled = 12000,
                    Description = "Monthly water tanker for Grand Plaza Towers.",
                    CreatedAt = T0,
                    UpdatedAt = T0,
                    CreatedBy = AdminUserId,
                    UpdatedBy = AdminUserId
                }
            });
        await _context.SaveChangesAsync();
    }

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
            AdminPassword = "adminpassword".Hash(),
            UpdatedBy = AdminUserId,
            UpdatedAt = T0
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedNotifications()
    {
        if (await _context.Notifications.AnyAsync()) return;

        var notifications = new List<Notification>
        {
            new()
            {
                Id = Guid.Parse("e4f3b822-29c4-52a8-ad29-c8be5d491f01"),
                Type = "properties",
                Title = "Lease Expiring: Arjun Mehta",
                Detail = "Lease for Arjun Mehta is expiring on 2027-04-30. Flat: 302.",
                CreatedAt = T0.AddHours(-2)
            },
            new()
            {
                Id = Guid.Parse("e4f3b822-29c4-52a8-ad29-c8be5d491f02"),
                Type = "operations",
                Title = "AMC Expiring: Annual Elevator Comprehensive Maintenance 2026",
                Detail = "AMC contract 'Annual Elevator Comprehensive Maintenance 2026' (AMC-2026-001) expires on 2026-12-31.",
                CreatedAt = T0.AddHours(-1)
            },
            new()
            {
                Id = Guid.Parse("e4f3b822-29c4-52a8-ad29-c8be5d491f03"),
                Type = "finance",
                Title = "Expense Paid",
                Detail = "12,500 SAR paid for Common area lights at Grand Plaza Towers.",
                CreatedAt = T0
            }
        };

        await _context.Notifications.AddRangeAsync(notifications);
        await _context.SaveChangesAsync();

        // Deliver notifications to the seeded active users based on their permissions
        var recipients = new List<NotificationRecipient>();
        var users = await _context.Users.Where(x => x.IsActive).ToListAsync();
        foreach (var notification in notifications)
        {
            foreach (var user in users)
            {
                if (!CanReceiveType(user, notification.Type)) continue;
                recipients.Add(new NotificationRecipient
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notification.Id,
                    UserId = user.Id,
                    IsRead = false,
                    ReadAt = null
                });
            }
        }

        if (recipients.Any())
        {
            await _context.NotificationRecipients.AddRangeAsync(recipients);
            await _context.SaveChangesAsync();
        }
    }

    private static bool CanReceiveType(User user, string type)
    {
        var isAdmin = user.Role == UserRole.admin;
        var isManager = user.Role == UserRole.property_manager;
        var permissions = (user.Permissions ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .ToList();

        if (isAdmin || permissions.Contains("admin")) return true;

        return type.ToLowerInvariant() switch
        {
            "operations" => isManager || permissions.Contains("operations"),
            "finance" => isManager || permissions.Contains("finance"),
            "properties" => isManager || permissions.Contains("properties"),
            _ => isManager || permissions.Contains("dashboard")
        };
    }

    private async Task SeedActivities()
    {
        if (await _context.Activities.AnyAsync()) return;

        await _context.Activities.AddRangeAsync(
            new List<Activity>
            {
                new()
                {
                    Id = Guid.Parse("a1f3b822-29c4-52a8-ad29-c8be5d491f41"),
                    ActionType = "Create",
                    EntityType = "Building",
                    EntityId = BuildingGptId,
                    BuildingId = BuildingGptId,
                    Description = "Building 'Grand Plaza Towers' was created.",
                    CreatedAt = T0
                },
                new()
                {
                    Id = Guid.Parse("a1f3b822-29c4-52a8-ad29-c8be5d491f42"),
                    ActionType = "Create",
                    EntityType = "Apartment",
                    EntityId = Apt302Id,
                    BuildingId = BuildingGptId,
                    Description = "Apartment 'Flat 302' was created.",
                    CreatedAt = T0.AddMinutes(5)
                },
                new()
                {
                    Id = Guid.Parse("a1f3b822-29c4-52a8-ad29-c8be5d491f43"),
                    ActionType = "Create",
                    EntityType = "Tenant",
                    EntityId = TenantArjunId,
                    BuildingId = BuildingGptId,
                    Description = "Tenant 'Arjun Mehta' moved into Flat 302.",
                    CreatedAt = T0.AddMinutes(10)
                },
                new()
                {
                    Id = Guid.Parse("a1f3b822-29c4-52a8-ad29-c8be5d491f44"),
                    ActionType = "Create",
                    EntityType = "IncomeRecord",
                    EntityId = Income302Id,
                    BuildingId = BuildingGptId,
                    Description = "15,000 SAR rent recorded for Flat 302.",
                    CreatedAt = T0.AddMinutes(15)
                },
                new()
                {
                    Id = Guid.Parse("a1f3b822-29c4-52a8-ad29-c8be5d491f45"),
                    ActionType = "Create",
                    EntityType = "ExpenseRecord",
                    EntityId = ExpenseElectricId,
                    BuildingId = BuildingGptId,
                    Description = "12,500 SAR paid for Common area lights at Grand Plaza Towers.",
                    CreatedAt = T0.AddMinutes(20)
                }
            });
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds a couple of soft-delete history records so the Deleted History endpoints
    /// (DH-01..DH-04) have data to work with on a fresh database.
    /// </summary>
    private async Task SeedDeletedHistory()
    {
        if (await _context.DeletedHistories.AnyAsync()) return;

        await _context.DeletedHistories.AddRangeAsync(
            new List<DeletedHistory>
            {
                new()
                {
                    Id = DeletedHistoryBuildingId,
                    EntityType = "Building",
                    EntityId = BuildingGptId,
                    EntityTitle = "Grand Plaza Towers-New York-NY-2026-07-20T12:00:00Z",
                    DeletedBy = AdminUserId,
                    DeletedAt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc)
                },
                new()
                {
                    Id = DeletedHistoryTenantId,
                    EntityType = "Tenant",
                    EntityId = TenantJohnId,
                    EntityTitle = "John Tenant (john.t@gmail.com)",
                    DeletedBy = AdminUserId,
                    DeletedAt = new DateTime(2026, 8, 2, 11, 0, 0, DateTimeKind.Utc)
                }
            });
        await _context.SaveChangesAsync();
    }
}
