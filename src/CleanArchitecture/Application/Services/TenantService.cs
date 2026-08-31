using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Tenant;
using CleanArchitecture.Shared.Models.Building;
using CleanArchitecture.Shared.Models.Apartment;

namespace CleanArchitecture.Application.Services;

public class TenantService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IActivityService activityService, INotificationService notificationService) : ITenantService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IActivityService _activityService = activityService;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<PaginatedList<TenantViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        Guid? buildingId,
        Guid? apartmentId,
        TenantStatus? status,
        CancellationToken cancellationToken)
    {
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync(x =>
            (!buildingId.HasValue || x.BuildingId == buildingId.Value) &&
            (!apartmentId.HasValue || x.ApartmentId == apartmentId.Value) &&
            (!status.HasValue || x.Status == status.Value));

        var query = tenants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            var allBuildings = await _unitOfWork.BuildingRepository.GetAllAsync();
            var allApartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
            var bMap = allBuildings.ToDictionary(b => b.Id, b => b.BuildingName);
            var aMap = allApartments.ToDictionary(a => a.Id, a => (a.FlatNumber, a.NestawayId));

            query = query.Where(x =>
                x.FullName.ToLower().Contains(cleanSearch) ||
                x.Phone.ToLower().Contains(cleanSearch) ||
                x.Email.ToLower().Contains(cleanSearch) ||
                x.IdNumber.ToLower().Contains(cleanSearch) ||
                (bMap.ContainsKey(x.BuildingId) && bMap[x.BuildingId].ToLower().Contains(cleanSearch)) ||
                (aMap.ContainsKey(x.ApartmentId) && aMap[x.ApartmentId].FlatNumber.ToLower().Contains(cleanSearch))
            );
        }

        var totalCount = query.Count();
        var pageEntities = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Scope lookup maps to page entities
        var pageBuildingIds = pageEntities.Select(x => x.BuildingId).Distinct().ToList();
        var pageApartmentIds = pageEntities.Select(x => x.ApartmentId).Distinct().ToList();
        var pageUserIds = pageEntities.SelectMany(x => new[] { x.CreatedBy, x.UpdatedBy }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync(x => pageBuildingIds.Contains(x.Id));
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => pageApartmentIds.Contains(x.Id));
        var users = await _unitOfWork.UserRepository.GetAllAsync(x => pageUserIds.Contains(x.Id));

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => (a.FlatNumber, a.NestawayId));
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var items = pageEntities
            .Select(x => new TenantViewModel
            {
                Id = x.Id,
                BuildingId = x.BuildingId,
                BuildingName = buildingMap.TryGetValue(x.BuildingId, out var bName) ? bName : "Unknown Building",
                ApartmentId = x.ApartmentId,
                FlatNumber = apartmentMap.TryGetValue(x.ApartmentId, out var aInfo) ? aInfo.FlatNumber : "Unknown Flat",
                NestawayId = apartmentMap.TryGetValue(x.ApartmentId, out aInfo) ? aInfo.NestawayId : null,
                FullName = x.FullName,
                Phone = x.Phone,
                Email = x.Email,
                IdType = x.IdType,
                IdNumber = x.IdNumber,
                IdProofAttachmentUrl = x.IdProofAttachmentUrl,
                MoveInDate = x.MoveInDate,
                LeaseStartDate = x.LeaseStartDate,
                LeaseEndDate = x.LeaseEndDate,
                MonthlyRent = x.MonthlyRent,
                SecurityDeposit = x.SecurityDeposit,
                EmergencyContactName = x.EmergencyContactName,
                EmergencyContactPhone = x.EmergencyContactPhone,
                Status = x.Status,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
                UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null,
            })
            .ToList();

        return new PaginatedList<TenantViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<byte[]> ExportToXlsx(
        string? search,
        Guid? buildingId,
        Guid? apartmentId,
        TenantStatus? status,
        CancellationToken cancellationToken)
    {
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => (a.FlatNumber, a.NestawayId));

        var query = tenants.AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (apartmentId.HasValue)
        {
            query = query.Where(x => x.ApartmentId == apartmentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(cleanSearch) ||
                x.Phone.ToLower().Contains(cleanSearch) ||
                x.Email.ToLower().Contains(cleanSearch) ||
                x.IdNumber.ToLower().Contains(cleanSearch) ||
                (buildingMap.ContainsKey(x.BuildingId) && buildingMap[x.BuildingId].ToLower().Contains(cleanSearch)) ||
                (apartmentMap.ContainsKey(x.ApartmentId) && apartmentMap[x.ApartmentId].FlatNumber.ToLower().Contains(cleanSearch))
            );
        }

        var list = query.ToList();

        var rows = new List<List<string>>();
        var headers = new List<string> { "BuildingName", "FlatNumber", "FullName", "Phone", "Email", "IdType", "IdNumber", "IdProofAttachmentUrl", "MoveInDate", "LeaseStartDate", "LeaseEndDate", "MonthlyRent", "SecurityDeposit", "EmergencyContactName", "EmergencyContactPhone", "Status", "Notes" };
        rows.Add(headers);

        foreach (var t in list)
        {
            rows.Add(new List<string>
            {
                buildingMap.TryGetValue(t.BuildingId, out var buildingName) ? buildingName : string.Empty,
                apartmentMap.TryGetValue(t.ApartmentId, out var apartmentInfo) ? apartmentInfo.FlatNumber : string.Empty,
                t.FullName ?? string.Empty,
                t.Phone ?? string.Empty,
                t.Email ?? string.Empty,
                t.IdType.ToString(),
                t.IdNumber ?? string.Empty,
                t.IdProofAttachmentUrl ?? string.Empty,
                t.MoveInDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                t.LeaseStartDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                t.LeaseEndDate.HasValue ? t.LeaseEndDate.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : string.Empty,
                t.MonthlyRent.ToString(System.Globalization.CultureInfo.InvariantCulture),
                t.SecurityDeposit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                t.EmergencyContactName ?? string.Empty,
                t.EmergencyContactPhone ?? string.Empty,
                t.Status.ToString(),
                t.Notes ?? string.Empty
            });
        }

        return CleanArchitecture.Application.Common.Utilities.XlsxHelper.BuildXlsx(rows);
    }

    public async Task<TenantViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw TenantException.NotFoundException("The specified tenant does not exist.");

        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == tenant.BuildingId);
        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == tenant.ApartmentId);
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);
        var currentTenant = apartment != null && apartment.CurrentTenantId.HasValue ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == apartment.CurrentTenantId.Value) : null;

        return new TenantViewModel
        {
            Id = tenant.Id,
            BuildingId = tenant.BuildingId,
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
            ApartmentId = tenant.ApartmentId,
            FlatNumber = apartment?.FlatNumber ?? "Unknown Flat",
            Apartment = apartment == null ? null : new ApartmentViewModel
            {
                Id = apartment.Id,
                BuildingId = apartment.BuildingId,
                BuildingName = building?.BuildingName ?? "Unknown Building",
                OwnerId = apartment.OwnerId,
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
                CurrentTenantName = currentTenant?.FullName,
                CreatedAt = apartment.CreatedAt,
                UpdatedAt = apartment.UpdatedAt,
                CreatedBy = apartment.CreatedBy.HasValue && userMap.TryGetValue(apartment.CreatedBy.Value, out var acb) ? acb : null,
                UpdatedBy = apartment.UpdatedBy.HasValue && userMap.TryGetValue(apartment.UpdatedBy.Value, out var aub) ? aub : null
            },
            NestawayId = apartment?.NestawayId,
            FullName = tenant.FullName,
            Phone = tenant.Phone,
            Email = tenant.Email,
            IdType = tenant.IdType,
            IdNumber = tenant.IdNumber,
            IdProofAttachmentUrl = tenant.IdProofAttachmentUrl,
            MoveInDate = tenant.MoveInDate,
            LeaseStartDate = tenant.LeaseStartDate,
            LeaseEndDate = tenant.LeaseEndDate,
            MonthlyRent = tenant.MonthlyRent,
            SecurityDeposit = tenant.SecurityDeposit,
            EmergencyContactName = tenant.EmergencyContactName,
            EmergencyContactPhone = tenant.EmergencyContactPhone,
            Status = tenant.Status,
            Notes = tenant.Notes,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt,
            CreatedBy = tenant.CreatedBy.HasValue && userMap.TryGetValue(tenant.CreatedBy.Value, out var tcb) ? tcb : null,
            UpdatedBy = tenant.UpdatedBy.HasValue && userMap.TryGetValue(tenant.UpdatedBy.Value, out var tub) ? tub : null,
        };
    }

    public async Task Create(TenantCreateRequest request, CancellationToken cancellationToken)
    {
        // Validate Building
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId);
        if (!buildingExists)
        {
            throw TenantException.BadRequestException("The specified building does not exist.");
        }

        // Validate Apartment
        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == request.ApartmentId);
        if (apartment == null)
        {
            throw TenantException.BadRequestException("The specified apartment does not exist.");
        }

        // Validate apartment occupancy - reject only if this lease's own dates actually overlap
        // another active lease for the same apartment (not just "any current tenant"), so a
        // historical/backdated lease for an already-vacant period can still be recorded.
        var newTenantStatus = request.Status ?? TenantStatus.Active;
        if (newTenantStatus == TenantStatus.Active)
        {
            var hasOverlappingActiveLease = await _unitOfWork.TenantRepository.AnyAsync(x =>
                x.ApartmentId == request.ApartmentId &&
                x.Status == TenantStatus.Active &&
                x.LeaseStartDate <= (request.LeaseEndDate ?? DateTime.MaxValue) &&
                (x.LeaseEndDate == null || x.LeaseEndDate >= request.LeaseStartDate));

            if (hasOverlappingActiveLease)
            {
                throw TenantException.BadRequestException($"Apartment '{apartment.FlatNumber}' already has an active lease overlapping these dates. Please end the current tenancy before assigning a new one for this period.");
            }
        }

        // Validate Email uniqueness (only when provided)
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var isEmailExist = await _unitOfWork.TenantRepository.AnyIncludingDeletedAsync(x => x.Email.ToLower() == request.Email.Trim().ToLower());
            if (isEmailExist)
            {
                throw TenantException.BadRequestException($"Tenant with email '{request.Email}' already exists.");
            }
        }

        // Validate ID Number uniqueness
        var isIdNumberExist = await _unitOfWork.TenantRepository.AnyIncludingDeletedAsync(x => x.IdNumber.ToLower() == request.IdNumber.Trim().ToLower());
        if (isIdNumberExist)
        {
            throw TenantException.BadRequestException($"Tenant with ID number '{request.IdNumber}' already exists.");
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            BuildingId = request.BuildingId,
            ApartmentId = request.ApartmentId,
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email?.Trim() ?? string.Empty,
            IdType = request.IdType,
            IdNumber = request.IdNumber.Trim(),
            IdProofAttachmentUrl = request.IdProofAttachmentUrl?.Trim(),
            MoveInDate = request.MoveInDate,
            LeaseStartDate = request.LeaseStartDate,
            LeaseEndDate = request.LeaseEndDate,
            MonthlyRent = request.MonthlyRent,
            SecurityDeposit = request.SecurityDeposit ?? 0,
            EmergencyContactName = request.EmergencyContactName?.Trim(),
            EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
            Status = newTenantStatus,
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        var rentHistory = new TenantRentHistory
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            MonthlyRent = tenant.MonthlyRent,
            EffectiveFrom = tenant.LeaseStartDate,
            EffectiveTo = null,
            CreatedAt = DateTime.UtcNow
        };

        if (newTenantStatus == TenantStatus.Active)
        {
            apartment.CurrentTenantId = tenant.Id;
            apartment.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            await _unitOfWork.TenantRepository.AddAsync(tenant);
            await _unitOfWork.TenantRentHistoryRepository.AddAsync(rentHistory);
            _unitOfWork.ApartmentRepository.Update(apartment);
        }, cancellationToken);

        await _activityService.CreateActivity("Create", "Tenant", tenant.Id, tenant.BuildingId, $"Tenant '{tenant.FullName}' moved into Flat '{apartment.FlatNumber}'.", cancellationToken);

        await _notificationService.CreateNotificationInternal("properties", "Tenant Moved In", $"Tenant '{tenant.FullName}' moved into Flat '{apartment.FlatNumber}'.", cancellationToken);
    }

    public async Task Update(Guid id, TenantUpdateRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw TenantException.NotFoundException("The specified tenant does not exist.");

        // Validate Building
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId);
        if (!buildingExists)
        {
            throw TenantException.BadRequestException("The specified building does not exist.");
        }

        // Validate Apartment
        var requestedApartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == request.ApartmentId)
            ?? throw TenantException.BadRequestException("The specified apartment does not exist.");

        // Validate Email uniqueness if changed and provided
        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(tenant.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var isEmailExist = await _unitOfWork.TenantRepository.AnyIncludingDeletedAsync(x => x.Email.ToLower() == request.Email.Trim().ToLower() && x.Id != id);
            if (isEmailExist)
            {
                throw TenantException.BadRequestException($"Tenant with email '{request.Email}' already exists.");
            }
        }

        // Validate ID Number uniqueness if changed
        if (!string.Equals(tenant.IdNumber, request.IdNumber, StringComparison.OrdinalIgnoreCase))
        {
            var isIdNumberExist = await _unitOfWork.TenantRepository.AnyIncludingDeletedAsync(x => x.IdNumber.ToLower() == request.IdNumber.Trim().ToLower() && x.Id != id);
            if (isIdNumberExist)
            {
                throw TenantException.BadRequestException($"Tenant with ID number '{request.IdNumber}' already exists.");
            }
        }

        var oldApartmentId = tenant.ApartmentId;
        var oldStatus = tenant.Status;
        var newStatus = request.Status ?? oldStatus;

        // Validate apartment occupancy before applying any changes - reject only if this lease's
        // own dates actually overlap another active lease for the same apartment, not just
        // "any current tenant" (mirrors the same check in Create).
        if (newStatus == TenantStatus.Active)
        {
            var hasOverlappingActiveLease = await _unitOfWork.TenantRepository.AnyAsync(x =>
                x.Id != tenant.Id &&
                x.ApartmentId == request.ApartmentId &&
                x.Status == TenantStatus.Active &&
                x.LeaseStartDate <= (request.LeaseEndDate ?? DateTime.MaxValue) &&
                (x.LeaseEndDate == null || x.LeaseEndDate >= request.LeaseStartDate));

            if (hasOverlappingActiveLease)
            {
                throw TenantException.BadRequestException($"Apartment '{requestedApartment.FlatNumber}' already has an active lease overlapping these dates. Please end the current tenancy before assigning a new one for this period.");
            }
        }

        var oldMonthlyRent = tenant.MonthlyRent;

        tenant.BuildingId = request.BuildingId;
        tenant.ApartmentId = request.ApartmentId;
        tenant.FullName = request.FullName.Trim();
        tenant.Phone = request.Phone.Trim();
        if (!string.IsNullOrWhiteSpace(request.Email)) tenant.Email = request.Email.Trim();
        tenant.IdType = request.IdType;
        tenant.IdNumber = request.IdNumber.Trim();
        tenant.IdProofAttachmentUrl = request.IdProofAttachmentUrl?.Trim();
        tenant.MoveInDate = request.MoveInDate;
        tenant.LeaseStartDate = request.LeaseStartDate;
        tenant.LeaseEndDate = request.LeaseEndDate;
        tenant.MonthlyRent = request.MonthlyRent;
        if (request.SecurityDeposit.HasValue) tenant.SecurityDeposit = request.SecurityDeposit.Value;
        tenant.EmergencyContactName = request.EmergencyContactName?.Trim();
        tenant.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        tenant.Status = newStatus;
        tenant.Notes = request.Notes?.Trim();
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedBy = _currentUser.GetCurrentUserId();

        if (oldApartmentId != request.ApartmentId)
        {
            var oldApartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == oldApartmentId);
            if (oldApartment != null && oldApartment.CurrentTenantId == tenant.Id)
            {
                oldApartment.CurrentTenantId = null;
                oldApartment.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ApartmentRepository.Update(oldApartment);
            }

            if (newStatus == TenantStatus.Active)
            {
                requestedApartment.CurrentTenantId = tenant.Id;
                requestedApartment.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ApartmentRepository.Update(requestedApartment);
            }
        }
        else
        {
            if (oldStatus == TenantStatus.Active && newStatus != TenantStatus.Active)
            {
                if (requestedApartment.CurrentTenantId == tenant.Id)
                {
                    requestedApartment.CurrentTenantId = null;
                    requestedApartment.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.ApartmentRepository.Update(requestedApartment);
                }
            }
            else if (oldStatus != TenantStatus.Active && newStatus == TenantStatus.Active)
            {
                requestedApartment.CurrentTenantId = tenant.Id;
                requestedApartment.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ApartmentRepository.Update(requestedApartment);
            }
        }

        if (tenant.MonthlyRent != oldMonthlyRent)
        {
            var today = DateTime.UtcNow.Date;
            var openRentSegment = await _unitOfWork.TenantRentHistoryRepository.FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.EffectiveTo == null);

            if (openRentSegment == null)
            {
                // No history row exists yet (tenant predates this feature) - seed one from the lease's own start date.
                await _unitOfWork.TenantRentHistoryRepository.AddAsync(new TenantRentHistory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    MonthlyRent = tenant.MonthlyRent,
                    EffectiveFrom = tenant.LeaseStartDate,
                    EffectiveTo = null,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (openRentSegment.EffectiveFrom >= today)
            {
                // The current segment hasn't taken effect yet (future-dated lease) - nothing
                // historical to preserve, just update its rate in place.
                openRentSegment.MonthlyRent = tenant.MonthlyRent;
                _unitOfWork.TenantRentHistoryRepository.Update(openRentSegment);
            }
            else
            {
                openRentSegment.EffectiveTo = today.AddDays(-1);
                _unitOfWork.TenantRentHistoryRepository.Update(openRentSegment);

                await _unitOfWork.TenantRentHistoryRepository.AddAsync(new TenantRentHistory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    MonthlyRent = tenant.MonthlyRent,
                    EffectiveFrom = today,
                    EffectiveTo = null,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        _unitOfWork.TenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityService.CreateActivity("Update", "Tenant", tenant.Id, tenant.BuildingId, $"Tenant '{tenant.FullName}' details were updated.", cancellationToken);

        if (oldStatus != newStatus)
        {
            await _notificationService.CreateNotificationInternal("properties", "Tenant Status Changed", $"Tenant '{tenant.FullName}' status changed to '{newStatus}'.", cancellationToken);
        }
        else
        {
            await _notificationService.CreateNotificationInternal("properties", "Tenant Updated", $"Tenant '{tenant.FullName}' details were updated.", cancellationToken);
        }
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw TenantException.NotFoundException("The specified tenant does not exist.");

        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == tenant.ApartmentId);
        if (apartment != null && apartment.CurrentTenantId == tenant.Id)
        {
            apartment.CurrentTenantId = null;
            apartment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.ApartmentRepository.Update(apartment);
        }

        tenant.IsDeleted = true;
        tenant.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.TenantRepository.Update(tenant);

        // Record soft-delete history
        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "Tenant",
            EntityId = tenant.Id,
            EntityTitle = $"{tenant.FullName} ({tenant.Email})",
            DeletedBy = _currentUser.GetCurrentUserId(),
            DeletedAt = DateTime.UtcNow
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityService.CreateActivity("Delete", "Tenant", tenant.Id, tenant.BuildingId, $"Tenant '{tenant.FullName}' was deleted.", cancellationToken);

        await _notificationService.CreateNotificationInternal("properties", "Tenant Deleted", $"Tenant '{tenant.FullName}' was deleted.", cancellationToken);
    }
}
