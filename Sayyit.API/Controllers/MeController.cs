using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Sayyit.API.Services;
using Sayyit.Contracts.User;

namespace Sayyit.API.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController(BootstrapService bootstrapService, ILogger<MeController> logger) : ControllerBase
{
    private readonly BootstrapService _bootstrapService = bootstrapService;
    private readonly ILogger<MeController> _logger = logger;

    [HttpPost("bootstrap")]
    public async Task<IActionResult> BootstrapAsync([FromBody] UserBootstrap request, CancellationToken cancellationToken)
    {
        var entraObjectIdClaim = GetClaimValue(User, "oid", "sub");
        var tenantIdClaim = GetClaimValue(User, "tid");

        if (!Guid.TryParse(entraObjectIdClaim, out var entraObjectId) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            _logger.LogWarning("Authenticated token is missing valid oid/tid claims for bootstrap.");
            return Forbid();
        }

        var trustedRequest = new UserBootstrap
        {
            EntraObjectId = entraObjectId,
            TenantId = tenantId,
            UserName = request.UserName,
            DisplayName = request.DisplayName,
            Email = request.Email
        };

        if (string.IsNullOrWhiteSpace(trustedRequest.DisplayName))
        {
            ModelState.AddModelError(nameof(request.DisplayName), "DisplayName is required.");
            return ValidationProblem(ModelState);
        }

        try
        {
            await _bootstrapService.BootstrapAsync(trustedRequest, cancellationToken);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            return Conflict("A unique constraint was violated while syncing user data.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "User bootstrap failed because the API configuration is invalid.");
            return Problem(title: "User sync is temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "User bootstrap failed due to a SQL error with Number {SqlErrorNumber}.", ex.Number);
            return Problem(title: "User sync is temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static string? GetClaimValue(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
