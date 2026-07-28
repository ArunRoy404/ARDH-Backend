using System;
using System.Linq;
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

public class ApartmentService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IActivityService activityService) : IApartmentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IActivityService _activityService = activityService;

    public async Task<PaginatedList<ApartmentViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        Guid? buildingId,
        Guid? ownerId,
        string? apartmentType,
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
                CurrentTenantId = x.CurrentTenantId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy,
            })
            .ToList();

        return new PaginatedList<ApartmentViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<ApartmentViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw ApartmentException.NotFoundException("The specified apartment does not exist.");

        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == apartment.BuildingId);
        var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == apartment.OwnerId);

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
                CreatedBy = building.CreatedBy,
                UpdatedBy = building.UpdatedBy
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
                CreatedBy = owner.CreatedBy,
                UpdatedBy = owner.UpdatedBy
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
            CurrentTenantId = apartment.CurrentTenantId,
            CreatedAt = apartment.CreatedAt,
            UpdatedAt = apartment.UpdatedAt,
            CreatedBy = apartment.CreatedBy,
            UpdatedBy = apartment.UpdatedBy,
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
        var isFlatExist = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.BuildingId == request.BuildingId && x.FlatNumber.ToLower() == request.FlatNumber.Trim().ToLower());
        if (isFlatExist)
        {
            throw ApartmentException.BadRequestException($"Flat number '{request.FlatNumber}' already exists in this building.");
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
            AreaSqft = request.AreaSqft,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            HasBalcony = request.HasBalcony,
            ParkingSlot = request.ParkingSlot.Trim(),
            ExpectedRent = request.ExpectedRent,
            MaintenanceCharge = request.MaintenanceCharge,
            WaterCharge = request.WaterCharge,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.ApartmentRepository.AddAsync(apartment), cancellationToken);

        await _activityService.CreateActivity("Create", "Apartment", apartment.Id, apartment.BuildingId, $"Apartment '{apartment.FlatNumber}' added to building.", cancellationToken);
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
            var isFlatExist = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.BuildingId == request.BuildingId && x.FlatNumber.ToLower() == request.FlatNumber.Trim().ToLower() && x.Id != id);
            if (isFlatExist)
            {
                throw ApartmentException.BadRequestException($"Flat number '{request.FlatNumber}' already exists in this building.");
            }
        }

        apartment.BuildingId = request.BuildingId;
        apartment.OwnerId = request.OwnerId;
        apartment.NestawayId = request.NestawayId.Trim();
        apartment.FlatNumber = request.FlatNumber.Trim();
        apartment.Floor = request.Floor;
        apartment.ApartmentType = request.ApartmentType;
        apartment.AreaSqft = request.AreaSqft;
        apartment.Bedrooms = request.Bedrooms;
        apartment.Bathrooms = request.Bathrooms;
        apartment.HasBalcony = request.HasBalcony;
        apartment.ParkingSlot = request.ParkingSlot.Trim();
        apartment.ExpectedRent = request.ExpectedRent;
        apartment.MaintenanceCharge = request.MaintenanceCharge;
        apartment.WaterCharge = request.WaterCharge;
        apartment.UpdatedAt = DateTime.UtcNow;
        apartment.UpdatedBy = _currentUser.GetCurrentUserId();

        _unitOfWork.ApartmentRepository.Update(apartment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityService.CreateActivity("Update", "Apartment", apartment.Id, apartment.BuildingId, $"Apartment '{apartment.FlatNumber}' details were updated.", cancellationToken);
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
    }
}
