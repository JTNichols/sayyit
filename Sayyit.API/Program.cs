using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using Sayyit.API.Infrastructure.Errors;
using Sayyit.API.Services;

var builder = WebApplication.CreateBuilder(args);

var websiteHostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
if (!string.IsNullOrWhiteSpace(websiteHostName) &&
    websiteHostName.Contains("sayyit-dev-apiapp.azurewebsites.net", StringComparison.OrdinalIgnoreCase))
{
    builder.Configuration.AddJsonFile("appsettings.Dev.json", optional: true, reloadOnChange: false);
}

var authority = builder.Configuration["AzureAd:Authority"];
var audience = builder.Configuration["AzureAd:Audience"];
var tenantId = builder.Configuration["AzureAd:TenantId"];

var missingAzureAdSettings = new List<string>();
if (string.IsNullOrWhiteSpace(authority))
{
    missingAzureAdSettings.Add("AzureAd:Authority");
}

if (string.IsNullOrWhiteSpace(audience))
{
    missingAzureAdSettings.Add("AzureAd:Audience");
}

if (string.IsNullOrWhiteSpace(tenantId))
{
    missingAzureAdSettings.Add("AzureAd:TenantId");
}

if (missingAzureAdSettings.Count > 0)
{
    var missingSettingsMessage = $"Missing required configuration setting(s): {string.Join(", ", missingAzureAdSettings)}. Environment: {builder.Environment.EnvironmentName}; WEBSITE_HOSTNAME: {websiteHostName ?? "<null>"}.";
    Console.Error.WriteLine($"[StartupConfig] {missingSettingsMessage}");
    throw new InvalidOperationException(missingSettingsMessage);
}

authority = authority.TrimEnd('/');
if (authority.EndsWith("/v2.0", StringComparison.OrdinalIgnoreCase))
{
    authority = authority[..^"/v2.0".Length];
}

var tenantAuthority = $"https://{tenantId}.ciamlogin.com/{tenantId}";
var metadataAddress = $"{tenantAuthority}/v2.0/.well-known/openid-configuration";
var customDomainIssuer = $"{authority}/v2.0";
var tenantIdIssuer = $"{tenantAuthority}/v2.0";
var bareAudience = audience.StartsWith("api://", StringComparison.OrdinalIgnoreCase)
    ? audience["api://".Length..]
    : audience;
var validAudiences = new[] { audience, bareAudience }
    .Where(value => !string.IsNullOrWhiteSpace(value))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
var validIssuers = new[] { customDomainIssuer, tenantIdIssuer }
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddControllers();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["traceId"] = traceId;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<SayyitDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SayyitDb");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'ConnectionStrings:SayyitDb' is not configured.");
    }

    options.UseSqlServer(connectionString);
});
builder.Services.AddScoped<BootstrapService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = tenantAuthority;
        options.MetadataAddress = metadataAddress;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudiences = validAudiences,
            ValidIssuers = validIssuers
        };
    });

builder.Services.AddAuthorization();

// Local API CORS
var configuredDevOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
string[] allowedOrigins = builder.Environment.IsDevelopment()
    ? (configuredDevOrigins is { Length: > 0 }
        ? configuredDevOrigins
        : ["https://localhost:5501", "http://localhost:5500"])
    : [];

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WebClientPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var sayyitDbConnectionString = app.Configuration.GetConnectionString("SayyitDb");

if (string.IsNullOrWhiteSpace(sayyitDbConnectionString))
{
    logger.LogWarning("Connection string 'ConnectionStrings:SayyitDb' is not configured.");
}
else
{
    var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(sayyitDbConnectionString);
    logger.LogInformation(
        "SayyitDb configured for server '{Server}', database '{Database}', authentication '{Authentication}'.",
        sqlConnectionStringBuilder.DataSource,
        sqlConnectionStringBuilder.InitialCatalog,
        sqlConnectionStringBuilder.Authentication); 
}

logger.LogInformation("Token validation configured. ConfigAuthority: {ConfigAuthority}; ValidationAuthority: {ValidationAuthority}; MetadataAddress: {MetadataAddress}; ValidAudiences: {ValidAudiences}; ValidIssuers: {ValidIssuers}", authority, tenantAuthority, metadataAddress, string.Join(", ", validAudiences), string.Join(", ", validIssuers));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
// Give JSON error responses for empty errors w/o overriding real responses/auth headers
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var response = httpContext.Response;

    if (response.HasStarted) { return; } 
    if (response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden) { return; } 
    if (response.ContentLength is > 0) { return; } 
    if (!string.IsNullOrWhiteSpace(response.ContentType)) { return; } 
    if (response.StatusCode is < 400 or > 599) { return; }

    var title = ReasonPhrases.GetReasonPhrase(response.StatusCode);
    if (string.IsNullOrWhiteSpace(title)) { title = "Error"; }

    var problemDetails = new ProblemDetails
    {
        Type = "about:blank",
        Title = title,
        Status = response.StatusCode
    };

    var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
    await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
        HttpContext = httpContext,
        ProblemDetails = problemDetails,
        Exception = null
    });
});

app.UseHttpsRedirection();
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors("WebClientPolicy");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var corsDescription = app.Environment.IsDevelopment()
    ? $"ASP.NET Core; AllowedOrigins: {string.Join(", ", allowedOrigins)}"
    : "Azure App Service; allowed origins managed by Bicep";

Console.WriteLine(
    $"[Startup] Environment: {app.Environment.EnvironmentName}\n" +
    $"CORS: {corsDescription}\n" +
    $"WebsiteHostName: {websiteHostName ?? "<null>"}\n");

app.Run();