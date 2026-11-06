using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sayyit.API.Features.Users.Persistence;
using Sayyit.API.Infrastructure.Errors;
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
            throw new SayyitException(
                "User sync is temporarily unavailable.",
                "Connection string 'ConnectionStrings:SayyitDb' is not configured.",
                StatusCodes.Status503ServiceUnavailable);
        }

        var normalizedUserName = NormalizeOptional(request.UserName, 50);
        var normalizedEmail = NormalizeOptional(request.Email, 320);
        var normalizedDisplayName = NormalizeRequiredDisplayName(request.DisplayName, normalizedUserName, normalizedEmail, request.EntraObjectId);

        try
        {
            var user = await _dbContext.Users
                .SingleOrDefaultAsync(u => u.TenantId == request.TenantId && u.EntraObjectId == request.EntraObjectId, cancellationToken);

            var utcNow = DateTime.UtcNow;

            if (user is null)
            {
                user = new SayyitUser
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
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number is 2627 or 2601)
        {
            throw new SayyitException(
                "Login synchronization encountered an error.",
                $"Unique constraint violation while syncing user data. SQL Number: {sqlEx.Number}.",
                StatusCodes.Status409Conflict);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx)
        {
            throw new SayyitException(
                "Login synchronization encountered an error.",
                $"Database update failed while syncing user data. SQL Number: {sqlEx.Number}.",
                StatusCodes.Status503ServiceUnavailable);
        }
        catch (SqlException ex)
        {
            throw new SayyitException(
                "Login synchronization encountered an error.",
                $"SQL error while syncing user data. SQL Number: {ex.Number}.",
                StatusCodes.Status503ServiceUnavailable);
        }
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
