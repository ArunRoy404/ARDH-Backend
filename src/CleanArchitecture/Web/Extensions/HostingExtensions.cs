using CleanArchitecture.Application;
using CleanArchitecture.Application.Common;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Web.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;

namespace CleanArchitecture.Web.Extensions;

public static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder, AppSettings appsettings)
    {
        builder.Services.AddInfrastructuresService(appsettings);
        builder.Services.AddApplicationService(appsettings);
        builder.Services.AddWebAPIService(appsettings);

        return builder.Build();
    }

    public static async Task<WebApplication> ConfigurePipelineAsync(this WebApplication app, AppSettings appsettings)
    {
        using var loggerFactory = LoggerFactory.Create(builder => { });
        using var scope = app.Services.CreateScope();

        var initialize = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
        await initialize.InitializeAsync();

        // Behind Nginx (which terminates TLS and forwards plain HTTP to Kestrel on
        // 127.0.0.1:8080), Kestrel would otherwise see every request as HTTP, making
        // Request.IsHttps false. That breaks CookieService's Secure/SameSite=None
        // logic and any HTTPS-only checks. Trust the proxy's X-Forwarded-Proto/-For
        // headers so Request.IsHttps reflects the original client scheme. The proxy
        // is not directly reachable from the internet (container only binds to
        // 127.0.0.1), so clearing KnownNetworks/KnownProxies to accept it is safe.
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };
        forwardedHeadersOptions.KnownNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedHeadersOptions);

        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.ConfigureExceptionHandler(loggerFactory.CreateLogger("Exceptions"));
        app.UseMiddleware<LoggingMiddleware>();
        app.UseMiddleware<PerformanceMiddleware>();
        app.UseHttpsRedirection();

        var storagePath = System.IO.Path.Combine(app.Environment.ContentRootPath, appsettings.FileStorageSettings.Path);
        if (!System.IO.Directory.Exists(storagePath))
        {
            System.IO.Directory.CreateDirectory(storagePath);
        }
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(storagePath),
            RequestPath = "/" + appsettings.FileStorageSettings.Path.TrimStart('/')
        });

        app.UseCors("AllowSpecificOrigin");
        app.UseSwagger(appsettings);
        app.ConfigureHealthCheck();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

}
