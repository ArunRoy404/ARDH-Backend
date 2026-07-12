using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

public class AuthController(IAuthService authService) : BaseController
{
    private readonly IAuthService _authService = authService;

    /// <summary>
    /// Sign in to the application.
    /// </summary>
    [HttpPost("sign-in")]
    [SwaggerResponse(200, "Successfully signed in.", typeof(UserSignInResponse))]
    [SwaggerResponse(400, "Invalid email or password.")]
    public async Task<IActionResult> SignIn([FromBody] UserSignInRequest request)
        => Ok(await _authService.SignIn(request));

    /// <summary>
    /// Sign up a new user.
    /// </summary>
    [HttpPost("sign-up")]
    [SwaggerResponse(200, "User registered successfully.")]
    [SwaggerResponse(400, "User already exists or validation failed.")]
    public async Task<IActionResult> SignUp([FromBody] UserSignUpRequest request, CancellationToken token)
    {
        await _authService.SignUp(request, token);
        return Ok(new { message = "User registered successfully." });
    }

    /// <summary>
    /// Request a password reset OTP.
    /// </summary>
    [HttpPost("forgot-password")]
    [SwaggerResponse(200, "OTP code generated and logged successfully.")]
    [SwaggerResponse(400, "User not found.")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken token)
    {
        await _authService.ForgotPassword(request, token);
        return Ok(new { message = "Password reset OTP sent to email." });
    }

    /// <summary>
    /// Verify a password reset OTP.
    /// </summary>
    [HttpPost("verify-otp")]
    [SwaggerResponse(200, "OTP validation result.")]
    [SwaggerResponse(400, "Invalid OTP format.")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken token)
    {
        var isValid = await _authService.VerifyOtp(request, token);
        if (!isValid)
        {
            return BadRequest(new { message = "The OTP code is wrong or has expired." });
        }
        return Ok(new { message = "OTP verified successfully. You can now reset your password." });
    }

    /// <summary>
    /// Reset the user password using a verified OTP.
    /// </summary>
    [HttpPost("reset-password")]
    [SwaggerResponse(200, "Password reset successfully.")]
    [SwaggerResponse(400, "Invalid OTP or passwords do not match.")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken token)
    {
        await _authService.ResetPassword(request, token);
        return Ok(new { message = "Password has been reset successfully." });
    }

    /// <summary>
    /// Log out the user.
    /// </summary>
    [HttpDelete("logout")]
    [SwaggerResponse(200, "Successfully logged out.")]
    public IActionResult Logout()
    {
        _authService.Logout();
        return Ok(new { message = "Successfully logged out." });
    }

    /// <summary>
    /// Refresh the JWT authentication token.
    /// </summary>
    [HttpGet("refresh")]
    [SwaggerResponse(200, "Token refreshed successfully.", typeof(string))]
    [SwaggerResponse(401, "Token is invalid or expired.")]
    public async Task<IActionResult> RefreshToken()
        => Ok(await _authService.RefreshToken());

    /// <summary>
    /// Get the logged-in user profile.
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [SwaggerResponse(200, "Successfully retrieved user profile.", typeof(UserProfileResponse))]
    [SwaggerResponse(401, "User is not authorized.")]
    public async Task<IActionResult> GetProfile()
        => Ok(await _authService.GetProfile());
}
