using CleanArchitecture.Shared.Models.User;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface IAuthService
{
    Task<UserSignInResponse> SignIn(UserSignInRequest request);
    Task SignUp(UserSignUpRequest request, CancellationToken token);
    Task ForgotPassword(ForgotPasswordRequest request, CancellationToken token);
    Task<bool> VerifyOtp(VerifyOtpRequest request, CancellationToken token);
    Task ResetPassword(ResetPasswordRequest request, CancellationToken token);
    void Logout();
    Task<string> RefreshToken();
    Task<UserProfileResponse> GetProfile();
}
