using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Sayyit.API.Services;
using Sayyit.Contracts.User;

namespace Sayyit.API.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController(BootstrapService bootstrapService) : ControllerBase
{
    private readonly BootstrapService _bootstrapService = bootstrapService;

    [HttpPost("bootstrap")]
    public async Task<IActionResult> BootstrapAsync([FromBody] UserBootstrap request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            ModelState.AddModelError(nameof(request.DisplayName), "DisplayName is required.");
            return ValidationProblem(ModelState);
        }

        try
        {
            await _bootstrapService.BootstrapAsync(request, cancellationToken);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            return Conflict("A unique constraint was violated while syncing user data.");
        }
    }
}
