using Microsoft.Data.SqlClient;
using Sayyit.Contracts.User;

namespace Sayyit.API.Services;

public sealed class BootstrapService
{
    private readonly string _connectionString;

    public BootstrapService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SayyitDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ConnectionStrings:SayyitDb' is not configured.");
        }

        _connectionString = connectionString;
    }

    public async Task BootstrapAsync(UserBootstrap request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedUserName = NormalizeOptional(request.UserName, 50);
        var normalizedEmail = NormalizeOptional(request.Email, 320);
        var normalizedDisplayName = NormalizeRequiredDisplayName(request.DisplayName, normalizedUserName, normalizedEmail, request.EntraObjectId);

        const string sql =
            "UPDATE auth.Users " +
            "SET UserName = @UserName, DisplayName = @DisplayName, Email = @Email, LastSeenUtc = SYSUTCDATETIME(), UpdatedUtc = SYSUTCDATETIME() " +
            "WHERE TenantId = @TenantId AND EntraObjectId = @EntraObjectId; " +
            "IF @@ROWCOUNT = 0 " +
            "BEGIN " +
            "INSERT INTO auth.Users (EntraObjectId, TenantId, UserName, DisplayName, Email, LastSeenUtc, UpdatedUtc) " +
            "VALUES (@EntraObjectId, @TenantId, @UserName, @DisplayName, @Email, SYSUTCDATETIME(), SYSUTCDATETIME()); " +
            "END;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EntraObjectId", request.EntraObjectId);
        command.Parameters.AddWithValue("@TenantId", request.TenantId);
        command.Parameters.AddWithValue("@UserName", (object?)normalizedUserName ?? DBNull.Value);
        command.Parameters.AddWithValue("@DisplayName", normalizedDisplayName);
        command.Parameters.AddWithValue("@Email", (object?)normalizedEmail ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
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
