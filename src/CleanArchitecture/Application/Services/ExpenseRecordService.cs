using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Expenses;
using CleanArchitecture.Shared.Models.Vendor;
using CleanArchitecture.Shared.Models.Building;
using CleanArchitecture.Shared.Models.Apartment;

namespace CleanArchitecture.Application.Services;

public class ExpenseRecordService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IActivityService activityService,
    INotificationService notificationService) : IExpenseRecordService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IActivityService _activityService = activityService;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<PaginatedList<ExpenseRecordViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        ExpenseCategory? category,
        ExpenseStatus? status,
        ExpenseNature? nature,
        Guid? buildingId,
        Guid? vendorId,
        Guid? apartmentId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var records = await _unitOfWork.ExpenseRecordRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
        var vendors = await _unitOfWork.VendorRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);
        var vendorMap = vendors.ToDictionary(v => v.Id, v => (v.Name, v.CompanyName));

        var query = records.AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(x => x.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (nature.HasValue)
        {
            query = query.Where(x => x.Nature == nature.Value);
        }

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (vendorId.HasValue)
        {
            query = query.Where(x => x.VendorId == vendorId.Value);
        }

        if (apartmentId.HasValue)
        {
            query = query.Where(x => x.ApartmentId == apartmentId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                (x.ExpenseHead != null && x.ExpenseHead.ToLower().Contains(searchLower)) ||
                (x.SpecificItem != null && x.SpecificItem.ToLower().Contains(searchLower)) ||
                (x.Reference != null && x.Reference.ToLower().Contains(searchLower)) ||
                (x.Description != null && x.Description.ToLower().Contains(searchLower))
            );
        }

        var totalItems = query.Count();
        var pageEntities = query
            .OrderByDescending(x => x.ExpenseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => new ExpenseRecordViewModel
        {
            Id = x.Id,
            Category = x.Category,
            ExpenseHead = x.ExpenseHead,
            SpecificItem = x.SpecificItem,
            VendorId = x.VendorId,
            VendorName = x.VendorId.HasValue && vendorMap.TryGetValue(x.VendorId.Value, out var vInfo) ? vInfo.Name : null,
            VendorCompanyName = x.VendorId.HasValue && vendorMap.TryGetValue(x.VendorId.Value, out var vInfo2) ? vInfo2.CompanyName : null,
            Nature = x.Nature,
            Amount = x.Amount,
            Entity = x.Entity,
            BuildingId = x.BuildingId,
            BuildingName = x.BuildingId.HasValue && buildingMap.TryGetValue(x.BuildingId.Value, out var bName) ? bName : null,
            ApartmentId = x.ApartmentId,
            FlatNumber = x.ApartmentId.HasValue && apartmentMap.TryGetValue(x.ApartmentId.Value, out var fNum) ? fNum : null,
            ExpenseDate = x.ExpenseDate,
            PaymentMethod = x.PaymentMethod,
            Status = x.Status,
            Reference = x.Reference,
            AttachmentUrl = x.AttachmentUrl,
            Description = x.Description,
            TankerNumber = x.TankerNumber,
            TimeOfDelivery = x.TimeOfDelivery,
            DeliveryDriverName = x.DeliveryDriverName,
            ManagerInAttendance = x.ManagerInAttendance,
            LitersFilled = x.LitersFilled,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
            UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null,
        }).ToList();

        return new PaginatedList<ExpenseRecordViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<ExpenseRecordViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw ExpenseRecordException.NotFoundException($"Expense record with ID '{id}' was not found.");

        var building = record.BuildingId.HasValue ? await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == record.BuildingId.Value) : null;
        var apartment = record.ApartmentId.HasValue ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value) : null;
        var vendor = record.VendorId.HasValue ? await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == record.VendorId.Value) : null;
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);
        var currentTenant = apartment != null && apartment.CurrentTenantId.HasValue ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == apartment.CurrentTenantId.Value) : null;

        return new ExpenseRecordViewModel
        {
            Id = record.Id,
            Category = record.Category,
            ExpenseHead = record.ExpenseHead,
            SpecificItem = record.SpecificItem,
            VendorId = record.VendorId,
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
            Nature = record.Nature,
            Amount = record.Amount,
            Entity = record.Entity,
            BuildingId = record.BuildingId,
            BuildingName = building?.BuildingName,
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
            ApartmentId = record.ApartmentId,
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
                WaterCharge = apartment.WaterCharge,
                CurrentTenantId = apartment.CurrentTenantId,
                CurrentTenantName = currentTenant?.FullName,
                CreatedAt = apartment.CreatedAt,
                UpdatedAt = apartment.UpdatedAt,
                CreatedBy = apartment.CreatedBy.HasValue && userMap.TryGetValue(apartment.CreatedBy.Value, out var acb) ? acb : null,
                UpdatedBy = apartment.UpdatedBy.HasValue && userMap.TryGetValue(apartment.UpdatedBy.Value, out var aub) ? aub : null
            },
            ExpenseDate = record.ExpenseDate,
            PaymentMethod = record.PaymentMethod,
            Status = record.Status,
            Reference = record.Reference,
            AttachmentUrl = record.AttachmentUrl,
            Description = record.Description,
            TankerNumber = record.TankerNumber,
            TimeOfDelivery = record.TimeOfDelivery,
            DeliveryDriverName = record.DeliveryDriverName,
            ManagerInAttendance = record.ManagerInAttendance,
            LitersFilled = record.LitersFilled,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            CreatedBy = record.CreatedBy.HasValue && userMap.TryGetValue(record.CreatedBy.Value, out var rcb) ? rcb : null,
            UpdatedBy = record.UpdatedBy.HasValue && userMap.TryGetValue(record.UpdatedBy.Value, out var rub) ? rub : null,
        };
    }

    public async Task Create(ExpenseRecordCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.BuildingId.HasValue)
        {
            var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId.Value);
            if (!buildingExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified building does not exist.");
            }
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value);
            if (!apartmentExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId.Value);
            if (!vendorExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified vendor does not exist.");
            }
        }

        var userId = _currentUser.GetCurrentUserId();

        if (!TimeOfDeliveryHelper.TryParse(request.TimeOfDelivery, request.ExpenseDate, out var timeOfDelivery))
        {
            throw ExpenseRecordException.BadRequestException(
                "Time of delivery must be a valid time (e.g. '13:31', '01:31 PM') or a full date-time.");
        }

        await EnsureNoDuplicateExpense(request.Amount, request.ExpenseDate, request.ExpenseHead, request.SpecificItem, request.Nature, null, cancellationToken);
        await EnsureNoDuplicateTankerDelivery(request.TankerNumber, timeOfDelivery, null, cancellationToken);

        var record = new ExpenseRecord
        {
            Id = Guid.NewGuid(),
            Category = request.Category,
            ExpenseHead = request.ExpenseHead?.Trim(),
            SpecificItem = request.SpecificItem?.Trim(),
            VendorId = request.VendorId,
            Nature = request.Nature ?? ExpenseNature.Service,
            Amount = request.Amount ?? 0,
            Entity = request.Entity ?? ExpenseEntity.General,
            BuildingId = request.BuildingId,
            ApartmentId = request.ApartmentId,
            ExpenseDate = request.ExpenseDate ?? DateTime.UtcNow,
            PaymentMethod = request.PaymentMethod?.Trim() ?? string.Empty,
            Status = request.Status ?? ExpenseStatus.Draft,
            Reference = request.Reference?.Trim(),
            AttachmentUrl = request.AttachmentUrl?.Trim(),
            Description = request.Description?.Trim(),
            TankerNumber = request.TankerNumber?.Trim(),
            TimeOfDelivery = timeOfDelivery,
            DeliveryDriverName = request.DeliveryDriverName?.Trim(),
            ManagerInAttendance = request.ManagerInAttendance?.Trim(),
            LitersFilled = request.LitersFilled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.ExpenseRecordRepository.AddAsync(record), cancellationToken);

        var building = record.BuildingId.HasValue
            ? await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == record.BuildingId.Value)
            : null;
        var buildingName = building?.BuildingName ?? "building";
        var itemName = string.IsNullOrEmpty(record.SpecificItem) ? record.ExpenseHead ?? "maintenance" : record.SpecificItem;
        var verb = record.Status == ExpenseStatus.Paid ? "paid for" : "recorded for";

        await _activityService.CreateActivity(
            "Create",
            "ExpenseRecord",
            record.Id,
            record.BuildingId,
            $"{record.Amount:N0} SAR {verb} {itemName} at {buildingName}.",
            cancellationToken);

        var createTitle = record.Status == ExpenseStatus.Paid ? "Expense Paid" : "Expense Recorded";
        await _notificationService.CreateNotificationInternal(
            "finance",
            createTitle,
            $"{record.Amount:N0} SAR {verb} {itemName} at {buildingName}.",
            cancellationToken);
    }

    public async Task Update(Guid id, ExpenseRecordUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw ExpenseRecordException.NotFoundException($"Expense record with ID '{id}' was not found.");

        if (request.BuildingId.HasValue)
        {
            var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId.Value);
            if (!buildingExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified building does not exist.");
            }
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value);
            if (!apartmentExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId.Value);
            if (!vendorExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified vendor does not exist.");
            }
        }

        var userId = _currentUser.GetCurrentUserId();
        var oldStatus = record.Status;

        if (!TimeOfDeliveryHelper.TryParse(request.TimeOfDelivery, request.ExpenseDate, out var timeOfDelivery))
        {
            throw ExpenseRecordException.BadRequestException(
                "Time of delivery must be a valid time (e.g. '13:31', '01:31 PM') or a full date-time.");
        }

        await EnsureNoDuplicateExpense(request.Amount, request.ExpenseDate, request.ExpenseHead, request.SpecificItem, request.Nature, id, cancellationToken);
        await EnsureNoDuplicateTankerDelivery(request.TankerNumber, timeOfDelivery, id, cancellationToken);

        record.Category = request.Category;
        record.ExpenseHead = request.ExpenseHead?.Trim();
        record.SpecificItem = request.SpecificItem?.Trim();
        record.VendorId = request.VendorId;
        record.Nature = request.Nature ?? record.Nature;
        record.Amount = request.Amount ?? record.Amount;
        record.Entity = request.Entity ?? record.Entity;
        record.BuildingId = request.BuildingId;
        record.ApartmentId = request.ApartmentId;
        record.ExpenseDate = request.ExpenseDate ?? record.ExpenseDate;
        record.PaymentMethod = request.PaymentMethod?.Trim() ?? record.PaymentMethod;
        record.Status = request.Status ?? record.Status;
        record.Reference = request.Reference?.Trim();
        record.AttachmentUrl = request.AttachmentUrl?.Trim();
        record.Description = request.Description?.Trim();
        record.TankerNumber = request.TankerNumber?.Trim();
        record.TimeOfDelivery = timeOfDelivery;
        record.DeliveryDriverName = request.DeliveryDriverName?.Trim();
        record.ManagerInAttendance = request.ManagerInAttendance?.Trim();
        record.LitersFilled = request.LitersFilled;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = userId;
 
        _unitOfWork.ExpenseRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (record.Status == ExpenseStatus.Paid && oldStatus != ExpenseStatus.Paid)
        {
            var building = record.BuildingId.HasValue
                ? await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == record.BuildingId.Value)
                : null;
            var buildingName = building?.BuildingName ?? "building";
            var itemName = string.IsNullOrEmpty(record.SpecificItem) ? record.ExpenseHead ?? "maintenance" : record.SpecificItem;

            await _activityService.CreateActivity(
                "Update",
                "ExpenseRecord",
                record.Id,
                record.BuildingId,
                $"{record.Amount:N0} SAR paid for {itemName} at {buildingName}.",
                cancellationToken);

            await _notificationService.CreateNotificationInternal(
                "finance",
                "Expense Paid",
                $"{record.Amount:N0} SAR paid for {itemName} at {buildingName}.",
                cancellationToken);
        }
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw ExpenseRecordException.NotFoundException($"Expense record with ID '{id}' was not found.");

        var now = DateTime.UtcNow;
        var userId = _currentUser.GetCurrentUserId();

        record.IsDeleted = true;
        record.UpdatedAt = now;
        _unitOfWork.ExpenseRecordRepository.Update(record);

        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "ExpenseRecord",
            EntityId = record.Id,
            EntityTitle = $"{record.Category} ({record.Amount:F2} SAR)",
            DeletedBy = userId,
            DeletedAt = now
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityService.CreateActivity(
            "Delete",
            "ExpenseRecord",
            record.Id,
            record.BuildingId,
            $"Expense record of {record.Amount:N0} SAR deleted.",
            cancellationToken);

        await _notificationService.CreateNotificationInternal(
            "finance",
            "Expense Record Deleted",
            $"Expense record of {record.Amount:N0} SAR deleted.",
            cancellationToken);
    }

    public async Task UpdateStatus(Guid id, ExpenseRecordStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw ExpenseRecordException.NotFoundException($"Expense record with ID '{id}' was not found.");

        var userId = _currentUser.GetCurrentUserId();

        var oldStatus = record.Status;

        record.Status = request.Status ?? record.Status;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = userId;

        _unitOfWork.ExpenseRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (record.Status == ExpenseStatus.Paid && oldStatus != ExpenseStatus.Paid)
        {
            var building = record.BuildingId.HasValue
                ? await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == record.BuildingId.Value)
                : null;
            var buildingName = building?.BuildingName ?? "building";
            var itemName = string.IsNullOrEmpty(record.SpecificItem) ? record.ExpenseHead ?? "maintenance" : record.SpecificItem;

            await _activityService.CreateActivity(
                "StatusChange",
                "ExpenseRecord",
                record.Id,
                record.BuildingId,
                $"{record.Amount:N0} SAR paid for {itemName} at {buildingName}.",
                cancellationToken);

            await _notificationService.CreateNotificationInternal(
                "finance",
                "Expense Paid",
                $"{record.Amount:N0} SAR paid for {itemName} at {buildingName}.",
                cancellationToken);
        }
    }

    public async Task<byte[]> ExportToCsv(
        string? search,
        ExpenseCategory? category,
        ExpenseStatus? status,
        ExpenseNature? nature,
        Guid? buildingId,
        Guid? vendorId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var records = await _unitOfWork.ExpenseRecordRepository.GetAllAsync();
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync();
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync();
        var vendors = await _unitOfWork.VendorRepository.GetAllAsync();

        var buildingMap = buildings.ToDictionary(b => b.Id, b => b.BuildingName);
        var apartmentMap = apartments.ToDictionary(a => a.Id, a => a.FlatNumber);
        var vendorMap = vendors.ToDictionary(v => v.Id, v => (v.Name, v.CompanyName));

        var query = records.AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(x => x.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (nature.HasValue)
        {
            query = query.Where(x => x.Nature == nature.Value);
        }

        if (buildingId.HasValue)
        {
            query = query.Where(x => x.BuildingId == buildingId.Value);
        }

        if (vendorId.HasValue)
        {
            query = query.Where(x => x.VendorId == vendorId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                (x.ExpenseHead != null && x.ExpenseHead.ToLower().Contains(searchLower)) ||
                (x.SpecificItem != null && x.SpecificItem.ToLower().Contains(searchLower)) ||
                (x.Reference != null && x.Reference.ToLower().Contains(searchLower)) ||
                (x.Description != null && x.Description.ToLower().Contains(searchLower)) ||
                (x.BuildingId.HasValue && buildingMap.ContainsKey(x.BuildingId.Value) && buildingMap[x.BuildingId.Value].ToLower().Contains(searchLower)) ||
                (x.VendorId.HasValue && vendorMap.ContainsKey(x.VendorId.Value) && (vendorMap[x.VendorId.Value].Name.ToLower().Contains(searchLower) || vendorMap[x.VendorId.Value].CompanyName.ToLower().Contains(searchLower))) ||
                (x.ApartmentId.HasValue && apartmentMap.ContainsKey(x.ApartmentId.Value) && apartmentMap[x.ApartmentId.Value].ToLower().Contains(searchLower))
            );
        }

        var list = query.OrderByDescending(x => x.ExpenseDate).ToList();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("ID,Category,ExpenseHead,SpecificItem,VendorName,VendorCompanyName,Nature,Amount,Entity,BuildingName,FlatNumber,ExpenseDate,PaymentMethod,Status,Reference,Description,TankerNumber,TimeOfDelivery,DeliveryDriverName,ManagerInAttendance,LitersFilled,CreatedAt");

        foreach (var r in list)
        {
            var vName = r.VendorId.HasValue && vendorMap.TryGetValue(r.VendorId.Value, out var v) ? v.Name : "";
            var vComp = r.VendorId.HasValue && vendorMap.TryGetValue(r.VendorId.Value, out var v2) ? v2.CompanyName : "";
            var bName = r.BuildingId.HasValue && buildingMap.TryGetValue(r.BuildingId.Value, out var bn) ? bn : "";
            var fNum = r.ApartmentId.HasValue && apartmentMap.TryGetValue(r.ApartmentId.Value, out var fn) ? fn : "";

            var deliveryTime = r.TimeOfDelivery.HasValue ? r.TimeOfDelivery.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";

            csv.AppendLine($"\"{r.Id}\",\"{r.Category}\",\"{EscapeCsv(r.ExpenseHead)}\",\"{EscapeCsv(r.SpecificItem)}\",\"{EscapeCsv(vName)}\",\"{EscapeCsv(vComp)}\",\"{r.Nature}\",{r.Amount:F2},\"{r.Entity}\",\"{EscapeCsv(bName)}\",\"{EscapeCsv(fNum)}\",\"{r.ExpenseDate:yyyy-MM-dd}\",\"{r.PaymentMethod}\",\"{r.Status}\",\"{EscapeCsv(r.Reference)}\",\"{EscapeCsv(r.Description)}\",\"{EscapeCsv(r.TankerNumber)}\",\"{deliveryTime}\",\"{EscapeCsv(r.DeliveryDriverName)}\",\"{EscapeCsv(r.ManagerInAttendance)}\",\"{r.LitersFilled}\",\"{r.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Rule: the same Amount + ExpenseDate + ExpenseHead + SpecificItem + Nature must not be recorded twice.
    /// </summary>
    private async Task EnsureNoDuplicateExpense(
        decimal? amount,
        DateTime? expenseDate,
        string? expenseHead,
        string? specificItem,
        ExpenseNature? nature,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(expenseHead) || string.IsNullOrWhiteSpace(specificItem))
        {
            return;
        }

        var head = expenseHead.Trim().ToLower();
        var item = specificItem.Trim().ToLower();
        var date = expenseDate?.Date ?? DateTime.UtcNow.Date;
        var amountValue = amount ?? 0;
        var natureValue = nature ?? ExpenseNature.Service;

        var exists = await _unitOfWork.ExpenseRecordRepository.AnyAsync(x =>
            !x.IsDeleted &&
            x.Id != (excludeId ?? Guid.Empty) &&
            x.Amount == amountValue &&
            x.ExpenseDate.Date == date &&
            x.ExpenseHead != null && x.ExpenseHead.ToLower() == head &&
            x.SpecificItem != null && x.SpecificItem.ToLower() == item &&
            x.Nature == natureValue);

        if (exists)
        {
            throw ExpenseRecordException.BadRequestException(
                $"An expense entry with amount {amountValue:N2} SAR, expense date {date:yyyy-MM-dd}, " +
                $"expense head '{expenseHead.Trim()}', specific item '{specificItem.Trim()}' and " +
                $"nature '{natureValue}' already exists. Duplicate expense entries are not allowed.");
        }
    }

    /// <summary>
    /// Rule: the same tanker number at the same delivery time must not be recorded twice.
    /// </summary>
    private async Task EnsureNoDuplicateTankerDelivery(
        string? tankerNumber,
        DateTime? timeOfDelivery,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tankerNumber) || !timeOfDelivery.HasValue)
        {
            return;
        }

        var tankerNo = tankerNumber.Trim().ToLower();
        var deliveryTime = timeOfDelivery.Value;

        var exists = await _unitOfWork.ExpenseRecordRepository.AnyAsync(x =>
            !x.IsDeleted &&
            x.Id != (excludeId ?? Guid.Empty) &&
            x.TankerNumber != null && x.TankerNumber.ToLower() == tankerNo &&
            x.TimeOfDelivery == deliveryTime);

        if (exists)
        {
            throw ExpenseRecordException.BadRequestException(
                $"A water tank delivery with tanker number '{tankerNumber.Trim()}' and delivery time " +
                $"'{deliveryTime:yyyy-MM-dd HH:mm}' already exists. Duplicate water tank deliveries are not allowed.");
        }
    }

    private static string EscapeCsv(string? val)
    {
        if (string.IsNullOrEmpty(val)) return "";
        return val.Replace("\"", "\"\"");
    }
}
