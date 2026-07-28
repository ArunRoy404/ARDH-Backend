using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Equipment;
using CleanArchitecture.Shared.Models.Building;
using CleanArchitecture.Shared.Models.Vendor;

namespace CleanArchitecture.Application.Services;

public class EquipmentService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IEquipmentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<PaginatedList<EquipmentViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        Guid? buildingId,
        EquipmentType? type,
        EquipmentStatus? status,
        Guid? amcVendorId,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var equipmentList = await _unitOfWork.EquipmentRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var vendors = await _unitOfWork.VendorRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var vendorMap = vendors.ToDictionary(v => v.Id, v => (v.Name, v.CompanyName));

        var query = equipmentList.AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (amcVendorId.HasValue)
        {
            query = query.Where(x => x.AmcVendorId == amcVendorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(searchLower) ||
                x.Brand.ToLower().Contains(searchLower) ||
                x.Model.ToLower().Contains(searchLower) ||
                x.SerialNumber.ToLower().Contains(searchLower) ||
                x.Notes.ToLower().Contains(searchLower) ||
                (buildingMap.ContainsKey(x.BuildingId) && buildingMap[x.BuildingId].ToLower().Contains(searchLower)) ||
                (vendorMap.ContainsKey(x.AmcVendorId) && (vendorMap[x.AmcVendorId].Name.ToLower().Contains(searchLower) || vendorMap[x.AmcVendorId].CompanyName.ToLower().Contains(searchLower))));
        }

        var totalItems = query.Count();
        var pageEntities = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => new EquipmentViewModel
        {
            Id = x.Id,
            BuildingId = x.BuildingId,
            BuildingName = buildingMap.TryGetValue(x.BuildingId, out var bName) ? bName : "Unknown Building",
            Name = x.Name,
            Type = x.Type,
            Brand = x.Brand,
            Model = x.Model,
            SerialNumber = x.SerialNumber,
            InstallDate = x.InstallDate,
            WarrantyExpiryDate = x.WarrantyExpiryDate,
            AmcVendorId = x.AmcVendorId,
            AmcVendorName = vendorMap.TryGetValue(x.AmcVendorId, out var vInfo) ? vInfo.Name : "Unknown Vendor",
            AmcVendorCompanyName = vendorMap.TryGetValue(x.AmcVendorId, out vInfo) ? vInfo.CompanyName : "Unknown Vendor",
            AmcExpiryDate = x.AmcExpiryDate,
            LastServiceDate = x.LastServiceDate,
            NextServiceDate = x.NextServiceDate,
            Status = x.Status,
            Notes = x.Notes,
            AttachmentUrl = x.AttachmentUrl,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
            UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null,
        }).ToList();

        return new PaginatedList<EquipmentViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<EquipmentViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw EquipmentException.NotFoundException($"Equipment with ID '{id}' was not found.");

        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == equipment.BuildingId);
        var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == equipment.AmcVendorId);
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        return new EquipmentViewModel
        {
            Id = equipment.Id,
            BuildingId = equipment.BuildingId,
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
            Name = equipment.Name,
            Type = equipment.Type,
            Brand = equipment.Brand,
            Model = equipment.Model,
            SerialNumber = equipment.SerialNumber,
            InstallDate = equipment.InstallDate,
            WarrantyExpiryDate = equipment.WarrantyExpiryDate,
            AmcVendorId = equipment.AmcVendorId,
            AmcVendorName = vendor?.Name ?? "Unknown Vendor",
            AmcVendorCompanyName = vendor?.CompanyName ?? "Unknown Vendor",
            AmcVendor = vendor == null ? null : new VendorViewModel
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
            AmcExpiryDate = equipment.AmcExpiryDate,
            LastServiceDate = equipment.LastServiceDate,
            NextServiceDate = equipment.NextServiceDate,
            Status = equipment.Status,
            Notes = equipment.Notes,
            AttachmentUrl = equipment.AttachmentUrl,
            CreatedAt = equipment.CreatedAt,
            UpdatedAt = equipment.UpdatedAt,
            CreatedBy = equipment.CreatedBy.HasValue && userMap.TryGetValue(equipment.CreatedBy.Value, out var ecb) ? ecb : null,
            UpdatedBy = equipment.UpdatedBy.HasValue && userMap.TryGetValue(equipment.UpdatedBy.Value, out var eub) ? eub : null,
        };
    }

    public async Task Create(EquipmentCreateRequest request, CancellationToken cancellationToken)
    {
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId);
        if (!buildingExists)
        {
            throw EquipmentException.BadRequestException("The specified building does not exist.");
        }

        var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.AmcVendorId);
        if (!vendorExists)
        {
            throw EquipmentException.BadRequestException("The specified AMC vendor does not exist.");
        }

        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            BuildingId = request.BuildingId,
            Name = request.Name.Trim(),
            Type = request.Type,
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            SerialNumber = request.SerialNumber.Trim(),
            InstallDate = request.InstallDate,
            WarrantyExpiryDate = request.WarrantyExpiryDate,
            AmcVendorId = request.AmcVendorId,
            AmcExpiryDate = request.AmcExpiryDate,
            LastServiceDate = request.LastServiceDate,
            NextServiceDate = request.NextServiceDate,
            Status = request.Status,
            Notes = request.Notes?.Trim() ?? string.Empty,
            AttachmentUrl = request.AttachmentUrl?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.EquipmentRepository.AddAsync(equipment), cancellationToken);
    }

    public async Task Update(Guid id, EquipmentUpdateRequest request, CancellationToken cancellationToken)
    {
        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw EquipmentException.NotFoundException($"Equipment with ID '{id}' was not found.");

        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId);
        if (!buildingExists)
        {
            throw EquipmentException.BadRequestException("The specified building does not exist.");
        }

        var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.AmcVendorId);
        if (!vendorExists)
        {
            throw EquipmentException.BadRequestException("The specified AMC vendor does not exist.");
        }

        equipment.BuildingId = request.BuildingId;
        equipment.Name = request.Name.Trim();
        equipment.Type = request.Type;
        equipment.Brand = request.Brand.Trim();
        equipment.Model = request.Model.Trim();
        equipment.SerialNumber = request.SerialNumber.Trim();
        equipment.InstallDate = request.InstallDate;
        equipment.WarrantyExpiryDate = request.WarrantyExpiryDate;
        equipment.AmcVendorId = request.AmcVendorId;
        equipment.AmcExpiryDate = request.AmcExpiryDate;
        equipment.LastServiceDate = request.LastServiceDate;
        equipment.NextServiceDate = request.NextServiceDate;
        equipment.Status = request.Status;
        equipment.Notes = request.Notes?.Trim() ?? string.Empty;
        equipment.AttachmentUrl = request.AttachmentUrl?.Trim() ?? string.Empty;
        equipment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EquipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatus(Guid id, EquipmentStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw EquipmentException.NotFoundException($"Equipment with ID '{id}' was not found.");

        equipment.Status = request.Status;
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.UpdatedBy = _currentUser.GetCurrentUserId();

        _unitOfWork.EquipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw EquipmentException.NotFoundException($"Equipment with ID '{id}' was not found.");

        var now = DateTime.UtcNow;

        equipment.IsDeleted = true;
        equipment.UpdatedAt = now;
        _unitOfWork.EquipmentRepository.Update(equipment);

        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "Equipment",
            EntityId = equipment.Id,
            EntityTitle = $"{equipment.Name} ({equipment.Brand} {equipment.Model})",
            DeletedBy = _currentUser.GetCurrentUserId(),
            DeletedAt = now
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
