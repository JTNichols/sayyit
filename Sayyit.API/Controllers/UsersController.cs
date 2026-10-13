using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Sayyit.API.Services;
using Sayyit.Contracts.User;

namespace Sayyit.API.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(UserSyncService userSyncService) : ControllerBase
{
    private readonly UserSyncService _userSyncService = userSyncService;

    [HttpPost("sync")]
    public async Task<IActionResult> SyncAsync([FromBody] UserSync request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            ModelState.AddModelError(nameof(request.DisplayName), "DisplayName is required.");
            return ValidationProblem(ModelState);
        }

        try
        {
            await _userSyncService.SyncAsync(request, cancellationToken);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            return Conflict("A unique constraint was violated while syncing user data.");
        }
    }
}
