using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
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
var exposeLogs = builder.Configuration.GetValue<bool>("Diagnostics:ExposeLogs");
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var delimitedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var allowedOrigins = configuredOrigins
    .Concat(delimitedOrigins)
    .Concat([
        "https://localhost:5501",
        "http://localhost:5500",
        "https://sayyit-dev-webapp.azurewebsites.net",
        "https://sayyit-prod-webapp.azurewebsites.net"
    ])
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClientPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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

    if (exposeLogs && sqlConnectionStringBuilder.Authentication == SqlAuthenticationMethod.ActiveDirectoryDefault)
    {
        logger.LogInformation("***\nDiagnostics enabled: API is using Microsoft Entra authentication mode '{Authentication}'.\n***", sqlConnectionStringBuilder.Authentication);
    }
}

logger.LogInformation("Token validation configured. ConfigAuthority: {ConfigAuthority}; ValidationAuthority: {ValidationAuthority}; MetadataAddress: {MetadataAddress}; ValidAudiences: {ValidAudiences}; ValidIssuers: {ValidIssuers}", authority, tenantAuthority, metadataAddress, string.Join(", ", validAudiences), string.Join(", ", validIssuers));

if (exposeLogs)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("WebClientPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine($"[Startup] Environment: {app.Environment.EnvironmentName}\n; " +
    $"AllowedOrigins: {string.Join(", ", allowedOrigins)} \n" +
    $"WebsiteHostName: {websiteHostName} \n");

app.Run();
