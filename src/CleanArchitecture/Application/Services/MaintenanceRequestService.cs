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

namespace CleanArchitecture.Application.Services;

public class MaintenanceRequestService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IMaintenanceRequestService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

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
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var requests = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => x.DeletedAt == null);
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync(x => x.DeletedAt == null);
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => x.DeletedAt == null);
        var vendors = await _unitOfWork.VendorRepository.GetAllAsync(x => x.DeletedAt == null);
        var equipmentList = await _unitOfWork.EquipmentRepository.GetAllAsync(x => x.DeletedAt == null);

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
            CreatedBy = x.CreatedBy,
            UpdatedBy = x.UpdatedBy,
            DeletedBy = x.DeletedBy,
            RestoredBy = x.RestoredBy
        }).ToList();

        return new PaginatedList<MaintenanceRequestViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<MaintenanceRequestViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw MaintenanceRequestException.NotFoundException($"Maintenance request with ID '{id}' was not found.");

        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == request.BuildingId && x.DeletedAt == null);
        var apartment = request.ApartmentId.HasValue ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == request.ApartmentId.Value && x.DeletedAt == null) : null;
        var vendor = request.VendorId.HasValue ? await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == request.VendorId.Value && x.DeletedAt == null) : null;
        var equipment = request.EquipmentId.HasValue ? await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == request.EquipmentId.Value && x.DeletedAt == null) : null;

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
            EquipmentId = request.EquipmentId,
            EquipmentName = equipment?.Name,
            BuildingId = request.BuildingId,
            BuildingName = building?.BuildingName ?? "Unknown Building",
            ApartmentId = request.ApartmentId,
            FlatNumber = apartment?.FlatNumber,
            EstimatedCost = request.EstimatedCost,
            AnnualCost = request.AnnualCost,
            ScheduledDate = request.ScheduledDate,
            ReceiptAttachmentUrl = request.ReceiptAttachmentUrl,
            Notes = request.Notes,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            CreatedBy = request.CreatedBy,
            UpdatedBy = request.UpdatedBy,
            DeletedBy = request.DeletedBy,
            RestoredBy = request.RestoredBy
        };
    }

    public async Task Create(MaintenanceRequestCreateRequest request, CancellationToken cancellationToken)
    {
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId && x.DeletedAt == null);
        if (!buildingExists)
        {
            throw MaintenanceRequestException.BadRequestException("The specified building does not exist.");
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value && x.DeletedAt == null);
            if (!apartmentExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId.Value && x.DeletedAt == null);
            if (!vendorExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified vendor does not exist.");
            }
        }

        if (request.EquipmentId.HasValue)
        {
            var equipmentExists = await _unitOfWork.EquipmentRepository.AnyAsync(x => x.Id == request.EquipmentId.Value && x.DeletedAt == null);
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

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.MaintenanceRequestRepository.AddAsync(maintenanceRequest), cancellationToken);
    }

    public async Task Update(Guid id, MaintenanceRequestUpdateRequest request, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw MaintenanceRequestException.NotFoundException($"Maintenance request with ID '{id}' was not found.");

        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId && x.DeletedAt == null);
        if (!buildingExists)
        {
            throw MaintenanceRequestException.BadRequestException("The specified building does not exist.");
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value && x.DeletedAt == null);
            if (!apartmentExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId.Value && x.DeletedAt == null);
            if (!vendorExists)
            {
                throw MaintenanceRequestException.BadRequestException("The specified vendor does not exist.");
            }
        }

        if (request.EquipmentId.HasValue)
        {
            var equipmentExists = await _unitOfWork.EquipmentRepository.AnyAsync(x => x.Id == request.EquipmentId.Value && x.DeletedAt == null);
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

        _unitOfWork.MaintenanceRequestRepository.Update(maintenanceRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw MaintenanceRequestException.NotFoundException($"Maintenance request with ID '{id}' was not found.");

        var now = DateTime.UtcNow;
        var userId = _currentUser.GetCurrentUserId();

        maintenanceRequest.DeletedAt = now;
        maintenanceRequest.UpdatedAt = now;
        maintenanceRequest.DeletedBy = userId;

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
        var maintenanceRequest = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw MaintenanceRequestException.NotFoundException($"Maintenance request with ID '{id}' was not found.");

        var userId = _currentUser.GetCurrentUserId();

        maintenanceRequest.Status = request.Status;
        maintenanceRequest.UpdatedAt = DateTime.UtcNow;
        maintenanceRequest.UpdatedBy = userId;

        _unitOfWork.MaintenanceRequestRepository.Update(maintenanceRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Assign(Guid id, MaintenanceRequestAssignRequest request, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw MaintenanceRequestException.NotFoundException($"Maintenance request with ID '{id}' was not found.");

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId.Value && x.DeletedAt == null);
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
        var requests = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => x.DeletedAt == null);

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
