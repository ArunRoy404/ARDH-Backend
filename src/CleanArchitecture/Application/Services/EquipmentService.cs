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

        var equipmentList = await _unitOfWork.EquipmentRepository.GetAllAsync(x => x.DeletedAt == null);
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync(x => x.DeletedAt == null);
        var vendors = await _unitOfWork.VendorRepository.GetAllAsync(x => x.DeletedAt == null);

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
            UpdatedAt = x.UpdatedAt
        }).ToList();

        return new PaginatedList<EquipmentViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<EquipmentViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw EquipmentException.NotFoundException($"Equipment with ID '{id}' was not found.");

        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == equipment.BuildingId && x.DeletedAt == null);
        var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == equipment.AmcVendorId && x.DeletedAt == null);

        return new EquipmentViewModel
        {
            Id = equipment.Id,
            BuildingId = equipment.BuildingId,
            BuildingName = building?.BuildingName ?? "Unknown Building",
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
            AmcExpiryDate = equipment.AmcExpiryDate,
            LastServiceDate = equipment.LastServiceDate,
            NextServiceDate = equipment.NextServiceDate,
            Status = equipment.Status,
            Notes = equipment.Notes,
            AttachmentUrl = equipment.AttachmentUrl,
            CreatedAt = equipment.CreatedAt,
            UpdatedAt = equipment.UpdatedAt
        };
    }

    public async Task Create(EquipmentCreateRequest request, CancellationToken cancellationToken)
    {
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId && x.DeletedAt == null);
        if (!buildingExists)
        {
            throw EquipmentException.BadRequestException("The specified building does not exist.");
        }

        var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.AmcVendorId && x.DeletedAt == null);
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
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.EquipmentRepository.AddAsync(equipment), cancellationToken);
    }

    public async Task Update(Guid id, EquipmentUpdateRequest request, CancellationToken cancellationToken)
    {
        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw EquipmentException.NotFoundException($"Equipment with ID '{id}' was not found.");

        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId && x.DeletedAt == null);
        if (!buildingExists)
        {
            throw EquipmentException.BadRequestException("The specified building does not exist.");
        }

        var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.AmcVendorId && x.DeletedAt == null);
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
        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw EquipmentException.NotFoundException($"Equipment with ID '{id}' was not found.");

        equipment.Status = request.Status;
        equipment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EquipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw EquipmentException.NotFoundException($"Equipment with ID '{id}' was not found.");

        var now = DateTime.UtcNow;
        equipment.DeletedAt = now;
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
