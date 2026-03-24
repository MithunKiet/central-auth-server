using Microsoft.AspNetCore.Identity;

namespace AuthServer.Entities;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
