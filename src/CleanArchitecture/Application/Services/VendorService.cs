using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Vendor;

namespace CleanArchitecture.Application.Services;

public class VendorService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    INotificationService notificationService) : IVendorService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<PaginatedList<VendorViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        string? vendorType,
        VendorStatus? status,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var vendors = await _unitOfWork.VendorRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);
        var query = vendors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(searchLower) ||
                x.CompanyName.ToLower().Contains(searchLower) ||
                x.Phone.ToLower().Contains(searchLower) ||
                x.Email.ToLower().Contains(searchLower) ||
                x.GstNumber.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrWhiteSpace(vendorType))
        {
            var vendorTypeTrimmed = vendorType.Trim();
            query = query.Where(x => x.VendorType.Equals(vendorTypeTrimmed, StringComparison.OrdinalIgnoreCase));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalItems = query.Count();
        var pageEntities = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => MapToViewModel(x, userMap)).ToList();

        return new PaginatedList<VendorViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<VendorViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw VendorException.NotFoundException($"Vendor with ID '{id}' was not found.");

        var createdByUser = vendor.CreatedBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == vendor.CreatedBy.Value) : null;
        var updatedByUser = vendor.UpdatedBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == vendor.UpdatedBy.Value) : null;
        var vendorUserMap = new Dictionary<Guid, string>();
        if (createdByUser != null) vendorUserMap[vendor.CreatedBy!.Value] = createdByUser.Name;
        if (updatedByUser != null) vendorUserMap[vendor.UpdatedBy!.Value] = updatedByUser.Name;

        return MapToViewModel(vendor, vendorUserMap);
    }

    public async Task Create(VendorCreateRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingEmail = await _unitOfWork.VendorRepository.AnyIncludingDeletedAsync(x =>
                x.Email.ToLower() == request.Email.Trim().ToLower());

            if (existingEmail)
            {
                throw VendorException.BadRequestException($"A vendor with email '{request.Email}' already exists.");
            }
        }

        var existingPhone = await _unitOfWork.VendorRepository.AnyIncludingDeletedAsync(x =>
            x.Phone.Trim() == request.Phone.Trim());

        if (existingPhone)
        {
            throw VendorException.BadRequestException($"A vendor with phone '{request.Phone}' already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.GstNumber))
        {
            var existingGst = await _unitOfWork.VendorRepository.AnyIncludingDeletedAsync(x =>
                x.GstNumber.Trim().ToLower() == request.GstNumber.Trim().ToLower());

            if (existingGst)
            {
                throw VendorException.BadRequestException($"A vendor with GST number '{request.GstNumber}' already exists.");
            }
        }

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CompanyName = request.CompanyName.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email?.Trim() ?? string.Empty,
            VendorType = request.VendorType.Trim(),
            GstNumber = request.GstNumber?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            Status = request.Status,
            Notes = request.Notes?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.VendorRepository.AddAsync(vendor), cancellationToken);

        await _notificationService.CreateNotificationInternal("operations", "Vendor Added", $"Vendor '{vendor.Name}' ({vendor.CompanyName}) was added.", cancellationToken);
    }

    public async Task Update(Guid id, VendorUpdateRequest request, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw VendorException.NotFoundException($"Vendor with ID '{id}' was not found.");

        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(vendor.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingEmail = await _unitOfWork.VendorRepository.AnyIncludingDeletedAsync(x =>
                x.Id != id && x.Email.ToLower() == request.Email.Trim().ToLower());

            if (existingEmail)
            {
                throw VendorException.BadRequestException($"Another vendor with email '{request.Email}' already exists.");
            }
        }

        if (!string.Equals(vendor.Phone, request.Phone, StringComparison.OrdinalIgnoreCase))
        {
            var existingPhone = await _unitOfWork.VendorRepository.AnyIncludingDeletedAsync(x =>
                x.Id != id && x.Phone.Trim() == request.Phone.Trim());

            if (existingPhone)
            {
                throw VendorException.BadRequestException($"Another vendor with phone '{request.Phone}' already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.GstNumber) && !string.Equals(vendor.GstNumber, request.GstNumber, StringComparison.OrdinalIgnoreCase))
        {
            var existingGst = await _unitOfWork.VendorRepository.AnyIncludingDeletedAsync(x =>
                x.Id != id && x.GstNumber.Trim().ToLower() == request.GstNumber.Trim().ToLower());

            if (existingGst)
            {
                throw VendorException.BadRequestException($"Another vendor with GST number '{request.GstNumber}' already exists.");
            }
        }

        vendor.Name = request.Name.Trim();
        vendor.CompanyName = request.CompanyName.Trim();
        vendor.Phone = request.Phone.Trim();
        vendor.Email = request.Email?.Trim() ?? string.Empty;
        vendor.VendorType = request.VendorType.Trim();
        vendor.GstNumber = request.GstNumber?.Trim() ?? string.Empty;
        vendor.Address = request.Address?.Trim() ?? string.Empty;
        vendor.Status = request.Status;
        vendor.Notes = request.Notes?.Trim() ?? string.Empty;
        vendor.UpdatedAt = DateTime.UtcNow;
        vendor.UpdatedBy = _currentUser.GetCurrentUserId();

        _unitOfWork.VendorRepository.Update(vendor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationInternal("operations", "Vendor Updated", $"Vendor '{vendor.Name}' details were updated.", cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw VendorException.NotFoundException($"Vendor with ID '{id}' was not found.");

        var now = DateTime.UtcNow;

        vendor.IsDeleted = true;
        vendor.UpdatedAt = now;
        _unitOfWork.VendorRepository.Update(vendor);

        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "Vendor",
            EntityId = vendor.Id,
            EntityTitle = $"{vendor.Name} ({vendor.CompanyName})",
            DeletedBy = _currentUser.GetCurrentUserId(),
            DeletedAt = now
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationInternal("operations", "Vendor Deleted", $"Vendor '{vendor.Name}' was deleted.", cancellationToken);
    }

    private static VendorViewModel MapToViewModel(Vendor vendor, Dictionary<Guid, string>? userMap = null)
    {
        return new VendorViewModel
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
            CreatedBy = vendor.CreatedBy.HasValue && userMap != null && userMap.TryGetValue(vendor.CreatedBy.Value, out var cb) ? cb : null,
            UpdatedBy = vendor.UpdatedBy.HasValue && userMap != null && userMap.TryGetValue(vendor.UpdatedBy.Value, out var ub) ? ub : null,
        };
    }
}
