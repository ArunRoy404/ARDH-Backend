using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Expenses;

namespace CleanArchitecture.Application.Services;

public class ExpenseRecordService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IExpenseRecordService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<PaginatedList<ExpenseRecordViewModel>> GetPaginated(
        int page,
        int pageSize,
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
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var records = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x => x.DeletedAt == null);
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync(x => x.DeletedAt == null);
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => x.DeletedAt == null);
        var vendors = await _unitOfWork.VendorRepository.GetAllAsync(x => x.DeletedAt == null);

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
            CreatedBy = x.CreatedBy,
            UpdatedBy = x.UpdatedBy,
            DeletedBy = x.DeletedBy,
            RestoredBy = x.RestoredBy
        }).ToList();

        return new PaginatedList<ExpenseRecordViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<ExpenseRecordViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw ExpenseRecordException.NotFoundException($"Expense record with ID '{id}' was not found.");

        var building = record.BuildingId.HasValue ? await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == record.BuildingId.Value && x.DeletedAt == null) : null;
        var apartment = record.ApartmentId.HasValue ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value && x.DeletedAt == null) : null;
        var vendor = record.VendorId.HasValue ? await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == record.VendorId.Value && x.DeletedAt == null) : null;

        return new ExpenseRecordViewModel
        {
            Id = record.Id,
            Category = record.Category,
            ExpenseHead = record.ExpenseHead,
            SpecificItem = record.SpecificItem,
            VendorId = record.VendorId,
            VendorName = vendor?.Name,
            VendorCompanyName = vendor?.CompanyName,
            Nature = record.Nature,
            Amount = record.Amount,
            Entity = record.Entity,
            BuildingId = record.BuildingId,
            BuildingName = building?.BuildingName,
            ApartmentId = record.ApartmentId,
            FlatNumber = apartment?.FlatNumber,
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
            CreatedBy = record.CreatedBy,
            UpdatedBy = record.UpdatedBy,
            DeletedBy = record.DeletedBy,
            RestoredBy = record.RestoredBy
        };
    }

    public async Task Create(ExpenseRecordCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.BuildingId.HasValue)
        {
            var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId.Value && x.DeletedAt == null);
            if (!buildingExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified building does not exist.");
            }
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value && x.DeletedAt == null);
            if (!apartmentExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId.Value && x.DeletedAt == null);
            if (!vendorExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified vendor does not exist.");
            }
        }

        var userId = _currentUser.GetCurrentUserId();

        var record = new ExpenseRecord
        {
            Id = Guid.NewGuid(),
            Category = request.Category,
            ExpenseHead = request.ExpenseHead?.Trim(),
            SpecificItem = request.SpecificItem?.Trim(),
            VendorId = request.VendorId,
            Nature = request.Nature,
            Amount = request.Amount,
            Entity = request.Entity,
            BuildingId = request.BuildingId,
            ApartmentId = request.ApartmentId,
            ExpenseDate = request.ExpenseDate,
            PaymentMethod = request.PaymentMethod,
            Status = request.Status,
            Reference = request.Reference?.Trim(),
            AttachmentUrl = request.AttachmentUrl?.Trim(),
            Description = request.Description?.Trim(),
            TankerNumber = request.TankerNumber?.Trim(),
            TimeOfDelivery = request.TimeOfDelivery,
            DeliveryDriverName = request.DeliveryDriverName?.Trim(),
            ManagerInAttendance = request.ManagerInAttendance?.Trim(),
            LitersFilled = request.LitersFilled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.ExpenseRecordRepository.AddAsync(record), cancellationToken);
    }

    public async Task Update(Guid id, ExpenseRecordUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw ExpenseRecordException.NotFoundException($"Expense record with ID '{id}' was not found.");

        if (request.BuildingId.HasValue)
        {
            var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId.Value && x.DeletedAt == null);
            if (!buildingExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified building does not exist.");
            }
        }

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId.Value && x.DeletedAt == null);
            if (!apartmentExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified apartment does not exist.");
            }
        }

        if (request.VendorId.HasValue)
        {
            var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId.Value && x.DeletedAt == null);
            if (!vendorExists)
            {
                throw ExpenseRecordException.BadRequestException("The specified vendor does not exist.");
            }
        }

        var userId = _currentUser.GetCurrentUserId();

        record.Category = request.Category;
        record.ExpenseHead = request.ExpenseHead?.Trim();
        record.SpecificItem = request.SpecificItem?.Trim();
        record.VendorId = request.VendorId;
        record.Nature = request.Nature;
        record.Amount = request.Amount;
        record.Entity = request.Entity;
        record.BuildingId = request.BuildingId;
        record.ApartmentId = request.ApartmentId;
        record.ExpenseDate = request.ExpenseDate;
        record.PaymentMethod = request.PaymentMethod;
        record.Status = request.Status;
        record.Reference = request.Reference?.Trim();
        record.AttachmentUrl = request.AttachmentUrl?.Trim();
        record.Description = request.Description?.Trim();
        record.TankerNumber = request.TankerNumber?.Trim();
        record.TimeOfDelivery = request.TimeOfDelivery;
        record.DeliveryDriverName = request.DeliveryDriverName?.Trim();
        record.ManagerInAttendance = request.ManagerInAttendance?.Trim();
        record.LitersFilled = request.LitersFilled;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = userId;

        _unitOfWork.ExpenseRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw ExpenseRecordException.NotFoundException($"Expense record with ID '{id}' was not found.");

        var now = DateTime.UtcNow;
        var userId = _currentUser.GetCurrentUserId();

        record.DeletedAt = now;
        record.UpdatedAt = now;
        record.DeletedBy = userId;

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
    }

    public async Task UpdateStatus(Guid id, ExpenseRecordStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw ExpenseRecordException.NotFoundException($"Expense record with ID '{id}' was not found.");

        var userId = _currentUser.GetCurrentUserId();

        record.Status = request.Status;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = userId;

        _unitOfWork.ExpenseRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
