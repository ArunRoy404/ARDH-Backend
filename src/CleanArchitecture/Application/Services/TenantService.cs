using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Tenant;

namespace CleanArchitecture.Application.Services;

public class TenantService(IUnitOfWork unitOfWork, ICurrentUser currentUser) : ITenantService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<PaginatedList<TenantViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        Guid? buildingId,
        Guid? apartmentId,
        TenantStatus? status,
        CancellationToken cancellationToken)
    {
        var tenants = await _unitOfWork.TenantRepository.GetAllAsync(x => x.DeletedAt == null);
        var buildings = await _unitOfWork.BuildingRepository.GetAllAsync(x => x.DeletedAt == null);
        var apartments = await _unitOfWork.ApartmentRepository.GetAllAsync(x => x.DeletedAt == null);

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

        var totalCount = query.Count();
        var pageEntities = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

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
                UpdatedAt = x.UpdatedAt
            })
            .ToList();

        return new PaginatedList<TenantViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<TenantViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw TenantException.NotFoundException("The specified tenant does not exist.");

        var building = await _unitOfWork.BuildingRepository.FirstOrDefaultAsync(x => x.Id == tenant.BuildingId && x.DeletedAt == null);
        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == tenant.ApartmentId && x.DeletedAt == null);

        return new TenantViewModel
        {
            Id = tenant.Id,
            BuildingId = tenant.BuildingId,
            BuildingName = building?.BuildingName ?? "Unknown Building",
            ApartmentId = tenant.ApartmentId,
            FlatNumber = apartment?.FlatNumber ?? "Unknown Flat",
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
            UpdatedAt = tenant.UpdatedAt
        };
    }

    public async Task Create(TenantCreateRequest request, CancellationToken cancellationToken)
    {
        // Validate Building
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId && x.DeletedAt == null);
        if (!buildingExists)
        {
            throw TenantException.BadRequestException("The specified building does not exist.");
        }

        // Validate Apartment
        var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId && x.DeletedAt == null);
        if (!apartmentExists)
        {
            throw TenantException.BadRequestException("The specified apartment does not exist.");
        }

        // Validate Email uniqueness
        var isEmailExist = await _unitOfWork.TenantRepository.AnyAsync(x => x.Email.ToLower() == request.Email.Trim().ToLower() && x.DeletedAt == null);
        if (isEmailExist)
        {
            throw TenantException.BadRequestException($"Tenant with email '{request.Email}' already exists.");
        }

        // Validate ID Number uniqueness
        var isIdNumberExist = await _unitOfWork.TenantRepository.AnyAsync(x => x.IdNumber.ToLower() == request.IdNumber.Trim().ToLower() && x.DeletedAt == null);
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
            Email = request.Email.Trim(),
            IdType = request.IdType,
            IdNumber = request.IdNumber.Trim(),
            IdProofAttachmentUrl = request.IdProofAttachmentUrl?.Trim(),
            MoveInDate = request.MoveInDate,
            LeaseStartDate = request.LeaseStartDate,
            LeaseEndDate = request.LeaseEndDate,
            MonthlyRent = request.MonthlyRent,
            SecurityDeposit = request.SecurityDeposit,
            EmergencyContactName = request.EmergencyContactName?.Trim(),
            EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
            Status = request.Status,
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.TenantRepository.AddAsync(tenant), cancellationToken);
    }

    public async Task Update(Guid id, TenantUpdateRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw TenantException.NotFoundException("The specified tenant does not exist.");

        // Validate Building
        var buildingExists = await _unitOfWork.BuildingRepository.AnyAsync(x => x.Id == request.BuildingId && x.DeletedAt == null);
        if (!buildingExists)
        {
            throw TenantException.BadRequestException("The specified building does not exist.");
        }

        // Validate Apartment
        var apartmentExists = await _unitOfWork.ApartmentRepository.AnyAsync(x => x.Id == request.ApartmentId && x.DeletedAt == null);
        if (!apartmentExists)
        {
            throw TenantException.BadRequestException("The specified apartment does not exist.");
        }

        // Validate Email uniqueness if changed
        if (!string.Equals(tenant.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var isEmailExist = await _unitOfWork.TenantRepository.AnyAsync(x => x.Email.ToLower() == request.Email.Trim().ToLower() && x.Id != id && x.DeletedAt == null);
            if (isEmailExist)
            {
                throw TenantException.BadRequestException($"Tenant with email '{request.Email}' already exists.");
            }
        }

        // Validate ID Number uniqueness if changed
        if (!string.Equals(tenant.IdNumber, request.IdNumber, StringComparison.OrdinalIgnoreCase))
        {
            var isIdNumberExist = await _unitOfWork.TenantRepository.AnyAsync(x => x.IdNumber.ToLower() == request.IdNumber.Trim().ToLower() && x.Id != id && x.DeletedAt == null);
            if (isIdNumberExist)
            {
                throw TenantException.BadRequestException($"Tenant with ID number '{request.IdNumber}' already exists.");
            }
        }

        tenant.BuildingId = request.BuildingId;
        tenant.ApartmentId = request.ApartmentId;
        tenant.FullName = request.FullName.Trim();
        tenant.Phone = request.Phone.Trim();
        tenant.Email = request.Email.Trim();
        tenant.IdType = request.IdType;
        tenant.IdNumber = request.IdNumber.Trim();
        tenant.IdProofAttachmentUrl = request.IdProofAttachmentUrl?.Trim();
        tenant.MoveInDate = request.MoveInDate;
        tenant.LeaseStartDate = request.LeaseStartDate;
        tenant.LeaseEndDate = request.LeaseEndDate;
        tenant.MonthlyRent = request.MonthlyRent;
        tenant.SecurityDeposit = request.SecurityDeposit;
        tenant.EmergencyContactName = request.EmergencyContactName?.Trim();
        tenant.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        tenant.Status = request.Status;
        tenant.Notes = request.Notes?.Trim();
        tenant.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.TenantRepository.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw TenantException.NotFoundException("The specified tenant does not exist.");

        tenant.DeletedAt = DateTime.UtcNow;
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
    }
}
