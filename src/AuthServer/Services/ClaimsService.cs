using System.Security.Claims;
using AuthServer.Entities;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Services;

public class ClaimsService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ClaimsService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ClaimsIdentity> CreateClaimsIdentityAsync(ApplicationUser user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(
            authenticationType: "Bearer",
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user));
        identity.SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user));

        var requestedScopes = scopes.ToList();

        if (requestedScopes.Contains(Scopes.Email))
        {
            identity.SetClaim(Claims.Email, await _userManager.GetEmailAsync(user));
            identity.SetClaim(Claims.EmailVerified, user.EmailConfirmed ? "true" : "false");
        }

        if (requestedScopes.Contains(Scopes.Profile))
        {
            if (!string.IsNullOrWhiteSpace(user.FirstName))
                identity.SetClaim(Claims.GivenName, user.FirstName);
            if (!string.IsNullOrWhiteSpace(user.LastName))
                identity.SetClaim(Claims.FamilyName, user.LastName);
            if (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
                identity.SetClaim(Claims.Name, user.FullName);
            identity.SetClaim("preferred_username", user.UserName ?? string.Empty);
            identity.SetClaim("created_at", new DateTimeOffset(user.CreatedAt).ToUnixTimeSeconds().ToString());
            if (!string.IsNullOrWhiteSpace(user.Department))
                identity.SetClaim("department", user.Department);
            identity.SetClaim("userId", await _userManager.GetUserIdAsync(user));
        }

        if (requestedScopes.Contains(Scopes.Roles) || requestedScopes.Contains("roles"))
        {
            var roles = await _userManager.GetRolesAsync(user);
            identity.SetClaims(Claims.Role, [.. roles]);
        }

        if (requestedScopes.Contains(Scopes.Phone))
        {
            identity.SetClaim(Claims.PhoneNumber, await _userManager.GetPhoneNumberAsync(user));
            identity.SetClaim(Claims.PhoneNumberVerified, user.PhoneNumberConfirmed ? "true" : "false");
        }

        return identity;
    }

    public static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            Claims.Name or Claims.Subject =>
                [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Email or Claims.EmailVerified or Claims.GivenName or Claims.FamilyName
                or "preferred_username" or "created_at" or "department" or "userId" =>
                claim.Subject?.HasScope(Scopes.Profile) == true || claim.Subject?.HasScope(Scopes.Email) == true
                    ? [Destinations.AccessToken, Destinations.IdentityToken]
                    : [Destinations.AccessToken],
            Claims.Role =>
                claim.Subject?.HasScope(Scopes.Roles) == true || claim.Subject?.HasScope("roles") == true
                    ? [Destinations.AccessToken, Destinations.IdentityToken]
                    : [Destinations.AccessToken],
            _ => [Destinations.AccessToken]
        };
    }
}
