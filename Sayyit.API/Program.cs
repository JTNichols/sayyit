using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using Sayyit.API.Services;

var builder = WebApplication.CreateBuilder(args);

var websiteHostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
if (builder.Environment.IsProduction() &&
    !string.IsNullOrWhiteSpace(websiteHostName) &&
    websiteHostName.Contains("sayyit-dev-apiapp.azurewebsites.net", StringComparison.OrdinalIgnoreCase))
{
    builder.Configuration.AddJsonFile("appsettings.Dev.json", optional: true, reloadOnChange: false);
}

var authority = builder.Configuration["AzureAd:Authority"];
var audience = builder.Configuration["AzureAd:Audience"];
if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
{
    throw new InvalidOperationException("Configuration values 'AzureAd:Authority' and 'AzureAd:Audience' are required.");
}

authority = authority.TrimEnd('/');
var metadataAddress = $"{authority}/v2.0/.well-known/openid-configuration";
var bareAudience = audience.StartsWith("api://", StringComparison.OrdinalIgnoreCase)
    ? audience["api://".Length..]
    : audience;
var validAudiences = new[] { audience, bareAudience }
    .Where(value => !string.IsNullOrWhiteSpace(value))
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
        options.Authority = authority;
        options.MetadataAddress = metadataAddress;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudiences = validAudiences
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

    if (app.Environment.IsDevelopment() && sqlConnectionStringBuilder.Authentication == SqlAuthenticationMethod.ActiveDirectoryDefault)
    {
        logger.LogInformation("***\nDevelopment environment is using Microsoft Entra local developer authentication (Active Directory Default).\n***");
    }
}

logger.LogInformation("Token validation configured. Authority: {Authority}; MetadataAddress: {MetadataAddress}; ValidAudiences: {ValidAudiences}", authority, metadataAddress, string.Join(", ", validAudiences));

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Dev"))
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
