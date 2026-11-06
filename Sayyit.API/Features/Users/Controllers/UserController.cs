using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayyit.API.Services;
using Sayyit.Contracts.User;

namespace Sayyit.API.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class UserController(
    BootstrapService bootstrapService,
    ILogger<UserController> logger) : ControllerBase
{
    private readonly BootstrapService _bootstrapService = bootstrapService;
    private readonly ILogger<UserController> _logger = logger;

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

        await _bootstrapService.BootstrapAsync(trustedRequest, cancellationToken);
        return NoContent();
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
