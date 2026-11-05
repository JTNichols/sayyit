using Microsoft.EntityFrameworkCore;
using Sayyit.API.Features.Users.Models;
using Sayyit.Contracts.User;

namespace Sayyit.API.Services;

public sealed class BootstrapService
{
    private readonly IConfiguration _configuration;
    private readonly SayyitDbContext _dbContext;

    public BootstrapService(IConfiguration configuration, SayyitDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task BootstrapAsync(UserBootstrap request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var connectionString = _configuration.GetConnectionString("SayyitDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ConnectionStrings:SayyitDb' is not configured.");
        }

        var normalizedUserName = NormalizeOptional(request.UserName, 50);
        var normalizedEmail = NormalizeOptional(request.Email, 320);
        var normalizedDisplayName = NormalizeRequiredDisplayName(request.DisplayName, normalizedUserName, normalizedEmail, request.EntraObjectId);

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.TenantId == request.TenantId && u.EntraObjectId == request.EntraObjectId, cancellationToken);

        var utcNow = DateTime.UtcNow;

        if (user is null)
        {
            user = new UserEntity
            {
                EntraObjectId = request.EntraObjectId,
                TenantId = request.TenantId,
                UserName = normalizedUserName,
                DisplayName = normalizedDisplayName,
                Email = normalizedEmail,
                LastSeenUtc = utcNow,
                UpdatedUtc = utcNow
            };

            _dbContext.Users.Add(user);
        }
        else
        {
            user.UserName = normalizedUserName;
            user.DisplayName = normalizedDisplayName;
            user.Email = normalizedEmail;
            user.LastSeenUtc = utcNow;
            user.UpdatedUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim();
        return trimmedValue.Length <= maxLength ? trimmedValue : trimmedValue[..maxLength];
    }

    private static string NormalizeRequiredDisplayName(string? displayName, string? userName, string? email, Guid entraObjectId)
    {
        var normalizedDisplayName = NormalizeOptional(displayName, 100);
        if (!string.IsNullOrWhiteSpace(normalizedDisplayName))
        {
            return normalizedDisplayName;
        }

        var fallback = userName ?? email ?? entraObjectId.ToString();
        return fallback.Length <= 100 ? fallback : fallback[..100];
    }
}
