using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Tenant;
using CleanArchitecture.Shared.Models.Apartment;

namespace CleanArchitecture.Application.Services;

public class TenantMoveOutService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IActivityService activityService) : ITenantMoveOutService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IActivityService _activityService = activityService;

    public async Task CreateMoveOut(Guid tenantId, TenantMoveOutCreateRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == tenantId)
            ?? throw TenantException.NotFoundException("The specified tenant does not exist.");

        var existingRecord = await _unitOfWork.TenantMoveOutRecordRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId);
        if (existingRecord != null)
        {
            throw TenantException.BadRequestException("A move-out record already exists for this tenant.");
        }

        var moveOutRecord = new TenantMoveOutRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ApartmentId = tenant.ApartmentId,
            MoveOutDate = request.MoveOutDate,
            PendingRent = request.PendingRent,
            DamageAmount = request.DamageAmount,
            RefundAmount = request.RefundAmount,
            IdNumber = request.IdNumber.Trim(),
            HandoverNote = request.HandoverNote?.Trim(),
            ProcessedBy = _currentUser.GetCurrentUserId(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        // Update tenant status to MovedOut
        tenant.Status = TenantStatus.MovedOut;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            await _unitOfWork.TenantMoveOutRecordRepository.AddAsync(moveOutRecord);
            _unitOfWork.TenantRepository.Update(tenant);
        }, cancellationToken);

        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == tenant.ApartmentId);
        var flatNum = apartment?.FlatNumber ?? "Unknown Flat";
        await _activityService.CreateActivity("MoveOut", "Tenant", tenant.Id, tenant.BuildingId, $"Tenant '{tenant.FullName}' moved out of Flat '{flatNum}'.", cancellationToken);
    }

    public async Task<TenantMoveOutRecordViewModel> GetByTenantId(Guid tenantId, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.TenantMoveOutRecordRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)
            ?? throw TenantException.NotFoundException("No move-out record found for the specified tenant.");

        var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == record.TenantId);
        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId);
        var processor = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == record.ProcessedBy);

        return new TenantMoveOutRecordViewModel
        {
            Id = record.Id,
            TenantId = record.TenantId,
            TenantName = tenant?.FullName ?? "Unknown Tenant",
            Tenant = tenant == null ? null : new TenantViewModel
            {
                Id = tenant.Id,
                BuildingId = tenant.BuildingId,
                ApartmentId = tenant.ApartmentId,
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
                UpdatedAt = tenant.UpdatedAt,
                CreatedBy = tenant.CreatedBy,
                UpdatedBy = tenant.UpdatedBy
            },
            ApartmentId = record.ApartmentId,
            FlatNumber = apartment?.FlatNumber ?? "Unknown Flat",
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
                CreatedAt = apartment.CreatedAt,
                UpdatedAt = apartment.UpdatedAt,
                CreatedBy = apartment.CreatedBy,
                UpdatedBy = apartment.UpdatedBy
            },
            MoveOutDate = record.MoveOutDate,
            PendingRent = record.PendingRent,
            DamageAmount = record.DamageAmount,
            RefundAmount = record.RefundAmount,
            IdNumber = record.IdNumber,
            HandoverNote = record.HandoverNote,
            ProcessedBy = record.ProcessedBy,
            ProcessedByName = processor?.Name ?? "System",
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            CreatedBy = record.CreatedBy,
            UpdatedBy = record.UpdatedBy,
        };
    }

    public async Task UpdateMoveOut(Guid tenantId, TenantMoveOutUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.TenantMoveOutRecordRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)
            ?? throw TenantException.NotFoundException("No move-out record found for the specified tenant.");

        record.MoveOutDate = request.MoveOutDate;
        record.PendingRent = request.PendingRent;
        record.DamageAmount = request.DamageAmount;
        record.RefundAmount = request.RefundAmount;
        record.IdNumber = request.IdNumber.Trim();
        record.HandoverNote = request.HandoverNote?.Trim();
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = _currentUser.GetCurrentUserId();

        _unitOfWork.TenantMoveOutRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMoveOut(Guid tenantId, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.TenantMoveOutRecordRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)
            ?? throw TenantException.NotFoundException("No move-out record found for the specified tenant.");

        _unitOfWork.TenantMoveOutRecordRepository.Delete(record);

        // Record soft-delete history
        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "TenantMoveOutRecord",
            EntityId = record.Id,
            EntityTitle = $"Move-out record for Tenant ID: {tenantId}",
            DeletedBy = _currentUser.GetCurrentUserId(),
            DeletedAt = DateTime.UtcNow
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
