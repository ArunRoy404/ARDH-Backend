using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Apartment;
using CleanArchitecture.Shared.Models.Building;
using CleanArchitecture.Shared.Models.Owner;

namespace CleanArchitecture.Application.Services;

public class ApartmentService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IActivityService activityService, INotificationService notificationService) : IApartmentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IActivityService _activityService = activityService;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<PaginatedList<ApartmentViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        Guid? buildingId,
        Guid? ownerId,
        string? apartmentType,
        string? status,
        CancellationToken cancellationToken)
    {
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x =>
            (!buildingId.HasValue || x.BuildingId == buildingId.Value) &&
            (!ownerId.HasValue || x.OwnerId == ownerId.Value) &&
            (string.IsNullOrWhiteSpace(apartmentType) || x.ApartmentType == apartmentType.Trim()) &&
            (string.IsNullOrWhiteSpace(status) || 
             (status.Trim().ToLower() == "occupied" ? x.CurrentTenantId != null :
              status.Trim().ToLower() == "vacant" ? x.CurrentTenantId == null : true)));
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var owners = await _unitOfWork.OwnerRepository.GetAllAsync();
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);
        var tenantMap = tenants.ToDictionary(t => t.Id, t => t.FullName);

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var ownerMap = owners.ToDictionary(o => o.Id, o => o.FullName);

        var query = apartments.AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (ownerId.HasValue)
        {
            query = query.Where(x => x.OwnerId == ownerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(apartmentType))
        {
            query = query.Where(x => x.ApartmentType == apartmentType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var cleanStatus = status.Trim().ToLower();
            if (cleanStatus == "occupied")
            {
                query = query.Where(x => x.CurrentTenantId != null);
            }
            else if (cleanStatus == "vacant")
            {
                query = query.Where(x => x.CurrentTenantId == null);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(x =>
                (x.NestawayId != null && x.NestawayId.ToLower().Contains(cleanSearch)) ||
                (x.FlatNumber != null && x.FlatNumber.ToLower().Contains(cleanSearch)) ||
                (x.ParkingSlot != null && x.ParkingSlot.ToLower().Contains(cleanSearch)) ||
                (buildingMap.ContainsKey(x.BuildingId) && buildingMap[x.BuildingId].ToLower().Contains(cleanSearch)) ||
                (ownerMap.ContainsKey(x.OwnerId) && ownerMap[x.OwnerId].ToLower().Contains(cleanSearch))
            );
        }

        var totalCount = query.Count();
        var pageEntities = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities
            .Select(x => new ApartmentViewModel
            {
                Id = x.Id,
                BuildingId = x.BuildingId,
                BuildingName = buildingMap.TryGetValue(x.BuildingId, out var bName) ? bName : "Unknown Building",
                OwnerId = x.OwnerId,
                OwnerName = ownerMap.TryGetValue(x.OwnerId, out var oName) ? oName : "Unknown Owner",
                NestawayId = x.NestawayId,
                FlatNumber = x.FlatNumber,
                Floor = x.Floor,
                ApartmentType = x.ApartmentType,
                AreaSqft = x.AreaSqft,
                Bedrooms = x.Bedrooms,
                Bathrooms = x.Bathrooms,
                HasBalcony = x.HasBalcony,
                ParkingSlot = x.ParkingSlot,
                ExpectedRent = x.ExpectedRent,
                MaintenanceCharge = x.MaintenanceCharge,
                WaterCharge = x.WaterCharge,
                Notes = x.Notes,
                CurrentTenantId = x.CurrentTenantId,
                CurrentTenantName = x.CurrentTenantId.HasValue && tenantMap.TryGetValue(x.CurrentTenantId.Value, out var tn) ? tn : null,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
                UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null,
            })
            .ToList();

        return new PaginatedList<ApartmentViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<byte[]> ExportToXlsx(
        string? search,
        Guid? buildingId,
        Guid? ownerId,
        string? apartmentType,
        string? status,
        CancellationToken cancellationToken)
    {
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var owners = await _unitOfWork.OwnerRepository.GetAllAsync();

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var ownerMap = owners.ToDictionary(o => o.Id, o => o.FullName);

        var query = apartments.AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (ownerId.HasValue)
        {
            query = query.Where(x => x.OwnerId == ownerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(apartmentType))
        {
            query = query.Where(x => x.ApartmentType == apartmentType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var cleanStatus = status.Trim().ToLower();
            if (cleanStatus == "occupied")
            {
                query = query.Where(x => x.CurrentTenantId != null);
            }
            else if (cleanStatus == "vacant")
            {
                query = query.Where(x => x.CurrentTenantId == null);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(x =>
                (x.NestawayId != null && x.NestawayId.ToLower().Contains(cleanSearch)) ||
                (x.FlatNumber != null && x.FlatNumber.ToLower().Contains(cleanSearch)) ||
                (x.ParkingSlot != null && x.ParkingSlot.ToLower().Contains(cleanSearch)) ||
                (buildingMap.ContainsKey(x.BuildingId) && buildingMap[x.BuildingId].ToLower().Contains(cleanSearch)) ||
                (ownerMap.ContainsKey(x.OwnerId) && ownerMap[x.OwnerId].ToLower().Contains(cleanSearch))
            );
        }

        var list = query.ToList();

        var rows = new List<List<string>>();
        var headers = new List<string> { "BuildingName", "OwnerName", "NestawayId", "FlatNumber", "Floor", "ApartmentType", "AreaSqft", "Bedrooms", "Bathrooms", "HasBalcony", "ParkingSlot", "ExpectedRent", "MaintenanceCharge", "WaterCharge", "Notes" };
        rows.Add(headers);

        foreach (var a in list)
        {
            rows.Add(new List<string>
            {
                buildingMap.TryGetValue(a.BuildingId, out var buildingName) ? buildingName : string.Empty,
                ownerMap.TryGetValue(a.OwnerId, out var ownerName) ? ownerName : string.Empty,
                a.NestawayId ?? string.Empty,
                a.FlatNumber ?? string.Empty,
                a.Floor.ToString(System.Globalization.CultureInfo.InvariantCulture),
                a.ApartmentType ?? string.Empty,
                a.AreaSqft.ToString(System.Globalization.CultureInfo.InvariantCulture),
                a.Bedrooms.ToString(System.Globalization.CultureInfo.InvariantCulture),
                a.Bathrooms.ToString(System.Globalization.CultureInfo.InvariantCulture),
                a.HasBalcony.ToString().ToLowerInvariant(),
                a.ParkingSlot ?? string.Empty,
                a.ExpectedRent.ToString(System.Globalization.CultureInfo.InvariantCulture),
                a.MaintenanceCharge.ToString(System.Globalization.CultureInfo.InvariantCulture),
                a.WaterCharge.ToString(System.Globalization.CultureInfo.InvariantCulture),
                a.Notes ?? string.Empty
            });
        }

        return CleanArchitecture.Application.Common.Utilities.XlsxHelper.BuildXlsx(rows);
    }

    public async Task<ApartmentViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw ApartmentException.NotFoundException("The specified apartment does not exist.");

        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == apartment.BuildingId);
        var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == apartment.OwnerId);
        var currentTenant = apartment.CurrentTenantId.HasValue ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == apartment.CurrentTenantId.Value) : null;
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        return new ApartmentViewModel
        {
            Id = apartment.Id,
            BuildingId = apartment.BuildingId,
            BuildingName = building?.BuildingName ?? "Unknown Building",
            Building = building == null ? null : new BuildingViewModel
            {
                Id = building.Id,
                BuildingName = building.BuildingName,
                Address = building.Address,
                City = building.City,
                State = building.State,
                Country = building.Country,
                GoogleMapLink = building.GoogleMapLink,
                TotalFloors = building.TotalFloors,
                ParkingDetails = building.ParkingDetails,
                Status = building.Status,
                Description = building.Description,
                ImageUrl = building.ImageUrl,
                CreatedAt = building.CreatedAt,
                UpdatedAt = building.UpdatedAt,
                CreatedBy = building.CreatedBy.HasValue && userMap.TryGetValue(building.CreatedBy.Value, out var bcb) ? bcb : null,
                UpdatedBy = building.UpdatedBy.HasValue && userMap.TryGetValue(building.UpdatedBy.Value, out var bub) ? bub : null
            },
            OwnerId = apartment.OwnerId,
            OwnerName = owner?.FullName ?? "Unknown Owner",
            Owner = owner == null ? null : new OwnerViewModel
            {
                Id = owner.Id,
                FullName = owner.FullName,
                Phone = owner.Phone,
                Email = owner.Email,
                City = owner.City,
                Address = owner.Address,
                IdType = owner.IdType,
                IdNumber = owner.IdNumber,
                BankName = owner.BankName,
                AccountNumber = owner.AccountNumber,
                IfscCode = owner.IfscCode,
                Status = owner.Status,
                Notes = owner.Notes,
                CreatedAt = owner.CreatedAt,
                UpdatedAt = owner.UpdatedAt,
                CreatedBy = owner.CreatedBy.HasValue && userMap.TryGetValue(owner.CreatedBy.Value, out var ocb) ? ocb : null,
                UpdatedBy = owner.UpdatedBy.HasValue && userMap.TryGetValue(owner.UpdatedBy.Value, out var oub) ? oub : null
            },
            NestawayId = apartment.NestawayId,
            FlatNumber = apartment.FlatNumber,
            Floor = apartment.Floor,
            ApartmentType = apartment.ApartmentType,
            AreaSqft = apartment.AreaSqft,
            Bedrooms = apartment.Bedrooms,
            Bathrooms = apartment.Bathrooms,
            HasBalcony = apartment.HasBalcony,
            ParkingSlot = apartment.ParkingSlot,
            ExpectedRent = apartment.ExpectedRent,
            MaintenanceCharge = apartment.MaintenanceCharge,
            WaterCharge = apartment.WaterCharge,
            Notes = apartment.Notes,
            CurrentTenantId = apartment.CurrentTenantId,
            CurrentTenantName = currentTenant?.FullName,
            CreatedAt = apartment.CreatedAt,
            UpdatedAt = apartment.UpdatedAt,
            CreatedBy = apartment.CreatedBy.HasValue && userMap.TryGetValue(apartment.CreatedBy.Value, out var acb) ? acb : null,
            UpdatedBy = apartment.UpdatedBy.HasValue && userMap.TryGetValue(apartment.UpdatedBy.Value, out var aub) ? aub : null,
        };
    }

    public async Task Create(ApartmentCreateRequest request, CancellationToken cancellationToken)
    {
        // Validate Building existence
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId);
        if (!buildingExists)
        {
            throw ApartmentException.BadRequestException("The specified building does not exist.");
        }

        // Validate Owner existence
        var ownerExists = await _unitOfWork.OwnerRepository.AnyAsync(x => x.Id == request.OwnerId);
        if (!ownerExists)
        {
            throw ApartmentException.BadRequestException("The specified owner does not exist.");
        }

        // Validate unique flat_number per building
        var isFlatExist = await _unitOfWork.ApartmentRepository.AnyIncludingDeletedAsync(x => x.BuildingId == request.BuildingId && x.FlatNumber.ToLower() == request.FlatNumber.Trim().ToLower());
        if (isFlatExist)
        {
            throw ApartmentException.BadRequestException($"Flat number '{request.FlatNumber}' already exists in this building.");
        }

        // Validate unique Nestaway ID (case-insensitive)
        var normalizedNestawayId = request.NestawayId?.Trim().ToLowerInvariant() ?? string.Empty;
        var isNestawayExist = await _unitOfWork.ApartmentRepository.AnyIncludingDeletedAsync(x => x.NestawayId.Trim().ToLower() == normalizedNestawayId);
        if (isNestawayExist)
        {
            throw ApartmentException.BadRequestException($"Nestaway ID '{request.NestawayId}' already exists.");
        }

        var apartment = new Apartment
        {
            Id = Guid.NewGuid(),
            BuildingId = request.BuildingId,
            OwnerId = request.OwnerId,
            NestawayId = request.NestawayId.Trim(),
            FlatNumber = request.FlatNumber.Trim(),
            Floor = request.Floor,
            ApartmentType = request.ApartmentType,
            AreaSqft = request.AreaSqft ?? 0,
            Bedrooms = request.Bedrooms ?? 0,
            Bathrooms = request.Bathrooms ?? 0,
            HasBalcony = request.HasBalcony,
            ParkingSlot = request.ParkingSlot?.Trim() ?? string.Empty,
            ExpectedRent = request.ExpectedRent,
            MaintenanceCharge = request.MaintenanceCharge,
            WaterCharge = request.WaterCharge,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.ApartmentRepository.AddAsync(apartment), cancellationToken);

        await _activityService.CreateActivity("Create", "Apartment", apartment.Id, apartment.BuildingId, $"Apartment '{apartment.FlatNumber}' added to building.", cancellationToken);

        var createdBuilding = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == apartment.BuildingId);
        await _notificationService.CreateNotificationInternal("properties", "Apartment Added", $"Apartment '{apartment.FlatNumber}' added to '{createdBuilding?.BuildingName ?? "building"}' building.", cancellationToken);
    }

    public async Task Update(Guid id, ApartmentUpdateRequest request, CancellationToken cancellationToken)
    {
        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw ApartmentException.NotFoundException("The specified apartment does not exist.");

        // Validate Building existence
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId);
        if (!buildingExists)
        {
            throw ApartmentException.BadRequestException("The specified building does not exist.");
        }

        // Validate Owner existence
        var ownerExists = await _unitOfWork.OwnerRepository.AnyAsync(x => x.Id == request.OwnerId);
        if (!ownerExists)
        {
            throw ApartmentException.BadRequestException("The specified owner does not exist.");
        }

        // Validate flat number uniqueness if modified
        if (apartment.BuildingId != request.BuildingId || !string.Equals(apartment.FlatNumber, request.FlatNumber, StringComparison.OrdinalIgnoreCase))
        {
            var isFlatExist = await _unitOfWork.ApartmentRepository.AnyIncludingDeletedAsync(x => x.BuildingId == request.BuildingId && x.FlatNumber.ToLower() == request.FlatNumber.Trim().ToLower() && x.Id != id);
            if (isFlatExist)
            {
                throw ApartmentException.BadRequestException($"Flat number '{request.FlatNumber}' already exists in this building.");
            }
        }

        // Validate Nestaway ID uniqueness if modified (case-insensitive)
        if (!string.Equals(apartment.NestawayId, request.NestawayId, StringComparison.OrdinalIgnoreCase))
        {
            var normalizedNestawayId = request.NestawayId?.Trim().ToLowerInvariant() ?? string.Empty;
            var isNestawayExist = await _unitOfWork.ApartmentRepository.AnyIncludingDeletedAsync(x => x.NestawayId.Trim().ToLower() == normalizedNestawayId && x.Id != id);
            if (isNestawayExist)
            {
                throw ApartmentException.BadRequestException($"Nestaway ID '{request.NestawayId}' already exists.");
            }
        }

        apartment.BuildingId = request.BuildingId;
        apartment.OwnerId = request.OwnerId;
        apartment.NestawayId = request.NestawayId.Trim();
        apartment.FlatNumber = request.FlatNumber.Trim();
        apartment.Floor = request.Floor;
        apartment.ApartmentType = request.ApartmentType;
        apartment.HasBalcony = request.HasBalcony;
        apartment.ExpectedRent = request.ExpectedRent;
        apartment.MaintenanceCharge = request.MaintenanceCharge;
        apartment.WaterCharge = request.WaterCharge;
        apartment.Notes = request.Notes;

        if (request.AreaSqft.HasValue) apartment.AreaSqft = request.AreaSqft.Value;
        if (request.Bedrooms.HasValue) apartment.Bedrooms = request.Bedrooms.Value;
        if (request.Bathrooms.HasValue) apartment.Bathrooms = request.Bathrooms.Value;
        if (request.ParkingSlot != null) apartment.ParkingSlot = request.ParkingSlot.Trim();

        apartment.UpdatedAt = DateTime.UtcNow;
        apartment.UpdatedBy = _currentUser.GetCurrentUserId();

        _unitOfWork.ApartmentRepository.Update(apartment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityService.CreateActivity("Update", "Apartment", apartment.Id, apartment.BuildingId, $"Apartment '{apartment.FlatNumber}' details were updated.", cancellationToken);
        await _notificationService.CreateNotificationInternal("properties", "Apartment Updated", $"Apartment '{apartment.FlatNumber}' details were updated.", cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw ApartmentException.NotFoundException("The specified apartment does not exist.");

        apartment.IsDeleted = true;
        apartment.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.ApartmentRepository.Update(apartment);

        // Record soft-delete history
        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "Apartment",
            EntityId = apartment.Id,
            EntityTitle = $"Flat {apartment.FlatNumber}-{apartment.NestawayId}-{apartment.CreatedAt}",
            DeletedBy = _currentUser.GetCurrentUserId(),
            DeletedAt = DateTime.UtcNow
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityService.CreateActivity("Delete", "Apartment", apartment.Id, apartment.BuildingId, $"Apartment '{apartment.FlatNumber}' was deleted.", cancellationToken);
        await _notificationService.CreateNotificationInternal("properties", "Apartment Deleted", $"Apartment '{apartment.FlatNumber}' was deleted.", cancellationToken);
    }
}
