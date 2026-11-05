namespace Sayyit.API.Features.Users.Persistence;

public sealed class SayyitUser
{
    public Guid UserId { get; set; }

    public Guid EntraObjectId { get; set; }

    public Guid TenantId { get; set; }

    public string? UserName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public DateTime? LastSeenUtc { get; set; }
}
