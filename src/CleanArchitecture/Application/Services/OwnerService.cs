using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Owner;

namespace CleanArchitecture.Application.Services;

public class OwnerService(IUnitOfWork unitOfWork, ICurrentUser currentUser, INotificationService notificationService) : IOwnerService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<PaginatedList<OwnerViewModel>> GetPaginated(int page, int pageSize, string? search, OwnerStatus? status, CancellationToken cancellationToken)
    {
        var owners = await _unitOfWork.OwnerRepository.GetAllAsync();
        var users = await _unitOfWork.UserRepository.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);
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
        var pageEntities = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageEntities.Select(x => new OwnerViewModel
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
                UpdatedAt = x.UpdatedAt,
                CreatedBy = x.CreatedBy.HasValue && userMap.TryGetValue(x.CreatedBy.Value, out var cb) ? cb : null,
                UpdatedBy = x.UpdatedBy.HasValue && userMap.TryGetValue(x.UpdatedBy.Value, out var ub) ? ub : null,
            })
            .ToList();

        return new PaginatedList<OwnerViewModel>(items, totalCount, page, pageSize);
    }

    public async Task<byte[]> ExportToCsv(
        string? search,
        OwnerStatus? status,
        CancellationToken cancellationToken)
    {
        var owners = await _unitOfWork.OwnerRepository.GetAllAsync();
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

        var list = query.ToList();

        var csv = new StringBuilder();
        csv.AppendLine("ID,FullName,Phone,Email,City,Address,IdType,IdNumber,BankName,AccountNumber,IfscCode,Status,Notes,CreatedAt");

        foreach (var o in list)
        {
            csv.AppendLine($"\"{o.Id}\",\"{EscapeCsv(o.FullName)}\",\"{EscapeCsv(o.Phone)}\",\"{EscapeCsv(o.Email)}\",\"{EscapeCsv(o.City)}\",\"{EscapeCsv(o.Address)}\",\"{o.IdType}\",\"{EscapeCsv(o.IdNumber)}\",\"{EscapeCsv(o.BankName)}\",\"{EscapeCsv(o.AccountNumber)}\",\"{EscapeCsv(o.IfscCode)}\",\"{o.Status}\",\"{EscapeCsv(o.Notes)}\",\"{o.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static string EscapeCsv(string? val)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Replace("\"", "\"\"");
    }

    public async Task<OwnerViewModel> GetById(Guid id, CancellationToken cancellationToken)
    {
        var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw OwnerException.NotFoundException("The specified owner does not exist.");

        var createdByUser = owner.CreatedBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == owner.CreatedBy.Value) : null;
        var updatedByUser = owner.UpdatedBy.HasValue ? await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == owner.UpdatedBy.Value) : null;

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
            UpdatedAt = owner.UpdatedAt,
            CreatedBy = createdByUser?.Name,
            UpdatedBy = updatedByUser?.Name,
        };
    }

    public async Task Create(OwnerCreateRequest request, CancellationToken cancellationToken)
    {
        var isNameExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(x => x.FullName == request.FullName);
        if (isNameExist)
        {
            throw OwnerException.BadRequestException($"Owner with name '{request.FullName}' already exists.");
        }

        var isEmailExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(x => x.Email == request.Email);
        if (isEmailExist)
        {
            throw OwnerException.BadRequestException($"Owner with email '{request.Email}' already exists.");
        }

        var isPhoneExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(x => x.Phone == request.Phone);
        if (isPhoneExist)
        {
            throw OwnerException.BadRequestException($"Owner with phone number '{request.Phone}' already exists.");
        }

        var isIdNumberExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(x => x.IdNumber == request.IdNumber);
        if (isIdNumberExist)
        {
            throw OwnerException.BadRequestException($"Owner with ID number '{request.IdNumber}' already exists.");
        }

        var isAccountNumberExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(x => x.AccountNumber == request.AccountNumber);
        if (isAccountNumberExist)
        {
            throw OwnerException.BadRequestException($"Owner with account number '{request.AccountNumber}' already exists.");
        }

        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            City = request.City ?? string.Empty,
            Address = request.Address ?? string.Empty,
            IdType = request.IdType,
            IdNumber = request.IdNumber,
            BankName = request.BankName,
            AccountNumber = request.AccountNumber,
            IfscCode = request.IfscCode,
            Status = request.Status,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.GetCurrentUserId()
        };

        await _unitOfWork.ExecuteTransactionAsync(async () => await _unitOfWork.OwnerRepository.AddAsync(owner), cancellationToken);

        await _notificationService.CreateNotificationInternal("properties", "Owner Added", $"Owner '{owner.FullName}' was added.", cancellationToken);
    }

    public async Task Update(Guid id, OwnerUpdateRequest request, CancellationToken cancellationToken)
    {
        var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw OwnerException.NotFoundException("The specified owner does not exist.");

        if (owner.FullName != request.FullName)
        {
            var isNameExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(x => x.FullName == request.FullName && x.Id != id);
            if (isNameExist)
            {
                throw OwnerException.BadRequestException($"Owner with name '{request.FullName}' already exists.");
            }
        }

        if (owner.Email != request.Email)
        {
            var isEmailExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(x => x.Email == request.Email && x.Id != id);
            if (isEmailExist)
            {
                throw OwnerException.BadRequestException($"Owner with email '{request.Email}' already exists.");
            }
        }

        if (owner.Phone != request.Phone)
        {
            var isPhoneExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(x => x.Phone == request.Phone && x.Id != id);
            if (isPhoneExist)
            {
                throw OwnerException.BadRequestException($"Owner with phone number '{request.Phone}' already exists.");
            }
        }

        if (owner.IdNumber != request.IdNumber)
        {
            var isIdNumberExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(x => x.IdNumber == request.IdNumber && x.Id != id);
            if (isIdNumberExist)
            {
                throw OwnerException.BadRequestException($"Owner with ID number '{request.IdNumber}' already exists.");
            }
        }

        if (owner.AccountNumber != request.AccountNumber)
        {
            var isAccountNumberExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(x => x.AccountNumber == request.AccountNumber && x.Id != id);
            if (isAccountNumberExist)
            {
                throw OwnerException.BadRequestException($"Owner with account number '{request.AccountNumber}' already exists.");
            }
        }

        owner.FullName = request.FullName;
        owner.Phone = request.Phone;
        owner.Email = request.Email;
        owner.IdType = request.IdType;
        owner.IdNumber = request.IdNumber;
        owner.BankName = request.BankName;
        owner.AccountNumber = request.AccountNumber;
        owner.IfscCode = request.IfscCode;
        owner.Status = request.Status;
        owner.Notes = request.Notes;

        if (request.City != null) owner.City = request.City;
        if (request.Address != null) owner.Address = request.Address;

        owner.UpdatedAt = DateTime.UtcNow;
        owner.UpdatedBy = _currentUser.GetCurrentUserId();
 
        _unitOfWork.OwnerRepository.Update(owner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateNotificationInternal("properties", "Owner Updated", $"Owner '{owner.FullName}' details were updated.", cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        var owner = await _unitOfWork.OwnerRepository.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw OwnerException.NotFoundException("The specified owner does not exist.");
 
        owner.IsDeleted = true;
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

        await _notificationService.CreateNotificationInternal("properties", "Owner Deleted", $"Owner '{owner.FullName}' was deleted.", cancellationToken);
    }
}
