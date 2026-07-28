using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Maintenance;
using CleanArchitecture.Shared.Models.Building;
using CleanArchitecture.Shared.Models.Apartment;
using CleanArchitecture.Shared.Models.Vendor;
using CleanArchitecture.Shared.Models.Equipment;

namespace CleanArchitecture.Application.Services;

public class MaintenanceRequestService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    INotificationService notificationService,
    IActivityService activityService) : IMaintenanceRequestService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IActivityService _activityService = activityService;

    public async Task<PaginatedList<MaintenanceRequestViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        MaintenanceStatus? status,
        MaintenancePriority? priority,
        MaintenanceCategory? category,
        Guid? buildingId,
        Guid? vendorId,
        Guid? equipmentId,
        Guid? apartmentId,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var requests = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
        var vendors = await _unitOfWork.VendorRepository.GetAllAsync();
        var equipmentList = await _unitOfWork.EquipmentRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);
        var vendorMap = vendors.ToDictionary(v => v.Id, v => (v.Name, v.CompanyName));
        var equipmentMap = equipmentList.ToDictionary(e => e.Id, e => e.Name);

        var query = requests.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(x => x.Priority == priority.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(x => x.Category == category.Value);
        }

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (vendorId.HasValue)
        {
            query = query.Where(x => x.VendorId == vendorId.Value);
        }

        if (equipmentId.HasValue)
        {
            query = query.Where(x => x.EquipmentId == equipmentId.Value);
        }

        if (apartmentId.HasValue)
        {
            query = query.Where(x => x.ApartmentId == apartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(searchLower) ||
                x.Description.ToLower().Contains(searchLower) ||
                (x.Notes != null && x.Notes.ToLower().Contains(searchLower)) ||
                (buildingMap.ContainsKey(x.BuildingId) && buildingMap[x.BuildingId].ToLower().Contains(searchLower)) ||
                (x.ApartmentId.HasValue && apartmentMap.ContainsKey(x.ApartmentId.Value) && apartmentMap[x.ApartmentId.Value].ToLower().Contains(searchLower)) ||
                (x.VendorId.HasValue && vendorMap.ContainsKey(x.VendorId.Value) && (vendorMap[x.VendorId.Value].Name.ToLower().Contains(searchLower) || vendorMap[x.VendorId.Value].CompanyName.ToLower().Contains(searchLower))) ||
                (x.EquipmentId.HasValue && equipmentMap.ContainsKey(x.EquipmentId.Value) && equipmentMap[x.EquipmentId.Value].ToLower().Contains(searchLower))
            );
        }

        var totalItems = query.Count();
        var pageEntities = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => new MaintenanceRequestViewModel
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            Category = x.Category,
            Priority = x.Priority,
            Status = x.Status,
            VendorId = x.VendorId,
            VendorName = x.VendorId.HasValue && vendorMap.TryGetValue(x.VendorId.Value, out var vInfo) ? vInfo.Name : null,
            VendorCompanyName = x.VendorId.HasValue && vendorMap.TryGetValue(x.VendorId.Value, out vInfo) ? vInfo.CompanyName : null,
            EquipmentId = x.EquipmentId,
            EquipmentName = x.EquipmentId.HasValue && equipmentMap.TryGetValue(x.EquipmentId.Value, out var eName) ? eName : null,
            BuildingId = x.BuildingId,
            BuildingName = buildingMap.TryGetValue(x.BuildingId, out var bName) ? bName : "Unknown Building",
            ApartmentId = x.ApartmentId,
            FlatNumber = x.ApartmentId.HasValue && apartmentMap.TryGetValue(x.ApartmentId.Value, out var fNum) ? fNum : null,
            EstimatedCost = x.EstimatedCost,
            AnnualCost = x.AnnualCost,
            ScheduledDate = x.ScheduledDate,
            ReceiptAttachmentUrl = x.ReceiptAttachmentUrl,
            Notes = x.Notes,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
            UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null,
        }).ToList();

        return new PaginatedList<MaintenanceRequestViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<MaintenanceRequestViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw MaintenanceRequestException.NotFoundException($"Maintenance request with ID '{id}' was not found.");

        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == request.BuildingId);
        var apartment = request.ApartmentId.HasValue ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == request.ApartmentId.Value) : null;
        var vendor = request.VendorId.HasValue ? await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == request.VendorId.Value) : null;
        var equipment = request.EquipmentId.HasValue ? await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == request.EquipmentId.Value) : null;
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);
        var currentTenant = apartment != null && apartment.CurrentTenantId.HasValue ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == apartment.CurrentTenantId.Value) : null;

        return new MaintenanceRequestViewModel
        {
            Id = request.Id,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            Status = request.Status,
            VendorId = request.VendorId,
            VendorName = vendor?.Name,
            VendorCompanyName = vendor?.CompanyName,
            Vendor = vendor == null ? null : new VendorViewModel
            {
                Id = vendor.Id,
                Name = vendor.Name,
                CompanyName = vendor.CompanyName,
                Phone = vendor.Phone,
                Email = vendor.Email,
                VendorType = vendor.VendorType,
                GstNumber = vendor.GstNumber,
                Address = vendor.Address,
                Status = vendor.Status,
                Notes = vendor.Notes,
                CreatedAt = vendor.CreatedAt,
                UpdatedAt = vendor.UpdatedAt,
                CreatedBy = vendor.CreatedBy.HasValue && userMap.TryGetValue(vendor.CreatedBy.Value, out var vcb) ? vcb : null,
                UpdatedBy = vendor.UpdatedBy.HasValue && userMap.TryGetValue(vendor.UpdatedBy.Value, out var vub) ? vub : null
            },
            EquipmentId = request.EquipmentId,
            EquipmentName = equipment?.Name,
            Equipment = equipment == null ? null : new EquipmentViewModel
            {
                Id = equipment.Id,
                BuildingId = equipment.BuildingId,
                Name = equipment.Name,
                Type = equipment.Type,
                Brand = equipment.Brand,
                Model = equipment.Model,
                SerialNumber = equipment.SerialNumber,
                InstallDate = equipment.InstallDate,
                WarrantyExpiryDate = equipment.WarrantyExpiryDate,
                AmcVendorId = equipment.AmcVendorId,
                AmcExpiryDate = equipment.AmcExpiryDate,
                LastServiceDate = equipment.LastServiceDate,
                NextServiceDate = equipment.NextServiceDate,
                Status = equipment.Status,
                Notes = equipment.Notes,
                AttachmentUrl = equipment.AttachmentUrl,
                CreatedAt = equipment.CreatedAt,
                UpdatedAt = equipment.UpdatedAt,
                CreatedBy = equipment.CreatedBy.HasValue && userMap.TryGetValue(equipment.CreatedBy.Value, out var ecb) ? ecb : null,
                UpdatedBy = equipment.UpdatedBy.HasValue && userMap.TryGetValue(equipment.UpdatedBy.Value, out var eub) ? eub : null
            },
            BuildingId = request.BuildingId,
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
            ApartmentId = request.ApartmentId,
            FlatNumber = apartment?.FlatNumber,
            Apartment = apartment == null ? null : new ApartmentViewModel
            {
                Id = apartment.Id,
                BuildingId = apartment.BuildingId,
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
                WaterCharge = apartment.WaterCharge,                CurrentTenantId = apartment.CurrentTenantId,
                CurrentTenantName = currentTenant?.FullName,
                CreatedAt = apartment.CreatedAt,
                UpdatedAt = apartment.UpdatedAt,
                CreatedBy = apartment.CreatedBy.HasValue && userMap.TryGetValue(apartment.CreatedBy.Value, out var acb) ? acb : null,
                UpdatedBy = apartment.UpdatedBy.HasValue && userMap.TryGetValue(apartment.UpdatedBy.Value, out var aub) ? aub : null
            },
            EstimatedCost = request.EstimatedCost,
            AnnualCost = request.AnnualCost,
            ScheduledDate = request.ScheduledDate,
            ReceiptAttachmentUrl = request.ReceiptAttachmentUrl,
            Notes = request.Notes,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            CreatedBy = request.CreatedBy.HasValue && userMap.TryGetValue(request.CreatedBy.Value, out var rcb) ? rcb : null,
            UpdatedBy = request.UpdatedBy.HasValue && userMap.TryGetValue(request.UpdatedBy.Value, out var rub) ? rub : null,
        };
    }

    public async Task Create(MaintenanceRequestCreateRequest request, CancellationToken cancellationToken)
    {
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId);
        if (!buildingExists)
        {
            throw MaintenanceRequestException.BadRequestException("The specified building does not exist.");
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value);
            if (!apartmentExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId.Value);
            if (!vendorExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified vendor does not exist.");
            }
        }

        if (request.EquipmentId.HasValue)
        {
            var equipmentExists = await _unitOfWork.EquipmentRepository.AnyAsync(x => x.Id == request.EquipmentId.Value);
            if (!equipmentExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified equipment does not exist.");
            }
        }

        var userId = _currentUser.GetCurrentUserId();

        var maintenanceRequest = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category,
            Priority = request.Priority,
            Status = request.Status,
            VendorId = request.VendorId,
            EquipmentId = request.EquipmentId,
            BuildingId = request.BuildingId,
            ApartmentId = request.ApartmentId,
            EstimatedCost = request.EstimatedCost,
            AnnualCost = request.AnnualCost,
            ScheduledDate = request.ScheduledDate,
            ReceiptAttachmentUrl = request.ReceiptAttachmentUrl?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        Equipment? equipmentToUpdate = null;
        if (maintenanceRequest.EquipmentId.HasValue &&
            (maintenanceRequest.Status == MaintenanceStatus.Open || maintenanceRequest.Status == MaintenanceStatus.InProgress))
        {
            equipmentToUpdate = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == maintenanceRequest.EquipmentId.Value);
            if (equipmentToUpdate != null)
            {
                equipmentToUpdate.Status = EquipmentStatus.UnderMaintenance;
                equipmentToUpdate.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            await _unitOfWork.MaintenanceRequestRepository.AddAsync(maintenanceRequest);
            if (equipmentToUpdate != null)
            {
                _unitOfWork.EquipmentRepository.Update(equipmentToUpdate);
            }
        }, cancellationToken);

        await _notificationService.CreateNotificationInternal(
            "operations",
            "New Maintenance Request",
            $"A new maintenance request '{maintenanceRequest.Title}' has been created with status '{maintenanceRequest.Status}'.",
            cancellationToken);

        var apartment = maintenanceRequest.ApartmentId.HasValue
            ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == maintenanceRequest.ApartmentId.Value)
            : null;
        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == maintenanceRequest.BuildingId);
        var locationStr = apartment != null
            ? $"Flat {apartment.FlatNumber} at {building?.BuildingName ?? "building"}"
            : (building?.BuildingName ?? "Building");

        await _activityService.CreateActivity(
            "Create",
            "MaintenanceRequest",
            maintenanceRequest.Id,
            maintenanceRequest.BuildingId,
            $"{locationStr} marked for maintenance.",
            cancellationToken);
    }

    public async Task Update(Guid id, MaintenanceRequestUpdateRequest request, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw MaintenanceRequestException.NotFoundException($"Maintenance request with ID '{id}' was not found.");

        var oldStatus = maintenanceRequest.Status;
        var oldEquipmentId = maintenanceRequest.EquipmentId;

        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId);
        if (!buildingExists)
        {
            throw MaintenanceRequestException.BadRequestException("The specified building does not exist.");
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value);
            if (!apartmentExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId.Value);
            if (!vendorExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified vendor does not exist.");
            }
        }

        if (request.EquipmentId.HasValue)
        {
            var equipmentExists = await _unitOfWork.EquipmentRepository.AnyAsync(x => x.Id == request.EquipmentId.Value);
            if (!equipmentExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified equipment does not exist.");
            }
        }

        var userId = _currentUser.GetCurrentUserId();

        maintenanceRequest.Title = request.Title.Trim();
        maintenanceRequest.Description = request.Description.Trim();
        maintenanceRequest.Category = request.Category;
        maintenanceRequest.Priority = request.Priority;
        maintenanceRequest.Status = request.Status;
        maintenanceRequest.BuildingId = request.BuildingId;
        maintenanceRequest.ApartmentId = request.ApartmentId;
        maintenanceRequest.VendorId = request.VendorId;
        maintenanceRequest.EquipmentId = request.EquipmentId;
        maintenanceRequest.EstimatedCost = request.EstimatedCost;
        maintenanceRequest.AnnualCost = request.AnnualCost;
        maintenanceRequest.ScheduledDate = request.ScheduledDate;
        maintenanceRequest.ReceiptAttachmentUrl = request.ReceiptAttachmentUrl?.Trim();
        maintenanceRequest.Notes = request.Notes?.Trim();
        maintenanceRequest.UpdatedAt = DateTime.UtcNow;
        maintenanceRequest.UpdatedBy = userId;

        if (oldEquipmentId != request.EquipmentId)
        {
            if (oldEquipmentId.HasValue)
            {
                var oldEq = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == oldEquipmentId.Value);
                if (oldEq != null)
                {
                    oldEq.Status = EquipmentStatus.Operational;
                    oldEq.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.EquipmentRepository.Update(oldEq);
                }
            }

            if (request.EquipmentId.HasValue)
            {
                var newEq = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == request.EquipmentId.Value);
                if (newEq != null)
                {
                    newEq.Status = (request.Status == MaintenanceStatus.Open || request.Status == MaintenanceStatus.InProgress)
                        ? EquipmentStatus.UnderMaintenance
                        : EquipmentStatus.Operational;
                    newEq.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.EquipmentRepository.Update(newEq);
                }
            }
        }
        else if (request.EquipmentId.HasValue && oldStatus != request.Status)
        {
            var eq = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == request.EquipmentId.Value);
            if (eq != null)
            {
                eq.Status = (request.Status == MaintenanceStatus.Open || request.Status == MaintenanceStatus.InProgress)
                    ? EquipmentStatus.UnderMaintenance
                    : EquipmentStatus.Operational;
                eq.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.EquipmentRepository.Update(eq);
            }
        }

        _unitOfWork.MaintenanceRequestRepository.Update(maintenanceRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (oldStatus != request.Status)
        {
            await _notificationService.CreateNotificationInternal(
                "operations",
                "Maintenance Request Status Updated",
                $"Request '{maintenanceRequest.Title}' status changed to '{request.Status}'.",
                cancellationToken);

            var isCritical = maintenanceRequest.Priority == MaintenancePriority.Critical;
            var prefix = isCritical ? "Critical: " : "";
            var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == maintenanceRequest.BuildingId);
            var bldName = building?.BuildingName ?? "building";

            await _activityService.CreateActivity(
                "StatusChange",
                "MaintenanceRequest",
                maintenanceRequest.Id,
                maintenanceRequest.BuildingId,
                $"{prefix}'{maintenanceRequest.Title}' status changed to '{request.Status}' at {bldName}.",
                cancellationToken);
        }
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw MaintenanceRequestException.NotFoundException($"Maintenance request with ID '{id}' was not found.");

        var now = DateTime.UtcNow;
        var userId = _currentUser.GetCurrentUserId();

        if (maintenanceRequest.EquipmentId.HasValue &&
            (maintenanceRequest.Status == MaintenanceStatus.Open || maintenanceRequest.Status == MaintenanceStatus.InProgress))
        {
            var eq = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == maintenanceRequest.EquipmentId.Value);
            if (eq != null)
            {
                eq.Status = EquipmentStatus.Operational;
                eq.UpdatedAt = now;
                _unitOfWork.EquipmentRepository.Update(eq);
            }
        }

        maintenanceRequest.IsDeleted = true;
        maintenanceRequest.UpdatedAt = now;
        _unitOfWork.MaintenanceRequestRepository.Update(maintenanceRequest);

        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "MaintenanceRequest",
            EntityId = maintenanceRequest.Id,
            EntityTitle = $"{maintenanceRequest.Title} ({maintenanceRequest.Category})",
            DeletedBy = userId,
            DeletedAt = now
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatus(Guid id, MaintenanceRequestStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw MaintenanceRequestException.NotFoundException($"Maintenance request with ID '{id}' was not found.");

        var userId = _currentUser.GetCurrentUserId();

        maintenanceRequest.Status = request.Status;
        maintenanceRequest.UpdatedAt = DateTime.UtcNow;
        maintenanceRequest.UpdatedBy = userId;

        if (maintenanceRequest.EquipmentId.HasValue)
        {
            var eq = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == maintenanceRequest.EquipmentId.Value);
            if (eq != null)
            {
                eq.Status = (request.Status == MaintenanceStatus.Open || request.Status == MaintenanceStatus.InProgress)
                    ? EquipmentStatus.UnderMaintenance
                    : EquipmentStatus.Operational;
                eq.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.EquipmentRepository.Update(eq);
            }
        }

        _unitOfWork.MaintenanceRequestRepository.Update(maintenanceRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Assign(Guid id, MaintenanceRequestAssignRequest request, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw MaintenanceRequestException.NotFoundException($"Maintenance request with ID '{id}' was not found.");

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId.Value);
            if (!vendorExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified vendor does not exist.");
            }
        }

        var userId = _currentUser.GetCurrentUserId();

        maintenanceRequest.VendorId = request.VendorId;
        maintenanceRequest.UpdatedAt = DateTime.UtcNow;
        maintenanceRequest.UpdatedBy = userId;

        _unitOfWork.MaintenanceRequestRepository.Update(maintenanceRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MaintenanceRequestStatsViewModel> GetStats(CancellationToken cancellationToken)
    {
        var requests = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync();

        var openCount = requests.Count(x => x.Status == MaintenanceStatus.Open);
        var inProgressCount = requests.Count(x => x.Status == MaintenanceStatus.InProgress);
        var completeCount = requests.Count(x => x.Status == MaintenanceStatus.Complete);
        var cancelledCount = requests.Count(x => x.Status == MaintenanceStatus.Cancelled);
        var totalCount = requests.Count;

        return new MaintenanceRequestStatsViewModel
        {
            OpenCount = openCount,
            InProgressCount = inProgressCount,
            CompleteCount = completeCount,
            CancelledCount = cancelledCount,
            TotalCount = totalCount
        };
    }
}
