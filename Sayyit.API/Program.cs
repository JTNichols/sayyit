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

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<BootstrapService>();

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

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Dev"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("WebClientPolicy");
app.UseAuthorization();

app.MapControllers();

app.Run();
