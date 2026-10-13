using System.ComponentModel.DataAnnotations;

namespace Sayyit.Contracts.User;

public sealed class UserSync
{
    [Required]
    public Guid EntraObjectId { get; init; }

    [Required]
    public Guid TenantId { get; init; }

    [MaxLength(50)]
    public string? UserName { get; init; }

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; init; } = string.Empty;

    [MaxLength(320)]
    public string? Email { get; init; }
}
