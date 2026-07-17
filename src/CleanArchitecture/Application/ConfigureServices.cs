using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Application.Services;
using CleanArchitecture.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationService(this IServiceCollection services, AppSettings appsettings)
    {
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IMailService, MailService>();
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IBuildingService, BuildingService>();
        services.AddTransient<ISettingService, SettingService>();
        services.AddTransient<IDeletedHistoryService, DeletedHistoryService>();

        services.AddTransient<ICurrentTime, CurrentTime>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddTransient<ITokenService, TokenService>();
        services.AddTransient<ICookieService, CookieService>();

        return services;
    }
}
