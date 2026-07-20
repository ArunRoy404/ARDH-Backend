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
    ICurrentUser currentUser) : IVendorService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<PaginatedList<VendorViewModel>> GetPaginated(
        int page,
        int pageSize,
        string? search,
        VendorType? vendorType,
        VendorStatus? status,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var vendors = await _unitOfWork.VendorRepository.GetAllAsync(x => x.DeletedAt == null);
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

        if (vendorType.HasValue)
        {
            query = query.Where(x => x.VendorType == vendorType.Value);
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

        var items = pageEntities.Select(MapToViewModel).ToList();

        return new PaginatedList<VendorViewModel>(items, totalItems, page, pageSize);
    }

    public async Task<VendorViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw VendorException.NotFoundException($"Vendor with ID '{id}' was not found.");

        return MapToViewModel(vendor);
    }

    public async Task Create(VendorCreateRequest request, CancellationToken cancellationToken)
    {
        var existingEmail = await _unitOfWork.VendorRepository.AnyAsync(x =>
            x.Email.ToLower() == request.Email.Trim().ToLower() && x.DeletedAt == null);

        if (existingEmail)
        {
            throw VendorException.BadRequestException($"A vendor with email '{request.Email}' already exists.");
        }

        var existingPhone = await _unitOfWork.VendorRepository.AnyAsync(x =>
            x.Phone.Trim() == request.Phone.Trim() && x.DeletedAt == null);

        if (existingPhone)
        {
            throw VendorException.BadRequestException($"A vendor with phone '{request.Phone}' already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.GstNumber))
        {
            var existingGst = await _unitOfWork.VendorRepository.AnyAsync(x =>
                x.GstNumber.Trim().ToLower() == request.GstNumber.Trim().ToLower() && x.DeletedAt == null);

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
            Email = request.Email.Trim(),
            VendorType = request.VendorType,
            GstNumber = request.GstNumber.Trim(),
            Address = request.Address.Trim(),
            Status = request.Status,
            Notes = request.Notes?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.VendorRepository.AddAsync(vendor), cancellationToken);
    }

    public async Task Update(Guid id, VendorUpdateRequest request, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw VendorException.NotFoundException($"Vendor with ID '{id}' was not found.");

        if (!string.Equals(vendor.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingEmail = await _unitOfWork.VendorRepository.AnyAsync(x =>
                x.Id != id && x.Email.ToLower() == request.Email.Trim().ToLower() && x.DeletedAt == null);

            if (existingEmail)
            {
                throw VendorException.BadRequestException($"Another vendor with email '{request.Email}' already exists.");
            }
        }

        if (!string.Equals(vendor.Phone, request.Phone, StringComparison.OrdinalIgnoreCase))
        {
            var existingPhone = await _unitOfWork.VendorRepository.AnyAsync(x =>
                x.Id != id && x.Phone.Trim() == request.Phone.Trim() && x.DeletedAt == null);

            if (existingPhone)
            {
                throw VendorException.BadRequestException($"Another vendor with phone '{request.Phone}' already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.GstNumber) && !string.Equals(vendor.GstNumber, request.GstNumber, StringComparison.OrdinalIgnoreCase))
        {
            var existingGst = await _unitOfWork.VendorRepository.AnyAsync(x =>
                x.Id != id && x.GstNumber.Trim().ToLower() == request.GstNumber.Trim().ToLower() && x.DeletedAt == null);

            if (existingGst)
            {
                throw VendorException.BadRequestException($"Another vendor with GST number '{request.GstNumber}' already exists.");
            }
        }

        vendor.Name = request.Name.Trim();
        vendor.CompanyName = request.CompanyName.Trim();
        vendor.Phone = request.Phone.Trim();
        vendor.Email = request.Email.Trim();
        vendor.VendorType = request.VendorType;
        vendor.GstNumber = request.GstNumber.Trim();
        vendor.Address = request.Address.Trim();
        vendor.Status = request.Status;
        vendor.Notes = request.Notes?.Trim() ?? string.Empty;
        vendor.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.VendorRepository.Update(vendor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await _unitOfWork.VendorRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw VendorException.NotFoundException($"Vendor with ID '{id}' was not found.");

        var now = DateTime.UtcNow;
        vendor.DeletedAt = now;
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
    }

    private static VendorViewModel MapToViewModel(Vendor vendor)
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
            UpdatedAt = vendor.UpdatedAt
        };
    }
}
