using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Models.Setting;

namespace CleanArchitecture.Application.Services;

public class SettingService(IUnitOfWork unitOfWork, ICurrentUser currentUser) : ISettingService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<SettingViewModel> Get(CancellationToken cancellationToken)
    {
        var setting = await _unitOfWork.SettingRepository.FirstOrDefaultAsync(x => true);
        if (setting == null)
        {
            throw SettingException.NotFoundException("Application settings not found.");
        }

        return new SettingViewModel
        {
            Id = setting.Id,
            CompanyName = setting.CompanyName,
            CompanyEmail = setting.CompanyEmail,
            Phone = setting.Phone,
            Address = setting.Address,
            Icon = setting.Icon,
            Fav = setting.Fav,
            UpdatedBy = setting.UpdatedBy,
            UpdatedAt = setting.UpdatedAt
        };
    }

    public async Task Update(SettingUpdateRequest request, CancellationToken cancellationToken)
    {
        var setting = await _unitOfWork.SettingRepository.FirstOrDefaultAsync(x => true);
        if (setting == null)
        {
            throw SettingException.NotFoundException("Application settings not found.");
        }

        // Verify admin password before allowing update to general settings
        bool isValidPassword = false;
        try
        {
            isValidPassword = StringHelper.Verify(request.AdminPassword, setting.AdminPassword);
        }
        catch
        {
            // Verify might throw if it's not a valid BCrypt hash
        }

        if (!isValidPassword)
        {
            throw SettingException.BadRequestException("Invalid Admin password.");
        }

        setting.CompanyName = request.CompanyName;
        setting.CompanyEmail = request.CompanyEmail;
        setting.Phone = request.Phone;
        setting.Address = request.Address;
        setting.Icon = request.Icon;
        setting.Fav = request.Fav;

        var userId = _currentUser.GetCurrentUserId();
        if (userId != Guid.Empty)
        {
            setting.UpdatedBy = userId;
        }
        setting.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.SettingRepository.Update(setting);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePassword(SettingUpdatePasswordRequest request, CancellationToken cancellationToken)
    {
        var setting = await _unitOfWork.SettingRepository.FirstOrDefaultAsync(x => true);
        if (setting == null)
        {
            throw SettingException.NotFoundException("Application settings not found.");
        }

        bool isValidPassword = false;
        try
        {
            isValidPassword = StringHelper.Verify(request.CurrentPassword, setting.AdminPassword);
        }
        catch
        {
            // Verify might throw if it's not a valid BCrypt hash
        }

        if (!isValidPassword)
        {
            throw SettingException.BadRequestException("Invalid current password.");
        }

        setting.AdminPassword = request.NewPassword.Hash();

        var userId = _currentUser.GetCurrentUserId();
        if (userId != Guid.Empty)
        {
            setting.UpdatedBy = userId;
        }
        setting.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.SettingRepository.Update(setting);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
