using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Equipment;
using CleanArchitecture.Shared.Models.Building;

namespace CleanArchitecture.Application.Services;

public class EquipmentService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    INotificationService notificationService) : IEquipmentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<PaginatedList<EquipmentViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        Guid? buildingId,
        string? type,
        string? status,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var equipmentList = await _unitOfWork.EquipmentRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);

        var query = equipmentList.AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var typeLower = type.Trim().ToLower();
            query = query.Where(x => x.Type.ToLower() == typeLower);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusLower = status.Trim().ToLower();
            query = query.Where(x => x.Status.ToLower() == statusLower);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(searchLower) ||
                x.Brand.ToLower().Contains(searchLower) ||
                (x.Model != null && x.Model.ToLower().Contains(searchLower)) ||
                (x.SerialNumber != null && x.SerialNumber.ToLower().Contains(searchLower)) ||
                (x.Notes != null && x.Notes.ToLower().Contains(searchLower)) ||
                (buildingMap.ContainsKey(x.BuildingId) && buildingMap[x.BuildingId].ToLower().Contains(searchLower)));
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

    public async Task<byte[]> ExportToCsv(
        string? search,
        Guid? buildingId,
        string? type,
        string? status,
        CancellationToken cancellationToken)
    {
        var equipmentList = await _unitOfWork.EquipmentRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);

        var query = equipmentList.AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var typeLower = type.Trim().ToLower();
            query = query.Where(x => x.Type.ToLower() == typeLower);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusLower = status.Trim().ToLower();
            query = query.Where(x => x.Status.ToLower() == statusLower);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(searchLower) ||
                x.Brand.ToLower().Contains(searchLower) ||
                (x.Model != null && x.Model.ToLower().Contains(searchLower)) ||
                (x.SerialNumber != null && x.SerialNumber.ToLower().Contains(searchLower)) ||
                (x.Notes != null && x.Notes.ToLower().Contains(searchLower)) ||
                (buildingMap.ContainsKey(x.BuildingId) && buildingMap[x.BuildingId].ToLower().Contains(searchLower)));
        }

        var list = query.OrderByDescending(x => x.CreatedAt).ToList();

        var csv = new StringBuilder();
        csv.AppendLine("ID,BuildingName,Name,Type,Brand,Model,SerialNumber,InstallDate,WarrantyExpiryDate,Status,Notes,AttachmentUrl,CreatedAt");

        foreach (var e in list)
        {
            var bName = buildingMap.TryGetValue(e.BuildingId, out var bn) ? bn : "";

            csv.AppendLine($"\"{e.Id}\",\"{EscapeCsv(bName)}\",\"{EscapeCsv(e.Name)}\",\"{EscapeCsv(e.Type)}\",\"{EscapeCsv(e.Brand)}\",\"{EscapeCsv(e.Model)}\",\"{EscapeCsv(e.SerialNumber)}\",\"{e.InstallDate:yyyy-MM-dd}\",\"{e.WarrantyExpiryDate:yyyy-MM-dd}\",\"{EscapeCsv(e.Status)}\",\"{EscapeCsv(e.Notes)}\",\"{EscapeCsv(e.AttachmentUrl)}\",\"{e.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static string EscapeCsv(string? val)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Replace("\"", "\"\"");
    }

    public async Task<EquipmentViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw EquipmentException.NotFoundException($"Equipment with ID '{id}' was not found.");

        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == equipment.BuildingId);
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

        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            BuildingId = request.BuildingId,
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Brand = request.Brand.Trim(),
            Model = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim(),
            SerialNumber = string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim(),
            InstallDate = request.InstallDate,
            WarrantyExpiryDate = request.WarrantyExpiryDate,
            Status = request.Status.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            AttachmentUrl = string.IsNullOrWhiteSpace(request.AttachmentUrl) ? null : request.AttachmentUrl.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.EquipmentRepository.AddAsync(equipment), cancellationToken);

        var equipBuilding = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == equipment.BuildingId);
        await _notificationService.CreateNotificationInternal("operations", "Equipment Added", $"Equipment '{equipment.Name}' added to '{equipBuilding?.BuildingName ?? "building"}' building.", cancellationToken);
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

        equipment.BuildingId = request.BuildingId;
        equipment.Name = request.Name.Trim();
        equipment.Type = request.Type.Trim();
        equipment.Brand = request.Brand.Trim();
        equipment.Model = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim();
        equipment.SerialNumber = string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim();
        equipment.InstallDate = request.InstallDate;
        equipment.WarrantyExpiryDate = request.WarrantyExpiryDate;
        equipment.Status = request.Status.Trim();
        equipment.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        equipment.AttachmentUrl = string.IsNullOrWhiteSpace(request.AttachmentUrl) ? null : request.AttachmentUrl.Trim();
        equipment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EquipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationInternal("operations", "Equipment Updated", $"Equipment '{equipment.Name}' details were updated.", cancellationToken);
    }

    public async Task UpdateStatus(Guid id, EquipmentStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw EquipmentException.NotFoundException($"Equipment with ID '{id}' was not found.");

        equipment.Status = request.Status.Trim();
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.UpdatedBy = _currentUser.GetCurrentUserId();

        _unitOfWork.EquipmentRepository.Update(equipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationInternal("operations", "Equipment Status Changed", $"Equipment '{equipment.Name}' status changed to '{request.Status}'.", cancellationToken);
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
            EntityTitle = $"{equipment.Name} ({equipment.Brand})",
            DeletedBy = _currentUser.GetCurrentUserId(),
            DeletedAt = now
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationInternal("operations", "Equipment Deleted", $"Equipment '{equipment.Name}' was deleted.", cancellationToken);
    }
}
