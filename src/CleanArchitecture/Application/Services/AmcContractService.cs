using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.AmcContract;

namespace CleanArchitecture.Application.Services;

public class AmcContractService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IAmcContractService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<PaginatedList<AmcContractViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        AmcStatus? status,
        AmcContractType? contractType,
        Guid? vendorId,
        Guid? equipmentId,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var contracts = await _unitOfWork.AmcContractRepository.GetAllAsync(x => x.DeletedAt == null);
        var equipmentList = await _unitOfWork.EquipmentRepository.GetAllAsync(x => x.DeletedAt == null);
        var vendors = await _unitOfWork.VendorRepository.GetAllAsync(x => x.DeletedAt == null);

        var equipmentMap = equipmentList.ToDictionary(e => e.Id, e => e.Name);
        var vendorMap = vendors.ToDictionary(v => v.Id, v => (v.Name, v.CompanyName));

        var query = contracts.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (contractType.HasValue)
        {
            query = query.Where(x => x.ContractType == contractType.Value);
        }

        if (vendorId.HasValue)
        {
            query = query.Where(x => x.VendorId == vendorId.Value);
        }

        if (equipmentId.HasValue)
        {
            query = query.Where(x => x.EquipmentId == equipmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                x.AmcCode.ToLower().Contains(searchLower) ||
                x.ContractNumber.ToLower().Contains(searchLower) ||
                x.ContractTitle.ToLower().Contains(searchLower) ||
                x.CoverageDetails.ToLower().Contains(searchLower) ||
                x.Exclusions.ToLower().Contains(searchLower) ||
                x.Remarks.ToLower().Contains(searchLower) ||
                (equipmentMap.ContainsKey(x.EquipmentId) && equipmentMap[x.EquipmentId].ToLower().Contains(searchLower)) ||
                (vendorMap.ContainsKey(x.VendorId) && (vendorMap[x.VendorId].Name.ToLower().Contains(searchLower) || vendorMap[x.VendorId].CompanyName.ToLower().Contains(searchLower))));
        }

        var totalItems = query.Count();
        var pageEntities = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => new AmcContractViewModel
        {
            Id = x.Id,
            AmcCode = x.AmcCode,
            ContractNumber = x.ContractNumber,
            ContractTitle = x.ContractTitle,
            ContractType = x.ContractType,
            EquipmentId = x.EquipmentId,
            EquipmentName = equipmentMap.TryGetValue(x.EquipmentId, out var eName) ? eName : "Unknown Equipment",
            VendorId = x.VendorId,
            VendorName = vendorMap.TryGetValue(x.VendorId, out var vInfo) ? vInfo.Name : "Unknown Vendor",
            VendorCompanyName = vendorMap.TryGetValue(x.VendorId, out vInfo) ? vInfo.CompanyName : "Unknown Vendor",
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            ContractAmount = x.ContractAmount,
            PaymentTerms = x.PaymentTerms,
            ServiceFrequency = x.ServiceFrequency,
            CoverageDetails = x.CoverageDetails,
            Exclusions = x.Exclusions,
            DocumentLink = x.DocumentLink,
            Remarks = x.Remarks,
            Status = x.Status,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CreatedBy = x.CreatedBy,
            UpdatedBy = x.UpdatedBy,
            DeletedBy = x.DeletedBy,
            RestoredBy = x.RestoredBy
        }).ToList();

        return new PaginatedList<AmcContractViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<AmcContractViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var contract = await _unitOfWork.AmcContractRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw AmcContractException.NotFoundException($"AMC Contract with ID '{id}' was not found.");

        var equipment = await _unitOfWork.EquipmentRepository.FirstOrDefaultAsync(x => x.Id == contract.EquipmentId && x.DeletedAt == null);
        var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == contract.VendorId && x.DeletedAt == null);

        return new AmcContractViewModel
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

    public async Task<AmcContractStatsViewModel> GetStats(CancellationToken cancellationToken)
    {
        var contracts = await _unitOfWork.AmcContractRepository.GetAllAsync(x => x.DeletedAt == null);

        var activeCount = contracts.Count(x => x.Status == AmcStatus.Active);
        var expiringCount = contracts.Count(x => x.Status == AmcStatus.Expiring);
        var expiredCount = contracts.Count(x => x.Status == AmcStatus.Expired);
        var cancelledCount = contracts.Count(x => x.Status == AmcStatus.Cancelled);
        var totalCount = contracts.Count;

        return new AmcContractStatsViewModel
        {
            ActiveCount = activeCount,
            ExpiringCount = expiringCount,
            ExpiredCount = expiredCount,
            CancelledCount = cancelledCount,
            TotalCount = totalCount
        };
    }

    public async Task Create(AmcContractCreateRequest request, CancellationToken cancellationToken)
    {
        var equipmentExists = await _unitOfWork.EquipmentRepository.AnyAsync(x => x.Id == request.EquipmentId && x.DeletedAt == null);
        if (!equipmentExists)
        {
            throw AmcContractException.BadRequestException("The specified equipment does not exist.");
        }

        var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId && x.DeletedAt == null);
        if (!vendorExists)
        {
            throw AmcContractException.BadRequestException("The specified vendor does not exist.");
        }

        string amcCode;
        if (!string.IsNullOrWhiteSpace(request.AmcCode))
        {
            amcCode = request.AmcCode.Trim();
            var codeExists = await _unitOfWork.AmcContractRepository.AnyAsync(x => x.AmcCode.ToLower() == amcCode.ToLower() && x.DeletedAt == null);
            if (codeExists)
            {
                throw AmcContractException.BadRequestException($"AMC Contract with code '{amcCode}' already exists.");
            }
        }
        else
        {
            var count = await _unitOfWork.AmcContractRepository.CountAsync(x => true);
            amcCode = $"AMC-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
        }

        var userId = _currentUser.GetCurrentUserId();

        var contract = new AmcContract
        {
            Id = Guid.NewGuid(),
            AmcCode = amcCode,
            ContractNumber = request.ContractNumber.Trim(),
            ContractTitle = request.ContractTitle.Trim(),
            ContractType = request.ContractType,
            EquipmentId = request.EquipmentId,
            VendorId = request.VendorId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ContractAmount = request.ContractAmount,
            PaymentTerms = request.PaymentTerms,
            ServiceFrequency = request.ServiceFrequency,
            CoverageDetails = request.CoverageDetails?.Trim() ?? string.Empty,
            Exclusions = request.Exclusions?.Trim() ?? string.Empty,
            DocumentLink = request.DocumentLink?.Trim() ?? string.Empty,
            Remarks = request.Remarks?.Trim() ?? string.Empty,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.AmcContractRepository.AddAsync(contract), cancellationToken);
    }

    public async Task Update(Guid id, AmcContractUpdateRequest request, CancellationToken cancellationToken)
    {
        var contract = await _unitOfWork.AmcContractRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw AmcContractException.NotFoundException($"AMC Contract with ID '{id}' was not found.");

        var equipmentExists = await _unitOfWork.EquipmentRepository.AnyAsync(x => x.Id == request.EquipmentId && x.DeletedAt == null);
        if (!equipmentExists)
        {
            throw AmcContractException.BadRequestException("The specified equipment does not exist.");
        }

        var vendorExists = await _unitOfWork.VendorRepository.AnyAsync(x => x.Id == request.VendorId && x.DeletedAt == null);
        if (!vendorExists)
        {
            throw AmcContractException.BadRequestException("The specified vendor does not exist.");
        }

        var userId = _currentUser.GetCurrentUserId();

        contract.ContractNumber = request.ContractNumber.Trim();
        contract.ContractTitle = request.ContractTitle.Trim();
        contract.ContractType = request.ContractType;
        contract.EquipmentId = request.EquipmentId;
        contract.VendorId = request.VendorId;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.ContractAmount = request.ContractAmount;
        contract.PaymentTerms = request.PaymentTerms;
        contract.ServiceFrequency = request.ServiceFrequency;
        contract.CoverageDetails = request.CoverageDetails?.Trim() ?? string.Empty;
        contract.Exclusions = request.Exclusions?.Trim() ?? string.Empty;
        contract.DocumentLink = request.DocumentLink?.Trim() ?? string.Empty;
        contract.Remarks = request.Remarks?.Trim() ?? string.Empty;
        contract.Status = request.Status;
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = userId;

        _unitOfWork.AmcContractRepository.Update(contract);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var contract = await _unitOfWork.AmcContractRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw AmcContractException.NotFoundException($"AMC Contract with ID '{id}' was not found.");

        var now = DateTime.UtcNow;
        var userId = _currentUser.GetCurrentUserId();

        contract.DeletedAt = now;
        contract.UpdatedAt = now;
        contract.DeletedBy = userId;

        _unitOfWork.AmcContractRepository.Update(contract);

        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "AmcContract",
            EntityId = contract.Id,
            EntityTitle = $"{contract.AmcCode} - {contract.ContractTitle}",
            DeletedBy = userId,
            DeletedAt = now
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
