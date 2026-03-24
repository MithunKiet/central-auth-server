using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ResourceApi.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public sealed class UserController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var claims = User.Claims.ToList();

        var userId = claims.FirstOrDefault(c => c.Type == "sub")?.Value
                  ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        var email = claims.FirstOrDefault(c => c.Type == "email")?.Value
                 ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        var name = claims.FirstOrDefault(c => c.Type == "name")?.Value
                ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        var roles = claims
            .Where(c => c.Type == "role" || c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var department = claims.FirstOrDefault(c => c.Type == "department")?.Value;

        return Ok(new
        {
            userId,
            subject = userId,
            email,
            name,
            roles,
            department,
            allClaims = claims.Select(c => new { type = c.Type, value = c.Value }).ToList()
        });
    }
}
