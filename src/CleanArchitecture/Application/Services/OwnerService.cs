using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Owner;

namespace CleanArchitecture.Application.Services;

public class OwnerService(IUnitOfWork unitOfWork, ICurrentUser currentUser) : IOwnerService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<PaginatedList<OwnerViewModel>> GetPaginated(int page, int pageSize, string? search, OwnerStatus? status, CancellationToken cancellationToken)
    {
        var owners = await _unitOfWork.OwnerRepository.GetAllAsync(x => x.DeletedAt == null);
        var query = owners.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(x =>
                (x.FullName != null && x.FullName.ToLower().Contains(cleanSearch)) ||
                (x.Email != null && x.Email.ToLower().Contains(cleanSearch)) ||
                (x.Phone != null && x.Phone.ToLower().Contains(cleanSearch)) ||
                (x.City != null && x.City.ToLower().Contains(cleanSearch))
            );
        }

        var totalCount = query.Count();
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OwnerViewModel
            {
                Id = x.Id,
                FullName = x.FullName,
                Phone = x.Phone,
                Email = x.Email,
                City = x.City,
                Address = x.Address,
                IdType = x.IdType,
                IdNumber = x.IdNumber,
                BankName = x.BankName,
                AccountNumber = x.AccountNumber,
                IfscCode = x.IfscCode,
                Status = x.Status,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToList();

        return new PaginatedList<OwnerViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<OwnerViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw OwnerException.NotFoundException("The specified owner does not exist.");

        return new OwnerViewModel
        {
            Id = owner.Id,
            FullName = owner.FullName,
            Phone = owner.Phone,
            Email = owner.Email,
            City = owner.City,
            Address = owner.Address,
            IdType = owner.IdType,
            IdNumber = owner.IdNumber,
            BankName = owner.BankName,
            AccountNumber = owner.AccountNumber,
            IfscCode = owner.IfscCode,
            Status = owner.Status,
            Notes = owner.Notes,
            CreatedAt = owner.CreatedAt,
            UpdatedAt = owner.UpdatedAt
        };
    }

    public async Task Create(OwnerCreateRequest request, CancellationToken cancellationToken)
    {
        var isEmailExist = await _unitOfWork.OwnerRepository.AnyAsync(x => x.Email == request.Email && x.DeletedAt == null);
        if (isEmailExist)
        {
            throw OwnerException.BadRequestException($"Owner with email '{request.Email}' already exists.");
        }

        var isIdNumberExist = await _unitOfWork.OwnerRepository.AnyAsync(x => x.IdNumber == request.IdNumber && x.DeletedAt == null);
        if (isIdNumberExist)
        {
            throw OwnerException.BadRequestException($"Owner with ID number '{request.IdNumber}' already exists.");
        }

        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            City = request.City,
            Address = request.Address,
            IdType = request.IdType,
            IdNumber = request.IdNumber,
            BankName = request.BankName,
            AccountNumber = request.AccountNumber,
            IfscCode = request.IfscCode,
            Status = request.Status,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.OwnerRepository.AddAsync(owner), cancellationToken);
    }

    public async Task Update(OwnerUpdateRequest request, CancellationToken cancellationToken)
    {
        var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == request.Id && x.DeletedAt == null)
            ?? throw OwnerException.NotFoundException("The specified owner does not exist.");

        if (owner.Email != request.Email)
        {
            var isEmailExist = await _unitOfWork.OwnerRepository.AnyAsync(x => x.Email == request.Email && x.Id != request.Id && x.DeletedAt == null);
            if (isEmailExist)
            {
                throw OwnerException.BadRequestException($"Owner with email '{request.Email}' already exists.");
            }
        }

        if (owner.IdNumber != request.IdNumber)
        {
            var isIdNumberExist = await _unitOfWork.OwnerRepository.AnyAsync(x => x.IdNumber == request.IdNumber && x.Id != request.Id && x.DeletedAt == null);
            if (isIdNumberExist)
            {
                throw OwnerException.BadRequestException($"Owner with ID number '{request.IdNumber}' already exists.");
            }
        }

        owner.FullName = request.FullName;
        owner.Phone = request.Phone;
        owner.Email = request.Email;
        owner.City = request.City;
        owner.Address = request.Address;
        owner.IdType = request.IdType;
        owner.IdNumber = request.IdNumber;
        owner.BankName = request.BankName;
        owner.AccountNumber = request.AccountNumber;
        owner.IfscCode = request.IfscCode;
        owner.Status = request.Status;
        owner.Notes = request.Notes;
        owner.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.OwnerRepository.Update(owner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null)
            ?? throw OwnerException.NotFoundException("The specified owner does not exist.");

        owner.DeletedAt = DateTime.UtcNow;
        owner.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.OwnerRepository.Update(owner);

        // Record soft-delete history
        var history = new DeletedHistory
        {
            Id = Guid.NewGuid(),
            EntityType = "Owner",
            EntityId = owner.Id,
            EntityTitle = $"{owner.FullName}-{owner.City}-{owner.CreatedAt}",
            DeletedBy = _currentUser.GetCurrentUserId(),
            DeletedAt = DateTime.UtcNow
        };
        await _unitOfWork.DeletedHistoryRepository.AddAsync(history);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
