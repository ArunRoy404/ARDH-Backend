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
using CleanArchitecture.Shared.Models.User;
using CleanArchitecture.Shared.Models.Building;
using CleanArchitecture.Shared.Models.Vendor;
using CleanArchitecture.Shared.Models.Equipment;
using CleanArchitecture.Shared.Models.AmcContract;
using CleanArchitecture.Shared.Models.Maintenance;
using CleanArchitecture.Shared.Models.Income;
using CleanArchitecture.Shared.Models.Expenses;

namespace CleanArchitecture.Application.Services;

public class DeletedHistoryService(IUnitOfWork unitOfWork, ICurrentUser currentUser) : IDeletedHistoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

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
            DeletedBy = x.DeletedBy,
            DeletedByName = x.DeletedBy.HasValue && usersMap.TryGetValue(x.DeletedBy.Value, out var name1) ? name1 : null,
            DeletedAt = x.DeletedAt,
            RestoredAt = x.RestoredAt,
            RestoredBy = x.RestoredBy,
            RestoredByName = x.RestoredBy.HasValue && usersMap.TryGetValue(x.RestoredBy.Value, out var name2) ? name2 : null
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

        if (history.EntityType.Equals("User", StringComparison.OrdinalIgnoreCase))
        {
            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original User not found.");
            
            user.DeletedAt = null;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.UserRepository.Update(user);
        }
        else if (history.EntityType.Equals("Building", StringComparison.OrdinalIgnoreCase))
        {
            var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Building not found.");
            
            building.DeletedAt = null;
            building.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.BuildingRepository.Update(building);
        }
        else if (history.EntityType.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Owner not found.");
            
            owner.DeletedAt = null;
            owner.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.OwnerRepository.Update(owner);
        }
        else if (history.EntityType.Equals("Apartment", StringComparison.OrdinalIgnoreCase))
        {
            var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Apartment not found.");
            
            apartment.DeletedAt = null;
            apartment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.ApartmentRepository.Update(apartment);
        }
        else if (history.EntityType.Equals("Tenant", StringComparison.OrdinalIgnoreCase))
        {
            var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Tenant not found.");

            tenant.DeletedAt = null;
            tenant.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.TenantRepository.Update(tenant);
        }
        else if (history.EntityType.Equals("TenantMoveOutRecord", StringComparison.OrdinalIgnoreCase))
        {
            var moveOutRecord = await _unitOfWork.TenantMoveOutRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Move-out record not found.");

            moveOutRecord.DeletedAt = null;
            moveOutRecord.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.TenantMoveOutRecordRepository.Update(moveOutRecord);
        }
        else if (history.EntityType.Equals("Vendor", StringComparison.OrdinalIgnoreCase))
        {
            var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Vendor not found.");

            vendor.DeletedAt = null;
            vendor.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.VendorRepository.Update(vendor);
        }
        else if (history.EntityType.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
        {
            var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Equipment not found.");

            equipment.DeletedAt = null;
            equipment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.EquipmentRepository.Update(equipment);
        }
        else if (history.EntityType.Equals("AmcContract", StringComparison.OrdinalIgnoreCase))
        {
            var contract = await _unitOfWork.AmcContractRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original AMC Contract not found.");

            contract.DeletedAt = null;
            contract.UpdatedAt = DateTime.UtcNow;
            contract.RestoredBy = _currentUser.GetCurrentUserId();
            _unitOfWork.AmcContractRepository.Update(contract);
        }
        else if (history.EntityType.Equals("MaintenanceRequest", StringComparison.OrdinalIgnoreCase))
        {
            var request = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Maintenance request not found.");

            request.DeletedAt = null;
            request.UpdatedAt = DateTime.UtcNow;
            request.RestoredBy = _currentUser.GetCurrentUserId();
            _unitOfWork.MaintenanceRequestRepository.Update(request);
        }
        else if (history.EntityType.Equals("IncomeRecord", StringComparison.OrdinalIgnoreCase))
        {
            var income = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Income record not found.");

            income.DeletedAt = null;
            income.UpdatedAt = DateTime.UtcNow;
            income.RestoredBy = _currentUser.GetCurrentUserId();
            _unitOfWork.IncomeRecordRepository.Update(income);
        }
        else if (history.EntityType.Equals("ExpenseRecord", StringComparison.OrdinalIgnoreCase))
        {
            var expense = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId)
                ?? throw DeletedHistoryException.NotFoundException("Original Expense record not found.");

            expense.DeletedAt = null;
            expense.UpdatedAt = DateTime.UtcNow;
            expense.RestoredBy = _currentUser.GetCurrentUserId();
            _unitOfWork.ExpenseRecordRepository.Update(expense);
        }
        else
        {
            throw DeletedHistoryException.BadRequestException($"Unknown entity type: {history.EntityType}");
        }

        history.RestoredAt = DateTime.UtcNow;
        history.RestoredBy = _currentUser.GetCurrentUserId();
        _unitOfWork.DeletedHistoryRepository.Update(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePermanently(Guid id, CancellationToken cancellationToken)
    {
        var history = await _unitOfWork.DeletedHistoryRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw DeletedHistoryException.NotFoundException("Deleted history record not found.");

        if (history.RestoredAt.HasValue)
        {
            throw DeletedHistoryException.BadRequestException("This record has already been restored and cannot be permanently deleted.");
        }

        // Delete underlying entity permanently
        if (history.EntityType.Equals("User", StringComparison.OrdinalIgnoreCase))
        {
            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (user != null)
            {
                _unitOfWork.UserRepository.Delete(user);
            }
        }
        else if (history.EntityType.Equals("Building", StringComparison.OrdinalIgnoreCase))
        {
            var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (building != null)
            {
                _unitOfWork.BuildingRepository.Delete(building);
            }
        }
        else if (history.EntityType.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (owner != null)
            {
                _unitOfWork.OwnerRepository.Delete(owner);
            }
        }
        else if (history.EntityType.Equals("Apartment", StringComparison.OrdinalIgnoreCase))
        {
            var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (apartment != null)
            {
                _unitOfWork.ApartmentRepository.Delete(apartment);
            }
        }
        else if (history.EntityType.Equals("Tenant", StringComparison.OrdinalIgnoreCase))
        {
            var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (tenant != null)
            {
                _unitOfWork.TenantRepository.Delete(tenant);
            }
        }
        else if (history.EntityType.Equals("TenantMoveOutRecord", StringComparison.OrdinalIgnoreCase))
        {
            var moveOutRecord = await _unitOfWork.TenantMoveOutRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (moveOutRecord != null)
            {
                _unitOfWork.TenantMoveOutRecordRepository.Delete(moveOutRecord);
            }
        }
        else if (history.EntityType.Equals("Vendor", StringComparison.OrdinalIgnoreCase))
        {
            var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (vendor != null)
            {
                _unitOfWork.VendorRepository.Delete(vendor);
            }
        }
        else if (history.EntityType.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
        {
            var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (equipment != null)
            {
                _unitOfWork.EquipmentRepository.Delete(equipment);
            }
        }
        else if (history.EntityType.Equals("AmcContract", StringComparison.OrdinalIgnoreCase))
        {
            var contract = await _unitOfWork.AmcContractRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (contract != null)
            {
                _unitOfWork.AmcContractRepository.Delete(contract);
            }
        }
        else if (history.EntityType.Equals("MaintenanceRequest", StringComparison.OrdinalIgnoreCase))
        {
            var request = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (request != null)
            {
                _unitOfWork.MaintenanceRequestRepository.Delete(request);
            }
        }
        else if (history.EntityType.Equals("IncomeRecord", StringComparison.OrdinalIgnoreCase))
        {
            var income = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (income != null)
            {
                _unitOfWork.IncomeRecordRepository.Delete(income);
            }
        }
        else if (history.EntityType.Equals("ExpenseRecord", StringComparison.OrdinalIgnoreCase))
        {
            var expense = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (expense != null)
            {
                _unitOfWork.ExpenseRecordRepository.Delete(expense);
            }
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

        object? entityData = null;
        if (history.EntityType.Equals("User", StringComparison.OrdinalIgnoreCase))
        {
            var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (user != null)
            {
                entityData = new UserViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    Address = user.Address ?? string.Empty,
                    Role = user.Role,
                    AvatarUrl = user.AvatarUrl ?? string.Empty,
                    IsActive = user.IsActive,
                    Permissions = user.Permissions ?? string.Empty,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };
            }
        }
        else if (history.EntityType.Equals("Building", StringComparison.OrdinalIgnoreCase))
        {
            var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (building != null)
            {
                entityData = new BuildingViewModel
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
                    UpdatedAt = building.UpdatedAt
                };
            }
        }
        else if (history.EntityType.Equals("Vendor", StringComparison.OrdinalIgnoreCase))
        {
            var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (vendor != null)
            {
                entityData = new VendorViewModel
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
                    UpdatedAt = vendor.UpdatedAt
                };
            }
        }
        else if (history.EntityType.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
        {
            var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (equipment != null)
            {
                var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == equipment.BuildingId);
                var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == equipment.AmcVendorId);

                entityData = new EquipmentViewModel
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
        }
        else if (history.EntityType.Equals("AmcContract", StringComparison.OrdinalIgnoreCase))
        {
            var contract = await _unitOfWork.AmcContractRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (contract != null)
            {
                var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == contract.EquipmentId);
                var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == contract.VendorId);

                entityData = new AmcContractViewModel
                {
                    Id = contract.Id,
                    AmcCode = contract.AmcCode,
                    ContractNumber = contract.ContractNumber,
                    ContractTitle = contract.ContractTitle,
                    ContractType = contract.ContractType,
                    EquipmentId = contract.EquipmentId,
                    EquipmentName = equipment?.Name ?? "Unknown Equipment",
                    VendorId = contract.VendorId,
                    VendorName = vendor?.Name ?? "Unknown Vendor",
                    VendorCompanyName = vendor?.CompanyName ?? "Unknown Vendor",
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    ContractAmount = contract.ContractAmount,
                    PaymentTerms = contract.PaymentTerms,
                    ServiceFrequency = contract.ServiceFrequency,
                    CoverageDetails = contract.CoverageDetails,
                    Exclusions = contract.Exclusions,
                    DocumentLink = contract.DocumentLink,
                    Remarks = contract.Remarks,
                    Status = contract.Status,
                    CreatedAt = contract.CreatedAt,
                    UpdatedAt = contract.UpdatedAt,
                    CreatedBy = contract.CreatedBy,
                    UpdatedBy = contract.UpdatedBy,
                    DeletedBy = contract.DeletedBy,
                    RestoredBy = contract.RestoredBy
                };
            }
        }
        else if (history.EntityType.Equals("MaintenanceRequest", StringComparison.OrdinalIgnoreCase))
        {
            var request = await _unitOfWork.MaintenanceRequestRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (request != null)
            {
                var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == request.BuildingId);
                var apartment = request.ApartmentId.HasValue ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == request.ApartmentId.Value) : null;
                var vendor = request.VendorId.HasValue ? await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == request.VendorId.Value) : null;
                var equipment = request.EquipmentId.HasValue ? await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == request.EquipmentId.Value) : null;

                entityData = new MaintenanceRequestViewModel
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
        }
        else if (history.EntityType.Equals("IncomeRecord", StringComparison.OrdinalIgnoreCase))
        {
            var record = await _unitOfWork.IncomeRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (record != null)
            {
                var building = record.BuildingId.HasValue ? await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == record.BuildingId.Value) : null;
                var apartment = record.ApartmentId.HasValue ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value) : null;
                var tenant = record.TenantId.HasValue ? await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == record.TenantId.Value) : null;

                entityData = new IncomeRecordViewModel
                {
                    Id = record.Id,
                    IncomeEntity = record.IncomeEntity,
                    IncomeType = record.IncomeType,
                    Amount = record.Amount,
                    TenantId = record.TenantId,
                    TenantName = tenant?.FullName,
                    BuildingId = record.BuildingId,
                    BuildingName = building?.BuildingName,
                    ApartmentId = record.ApartmentId,
                    FlatNumber = apartment?.FlatNumber,
                    Period = record.Period,
                    PaymentDate = record.PaymentDate,
                    PaymentMethod = record.PaymentMethod,
                    TransactionReference = record.TransactionReference,
                    Status = record.Status,
                    Notes = record.Notes,
                    AttachmentUrl = record.AttachmentUrl,
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = record.UpdatedAt,
                    CreatedBy = record.CreatedBy,
                    UpdatedBy = record.UpdatedBy,
                    DeletedBy = record.DeletedBy,
                    RestoredBy = record.RestoredBy
                };
            }
        }
        else if (history.EntityType.Equals("ExpenseRecord", StringComparison.OrdinalIgnoreCase))
        {
            var record = await _unitOfWork.ExpenseRecordRepository.FirstOrDefaultAsync(x => x.Id == history.EntityId);
            if (record != null)
            {
                var building = record.BuildingId.HasValue ? await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == record.BuildingId.Value) : null;
                var apartment = record.ApartmentId.HasValue ? await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId.Value) : null;
                var vendor = record.VendorId.HasValue ? await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == record.VendorId.Value) : null;

                entityData = new ExpenseRecordViewModel
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
        }

        return new DeletedHistoryDetailsViewModel
        {
            Id = history.Id,
            EntityType = history.EntityType,
            EntityId = history.EntityId,
            EntityTitle = history.EntityTitle,
            DeletedBy = history.DeletedBy,
            DeletedByName = deletedBy?.Name,
            DeletedAt = history.DeletedAt,
            RestoredAt = history.RestoredAt,
            RestoredBy = history.RestoredBy,
            RestoredByName = restoredBy?.Name,
            EntityData = entityData
        };
    }
}
