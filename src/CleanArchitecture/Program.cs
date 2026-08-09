using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Web.Extensions;

// Load the local .env file (if present) into environment variables before configuration
// is built, so secrets (API keys, connection strings) stay out of the repository.
DotEnvExtension.LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration.Get<AppSettings>()
    ?? throw ProgramException.AppsettingNotSetException();

builder.Services.AddSingleton(configuration);
var app = await builder.ConfigureServices(configuration).ConfigurePipelineAsync(configuration);

await app.RunAsync();

// this line for integration test
public partial class Program { }
