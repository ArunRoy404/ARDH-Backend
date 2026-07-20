using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Tenant;

namespace CleanArchitecture.Application.Services;

public class TenantMoveOutService(IUnitOfWork unitOfWork, ICurrentUser currentUser) : ITenantMoveOutService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task CreateMoveOut(Guid tenantId, TenantMoveOutCreateRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == tenantId && x.DeletedAt == null)
            ?? throw TenantException.NotFoundException("The specified tenant does not exist.");

        var existingRecord = await _unitOfWork.TenantMoveOutRecordRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.DeletedAt == null);
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
            UpdatedAt = DateTime.UtcNow
        };

        // Update tenant status to MovedOut
        tenant.Status = TenantStatus.MovedOut;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            await _unitOfWork.TenantMoveOutRecordRepository.AddAsync(moveOutRecord);
            _unitOfWork.TenantRepository.Update(tenant);
        }, cancellationToken);
    }

    public async Task<TenantMoveOutRecordViewModel> GetByTenantId(Guid tenantId, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.TenantMoveOutRecordRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.DeletedAt == null)
            ?? throw TenantException.NotFoundException("No move-out record found for the specified tenant.");

        var tenant = await _unitOfWork.TenantRepository.FirstOrDefaultAsync(x => x.Id == record.TenantId);
        var apartment = await _unitOfWork.ApartmentRepository.FirstOrDefaultAsync(x => x.Id == record.ApartmentId);
        var processor = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == record.ProcessedBy);

        return new TenantMoveOutRecordViewModel
        {
            Id = record.Id,
            TenantId = record.TenantId,
            TenantName = tenant?.FullName ?? "Unknown Tenant",
            ApartmentId = record.ApartmentId,
            FlatNumber = apartment?.FlatNumber ?? "Unknown Flat",
            MoveOutDate = record.MoveOutDate,
            PendingRent = record.PendingRent,
            DamageAmount = record.DamageAmount,
            RefundAmount = record.RefundAmount,
            IdNumber = record.IdNumber,
            HandoverNote = record.HandoverNote,
            ProcessedBy = record.ProcessedBy,
            ProcessedByName = processor?.Name ?? "System",
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    public async Task UpdateMoveOut(Guid tenantId, TenantMoveOutUpdateRequest request, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.TenantMoveOutRecordRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.DeletedAt == null)
            ?? throw TenantException.NotFoundException("No move-out record found for the specified tenant.");

        record.MoveOutDate = request.MoveOutDate;
        record.PendingRent = request.PendingRent;
        record.DamageAmount = request.DamageAmount;
        record.RefundAmount = request.RefundAmount;
        record.IdNumber = request.IdNumber.Trim();
        record.HandoverNote = request.HandoverNote?.Trim();
        record.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.TenantMoveOutRecordRepository.Update(record);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMoveOut(Guid tenantId, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.TenantMoveOutRecordRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.DeletedAt == null)
            ?? throw TenantException.NotFoundException("No move-out record found for the specified tenant.");

        record.DeletedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.TenantMoveOutRecordRepository.Update(record);

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
