using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.DeletedHistory;

namespace CleanArchitecture.Application.Services;

public class DeletedHistoryService(IUnitOfWork unitOfWork, ICurrentUser currentUser, INotificationService notificationService) : IDeletedHistoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<PaginatedList<DeletedHistoryViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        string? entityType,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var histories = await _unitOfWork.DeletedHistoryRepository.GetAllAsync();
        var query = histories.AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(x => x.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase));
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.DeletedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.DeletedAt <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(x => x.EntityTitle.ToLower().Contains(cleanSearch) || x.EntityType.ToLower().Contains(cleanSearch));
        }

        query = query.OrderByDescending(x => x.DeletedAt);

        var totalCount = query.Count();
        var pagedItems = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Resolve user names for DeletedBy and RestoredBy
        var userIds = pagedItems.Select(x => x.DeletedBy)
            .Concat(pagedItems.Select(x => x.RestoredBy))
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var usersMap = new Dictionary<Guid, string>();
        foreach (var userId in userIds)
        {
            var u = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == userId);
            if (u != null)
            {
                usersMap[userId] = u.Name;
            }
        }

        var viewModels = pagedItems.Select(x => new DeletedHistoryViewModel
        {
            Id = x.Id,
            EntityType = x.EntityType,
            EntityId = x.EntityId,
            EntityTitle = x.EntityTitle,
            DeletedBy = x.DeletedBy.HasValue && usersMap.TryGetValue(x.DeletedBy.Value, out var name1) ? name1 : null,
            DeletedAt = x.DeletedAt,
            RestoredAt = x.RestoredAt,
            RestoredBy = x.RestoredBy.HasValue && usersMap.TryGetValue(x.RestoredBy.Value, out var name2) ? name2 : null,
            PermanentlyDeletable = !x.RestoredAt.HasValue,
            Restorable = !x.RestoredAt.HasValue
        }).ToList();

        return new PaginatedList<DeletedHistoryViewModel>(viewModels, totalCount, page, pageSize);
    }

    public async Task Restore(Guid id, CancellationToken cancellationToken)
    {
        var history = await _unitOfWork.DeletedHistoryRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw DeletedHistoryException.NotFoundException("Deleted history record not found.");

        if (history.RestoredAt.HasValue)
        {
            throw DeletedHistoryException.BadRequestException("This record has already been restored.");
        }

        switch (history.EntityType.ToLower())
        {
            case "building":
                var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (building != null)
                {
                    building.IsDeleted = false;
                    building.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.BuildingRepository.Update(building);
                }
                break;
            case "owner":
                var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (owner != null)
                {
                    owner.IsDeleted = false;
                    owner.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.OwnerRepository.Update(owner);
                }
                break;
            case "apartment":
                var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (apartment != null)
                {
                    apartment.IsDeleted = false;
                    apartment.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.ApartmentRepository.Update(apartment);
                }
                break;
            case "tenant":
                var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (tenant != null)
                {
                    tenant.IsDeleted = false;
                    tenant.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.TenantRepository.Update(tenant);
                }
                break;
            case "equipment":
                var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (equipment != null)
                {
                    equipment.IsDeleted = false;
                    equipment.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.EquipmentRepository.Update(equipment);
                }
                break;
            case "amccontract":
                var amc = await _unitOfWork.AmcContractRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (amc != null)
                {
                    amc.IsDeleted = false;
                    amc.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.AmcContractRepository.Update(amc);
                }
                break;
            case "maintenancerequest":
                var request = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (request != null)
                {
                    request.IsDeleted = false;
                    request.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.MaintenanceRequestRepository.Update(request);
                }
                break;
            case "incomerecord":
                var income = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (income != null)
                {
                    income.IsDeleted = false;
                    income.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.IncomeRecordRepository.Update(income);
                }
                break;
            case "expenserecord":
                var expense = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (expense != null)
                {
                    expense.IsDeleted = false;
                    expense.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.ExpenseRecordRepository.Update(expense);
                }
                break;
            case "vendor":
                var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (vendor != null)
                {
                    vendor.IsDeleted = false;
                    vendor.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.VendorRepository.Update(vendor);
                }
                break;
            case "user":
                var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (user != null)
                {
                    user.IsActive = true;
                    user.IsDeleted = false;
                    user.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.UserRepository.Update(user);
                }
                break;
            default:
                throw DeletedHistoryException.BadRequestException($"Restoring entity type '{history.EntityType}' is not supported.");
        }

        history.RestoredAt = DateTime.UtcNow;
        history.RestoredBy = _currentUser.GetCurrentUserId();
        _unitOfWork.DeletedHistoryRepository.Update(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationInternal(
            "admin",
            "Record Restored",
            $"{history.EntityType} record '{history.EntityTitle}' was restored.",
            cancellationToken);
    }

    public async Task DeletePermanently(Guid id, CancellationToken cancellationToken)
    {
        var history = await _unitOfWork.DeletedHistoryRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw DeletedHistoryException.NotFoundException("Deleted history record not found.");

        if (history.RestoredAt.HasValue)
        {
            throw DeletedHistoryException.BadRequestException("This record has already been restored and cannot be permanently deleted.");
        }

        switch (history.EntityType.ToLower())
        {
            case "building":
                var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (building != null)
                {
                    var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => x.BuildingId == building.Id);
                    foreach (var apt in apartments)
                    {
                        var tenants = await _unitOfWork.TenantRepository.GetAllAsync(x => x.ApartmentId == apt.Id);
                        foreach (var t in tenants)
                        {
                            var moveouts = await _unitOfWork.TenantMoveOutRecordRepository.GetAllAsync(x => x.TenantId == t.Id);
                            _unitOfWork.TenantMoveOutRecordRepository.DeleteRange(moveouts);

                            var incomes = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => x.TenantId == t.Id);
                            _unitOfWork.IncomeRecordRepository.DeleteRange(incomes);

                            _unitOfWork.TenantRepository.Delete(t);
                        }

                        var maint = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => x.ApartmentId == apt.Id);
                        _unitOfWork.MaintenanceRequestRepository.DeleteRange(maint);

                        var inc = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => x.ApartmentId == apt.Id);
                        _unitOfWork.IncomeRecordRepository.DeleteRange(inc);

                        var exp = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x => x.ApartmentId == apt.Id);
                        _unitOfWork.ExpenseRecordRepository.DeleteRange(exp);

                        _unitOfWork.ApartmentRepository.Delete(apt);
                    }

                    var equipments = await _unitOfWork.EquipmentRepository.GetAllAsync(x => x.BuildingId == building.Id);
                    foreach (var eq in equipments)
                    {
                        var amcs = await _unitOfWork.AmcContractRepository.GetAllAsync(x => x.EquipmentId == eq.Id);
                        _unitOfWork.AmcContractRepository.DeleteRange(amcs);

                        var maint = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => x.EquipmentId == eq.Id);
                        _unitOfWork.MaintenanceRequestRepository.DeleteRange(maint);

                        _unitOfWork.EquipmentRepository.Delete(eq);
                    }

                    var bldMaint = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => x.BuildingId == building.Id);
                    _unitOfWork.MaintenanceRequestRepository.DeleteRange(bldMaint);

                    var bldInc = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => x.BuildingId == building.Id);
                    _unitOfWork.IncomeRecordRepository.DeleteRange(bldInc);

                    var bldExp = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x => x.BuildingId == building.Id);
                    _unitOfWork.ExpenseRecordRepository.DeleteRange(bldExp);

                    _unitOfWork.BuildingRepository.Delete(building);
                }
                break;

            case "owner":
                var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (owner != null)
                {
                    var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => x.OwnerId == owner.Id);
                    foreach (var apt in apartments)
                    {
                        var tenants = await _unitOfWork.TenantRepository.GetAllAsync(x => x.ApartmentId == apt.Id);
                        foreach (var t in tenants)
                        {
                            var moveouts = await _unitOfWork.TenantMoveOutRecordRepository.GetAllAsync(x => x.TenantId == t.Id);
                            _unitOfWork.TenantMoveOutRecordRepository.DeleteRange(moveouts);

                            var incomes = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => x.TenantId == t.Id);
                            _unitOfWork.IncomeRecordRepository.DeleteRange(incomes);

                            _unitOfWork.TenantRepository.Delete(t);
                        }

                        var maint = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => x.ApartmentId == apt.Id);
                        _unitOfWork.MaintenanceRequestRepository.DeleteRange(maint);

                        var inc = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => x.ApartmentId == apt.Id);
                        _unitOfWork.IncomeRecordRepository.DeleteRange(inc);

                        var exp = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x => x.ApartmentId == apt.Id);
                        _unitOfWork.ExpenseRecordRepository.DeleteRange(exp);

                        _unitOfWork.ApartmentRepository.Delete(apt);
                    }

                    _unitOfWork.OwnerRepository.Delete(owner);
                }
                break;

            case "apartment":
                var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (apartment != null)
                {
                    var tenants = await _unitOfWork.TenantRepository.GetAllAsync(x => x.ApartmentId == apartment.Id);
                    foreach (var t in tenants)
                    {
                        var moveouts = await _unitOfWork.TenantMoveOutRecordRepository.GetAllAsync(x => x.TenantId == t.Id);
                        _unitOfWork.TenantMoveOutRecordRepository.DeleteRange(moveouts);

                        var incomes = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => x.TenantId == t.Id);
                        _unitOfWork.IncomeRecordRepository.DeleteRange(incomes);

                        _unitOfWork.TenantRepository.Delete(t);
                    }

                    var maint = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => x.ApartmentId == apartment.Id);
                    _unitOfWork.MaintenanceRequestRepository.DeleteRange(maint);

                    var inc = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => x.ApartmentId == apartment.Id);
                    _unitOfWork.IncomeRecordRepository.DeleteRange(inc);

                    var exp = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x => x.ApartmentId == apartment.Id);
                    _unitOfWork.ExpenseRecordRepository.DeleteRange(exp);

                    _unitOfWork.ApartmentRepository.Delete(apartment);
                }
                break;

            case "tenant":
                var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (tenant != null)
                {
                    var moveouts = await _unitOfWork.TenantMoveOutRecordRepository.GetAllAsync(x => x.TenantId == tenant.Id);
                    _unitOfWork.TenantMoveOutRecordRepository.DeleteRange(moveouts);

                    var incomes = await _unitOfWork.IncomeRecordRepository.GetAllAsync(x => x.TenantId == tenant.Id);
                    _unitOfWork.IncomeRecordRepository.DeleteRange(incomes);

                    _unitOfWork.TenantRepository.Delete(tenant);
                }
                break;

            case "equipment":
                var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (equipment != null)
                {
                    var amcs = await _unitOfWork.AmcContractRepository.GetAllAsync(x => x.EquipmentId == equipment.Id);
                    _unitOfWork.AmcContractRepository.DeleteRange(amcs);

                    var maint = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => x.EquipmentId == equipment.Id);
                    _unitOfWork.MaintenanceRequestRepository.DeleteRange(maint);

                    _unitOfWork.EquipmentRepository.Delete(equipment);
                }
                break;

            case "vendor":
                var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (vendor != null)
                {
                    var amcs = await _unitOfWork.AmcContractRepository.GetAllAsync(x => x.VendorId == vendor.Id);
                    _unitOfWork.AmcContractRepository.DeleteRange(amcs);

                    var maint = await _unitOfWork.MaintenanceRequestRepository.GetAllAsync(x => x.VendorId == vendor.Id);
                    _unitOfWork.MaintenanceRequestRepository.DeleteRange(maint);

                    var exps = await _unitOfWork.ExpenseRecordRepository.GetAllAsync(x => x.VendorId == vendor.Id);
                    _unitOfWork.ExpenseRecordRepository.DeleteRange(exps);

                    var eqs = await _unitOfWork.EquipmentRepository.GetAllAsync(x => x.AmcVendorId == vendor.Id);
                    foreach (var eq in eqs)
                    {
                        eq.AmcVendorId = Guid.Empty;
                        eq.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.EquipmentRepository.Update(eq);
                    }

                    _unitOfWork.VendorRepository.Delete(vendor);
                }
                break;

            case "amccontract":
                var amc = await _unitOfWork.AmcContractRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (amc != null)
                {
                    _unitOfWork.AmcContractRepository.Delete(amc);
                }
                break;

            case "maintenancerequest":
                var request = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (request != null)
                {
                    _unitOfWork.MaintenanceRequestRepository.Delete(request);
                }
                break;

            case "incomerecord":
                var income = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (income != null)
                {
                    _unitOfWork.IncomeRecordRepository.Delete(income);
                }
                break;

            case "expenserecord":
                var expense = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (expense != null)
                {
                    _unitOfWork.ExpenseRecordRepository.Delete(expense);
                }
                break;

            case "user":
                var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
                if (user != null)
                {
                    var recipients = await _unitOfWork.NotificationRecipientRepository.GetAllAsync(x => x.UserId == user.Id);
                    _unitOfWork.NotificationRecipientRepository.DeleteRange(recipients);

                    _unitOfWork.UserRepository.Delete(user);
                }
                break;
        }

        _unitOfWork.DeletedHistoryRepository.Delete(history);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeletedHistoryDetailsViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var history = await _unitOfWork.DeletedHistoryRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw DeletedHistoryException.NotFoundException("Deleted history record not found.");

        var deletedBy = history.DeletedBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.DeletedBy.Value) : null;
        var restoredBy = history.RestoredBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.RestoredBy.Value) : null;

        return new DeletedHistoryDetailsViewModel
        {
            Id = history.Id,
            EntityType = history.EntityType,
            EntityId = history.EntityId,
            EntityTitle = history.EntityTitle,
            DeletedBy = deletedBy?.Name,
            DeletedAt = history.DeletedAt,
            RestoredAt = history.RestoredAt,
            RestoredBy = restoredBy?.Name,
            PermanentlyDeletable = !history.RestoredAt.HasValue,
            Restorable = !history.RestoredAt.HasValue,
            EntityData = null
        };
    }
}
