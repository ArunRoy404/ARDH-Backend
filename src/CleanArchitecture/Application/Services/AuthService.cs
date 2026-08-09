using AutoMapper;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.User;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Services;

public class AuthService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ITokenService tokenService,
    ICurrentUser currentUser,
    ICookieService cookieService,
    IMailService mailService,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly ICookieService _cookieService = cookieService;
    private readonly IMailService _mailService = mailService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ILogger<AuthService> _logger = logger;

    public async Task<UserSignInResponse> SignIn(UserSignInRequest request)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Email == request.Email)
            ?? throw UserException.BadRequestException(UserErrorMessage.UserNotExist);

        if (!user.IsActive)
        {
            throw UserException.BadRequestException("This user account is inactive.");
        }

        if (!StringHelper.Verify(request.Password, user.PasswordHash))
        {
            throw UserException.BadRequestException(UserErrorMessage.PasswordIncorrect);
        }

        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(default);

        var token = _tokenService.GenerateToken(user, request.RememberMe);
        _cookieService.Set(token, request.RememberMe);

        return new UserSignInResponse { Message = "Successfully signed in." };
    }

    public async Task ForgotPassword(ForgotPasswordRequest request, CancellationToken token)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Email == request.Email)
            ?? throw UserException.BadRequestException(UserErrorMessage.UserNotExist);

        var resetPassword = new ForgotPassword
        {
            UserId = user.Id,
            Email = user.Email,
            OTP = "123456",
            Token = Guid.NewGuid().ToString(),
            DateTime = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteTransactionAsync(
            async () => await _unitOfWork.ForgotPasswordRepository.AddAsync(resetPassword), token);

        await SendOtpEmailAsync(user.Email, resetPassword.OTP);
    }

    public async Task<bool> VerifyOtp(VerifyOtpRequest request, CancellationToken token)
    {
        // Get the latest OTP for this email
        var otpDetails = await _unitOfWork.ForgotPasswordRepository.FirstOrDefaultAsync(
            filter: x => x.Email == request.Email && x.OTP == request.OTP,
            sort: x => x.DateTime,
            ascending: false
        );

        if (otpDetails == null)
        {
            return false;
        }

        // Validate expiration (10 minutes)
        if ((DateTime.UtcNow - otpDetails.DateTime).TotalMinutes > 10)
        {
            return false;
        }

        return true;
    }

    public async Task ResetPassword(ResetPasswordRequest request, CancellationToken token)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Email == request.Email)
            ?? throw UserException.BadRequestException(UserErrorMessage.UserNotExist);

        // Verify the OTP is valid first
        var isValidOtp = await VerifyOtp(new VerifyOtpRequest { Email = request.Email, OTP = request.OTP }, token);
        if (!isValidOtp)
        {
            throw UserException.BadRequestException("The OTP code is wrong or has expired.");
        }

        user.PasswordHash = request.NewPassword.Hash();
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(token);
    }

    public void Logout()
    {
        try
        {
            _ = _cookieService.Get();
            _cookieService.Delete();
        }
        catch { }
    }

    public async Task<UserProfileResponse> GetProfile()
    {
        var userId = _currentUser.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            throw UserException.UserUnauthorizedException();
        }

        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw UserException.BadRequestException(UserErrorMessage.UserNotExist);

        var result = _mapper.Map<UserProfileResponse>(user);
        return result;
    }

    public async Task ResendOtp(ResendOtpRequest request, CancellationToken token)
    {
        var user = await _unitOfWork.UserRepository.FirstOrDefaultAsync(x => x.Email == request.Email)
            ?? throw UserException.BadRequestException(UserErrorMessage.UserNotExist);

        var resetPassword = new ForgotPassword
        {
            UserId = user.Id,
            Email = user.Email,
            OTP = "123456",
            Token = Guid.NewGuid().ToString(),
            DateTime = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteTransactionAsync(
            async () => await _unitOfWork.ForgotPasswordRepository.AddAsync(resetPassword), token);

        await SendOtpEmailAsync(user.Email, resetPassword.OTP);
    }

    private async Task SendOtpEmailAsync(string email, string otp)
    {
        var subject = "Ardh - Password Reset OTP";
        var htmlMessage = $@"
            <div style=""font-family:Arial, sans-serif; max-width:480px; margin:0 auto; padding:24px; border:1px solid #e5e7eb; border-radius:12px;"">
                <h2 style=""margin:0 0 8px; color:#111827;"">Password Reset Request</h2>
                <p style=""margin:0 0 16px; color:#374151;"">Use the one-time code below to reset your password. It expires in 10 minutes.</p>
                <div style=""padding:16px; background:#f3f4f6; border-radius:8px; text-align:center; font-size:32px; font-weight:bold; letter-spacing:8px; color:#1f2937;"">{otp}</div>
                <p style=""margin:16px 0 0; color:#6b7280; font-size:12px;"">If you did not request this, you can safely ignore this email.</p>
            </div>";

        _logger.LogInformation("Password reset OTP generated for {email}: {otp}", email, otp);

        await _mailService.SendEmailAsync(email, subject, htmlMessage);
        _logger.LogInformation("Password reset OTP sent by email to {email}", email);
    }
}
