using Charley.Common;
using Charley.Core;
using Charley.Repositories;
using Charley.WebApi.Middleware;
using Charley.WebApi.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Data.SqlClient;
using Npgsql;
using TestControl.AppServices;
using TestControl.Infrastructure;
using TestControl.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("secrets.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

var dbEngine = Environment.GetEnvironmentVariable(Constants.EnvironmentVariableNames.DbEngine);
var dbVersion = Environment.GetEnvironmentVariable(Constants.EnvironmentVariableNames.DbVersion);

// Validate the inputs
if (string.IsNullOrWhiteSpace(dbEngine) || string.IsNullOrWhiteSpace(dbVersion))
{
    throw new InvalidOperationException("Database engine or version not set.");
}

// Parse dbEngine to an enum (adjust based on your actual enum type)
if (!Enum.TryParse(dbEngine, true, out DbEngine engine))
{
    throw new InvalidOperationException($"Invalid database engine: {dbEngine}");
}

// Parse dbVersion (assuming it’s in a format like "db1", adjust as needed)
if (!dbVersion.StartsWith("db", StringComparison.OrdinalIgnoreCase) ||
    !int.TryParse(dbVersion.AsSpan(2), out int version))
{
    throw new InvalidOperationException($"Invalid database version: {dbVersion}");
}

builder.Services.Configure<JsonOptions>(o =>
{
    o.SerializerOptions.IncludeFields = true;
});

builder.Services.AddSingleton(StartupServices.GetSqlProvider(dbEngine, version));

// Get Connection Strings
var commandConnectionString = builder.Configuration.GetConnectionString($"{dbVersion}{dbEngine}Command");
var queryConnectionString = builder.Configuration.GetConnectionString($"{dbVersion}{dbEngine}Query");

if (string.IsNullOrWhiteSpace(commandConnectionString) || string.IsNullOrWhiteSpace(queryConnectionString))
{
    throw new InvalidOperationException($"Connection strings for {dbEngine} are missing in configuration json files.");
}

builder.Services.AddScoped<IOperationContext, OperationContext>();

builder.Services.AddSingleton(_ => new DbProperties(
    commandConnectionString, queryConnectionString, dbVersion, engine));

builder.Services.AddControllers();

builder.Services.AddSingleton<TestMetricsService>();

/*
 * Start tested services; these are central to the test.
 * IOperationContext should be scoped in each test subject (e.g., Test.Charley, Test.Charley, etc. etc.)
 */
builder.Services.AddSingleton<DbMaintenanceService>();
builder.Services.AddSingleton<IReportService, ReportService>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IOrganizationService, OrganizationService>();
builder.Services.AddSingleton<IUserTransactionRepository, UserTransactionRepository>();
/*  */

builder.Services.AddHttpContextAccessor();

// Build the application
var app = builder.Build();

// Configure Middleware
app.UseMiddleware<NotFoundLoggingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<OperationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
