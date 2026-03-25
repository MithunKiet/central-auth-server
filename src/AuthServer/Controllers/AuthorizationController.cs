using System.Security.Claims;
using AuthServer.Entities;
using AuthServer.Models;
using AuthServer.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Controllers;

public class AuthorizationController : Controller
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ClaimsService _claimsService;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ClaimsService claimsService,
        ILogger<AuthorizationController> logger)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _userManager = userManager;
        _signInManager = signInManager;
        _claimsService = claimsService;
        _logger = logger;
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded || request.HasPromptValue(PromptValues.Login))
        {
            if (request.HasPromptValue(PromptValues.None))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in."
                    }));
            }

            var promptValues = request.GetPromptValues().Remove(PromptValues.Login);
            var prompt = string.Join(" ", promptValues);
            var parameters = Request.HasFormContentType
                ? Request.Form.Where(p => p.Key != Parameters.Prompt).ToList()
                : Request.Query.Where(p => p.Key != Parameters.Prompt).ToList();
            parameters.Add(KeyValuePair.Create(Parameters.Prompt, (Microsoft.Extensions.Primitives.StringValues)prompt));

            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(parameters)
                });
        }

        var user = await _userManager.GetUserAsync(result.Principal!)
            ?? throw new InvalidOperationException("The user details cannot be retrieved.");

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException("The application details cannot be retrieved.");

        var applicationId = await _applicationManager.GetIdAsync(application);
        var userId = await _userManager.GetUserIdAsync(user);

        var authorizations = await _authorizationManager.FindAsync(
            subject: userId,
            client: applicationId!,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes()).ToListAsync();

        switch (await _applicationManager.GetConsentTypeAsync(application))
        {
            case ConsentTypes.External when authorizations.Count == 0:
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The logged in user is not allowed to access this client application."
                    }));

            case ConsentTypes.Implicit:
            case ConsentTypes.External when authorizations.Count > 0:
            case ConsentTypes.Explicit when authorizations.Count > 0 && !request.HasPromptValue(PromptValues.Consent):
                return await IssueAuthorizationResponse(user, application, request, authorizations);

            case ConsentTypes.Explicit when request.HasPromptValue(PromptValues.None):
            case ConsentTypes.Systematic when request.HasPromptValue(PromptValues.None):
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Interactive user consent is required."
                    }));

            default:
                // If this is a POST with consent form response
                if (HttpMethods.IsPost(Request.Method))
                {
                    if (Request.Form.ContainsKey("submit.Accept"))
                    {
                        return await IssueAuthorizationResponse(user, application, request, authorizations);
                    }

                    if (Request.Form.ContainsKey("submit.Deny"))
                    {
                        return Forbid(
                            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                            properties: new AuthenticationProperties(new Dictionary<string, string?>
                            {
                                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                    "The user denied the authorization request."
                            }));
                    }
                }

                return View("Consent", new ConsentViewModel
                {
                    ApplicationName = await _applicationManager.GetLocalizedDisplayNameAsync(application)
                        ?? "Unknown Application",
                    Scopes = request.GetScopes()
                });
        }
    }

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal
                ?? throw new InvalidOperationException("The principal cannot be retrieved.");

            var user = await _userManager.FindByIdAsync(principal.GetClaim(Claims.Subject)!);
            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The token is no longer valid."
                    }));
            }

            if (!user.IsActive || !await _signInManager.CanSignInAsync(user))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The user is no longer allowed to sign in."
                    }));
            }

            var identity = await _claimsService.CreateClaimsIdentityAsync(user, principal.GetScopes());
            identity.SetScopes(principal.GetScopes());
            identity.SetResources(await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync());
            identity.SetDestinations(ClaimsService.GetDestinations);

            _logger.LogInformation("Token issued for user {UserId}", user.Id);
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsClientCredentialsGrantType())
        {
            var application = await _applicationManager.FindByClientIdAsync(request.ClientId!)
                ?? throw new InvalidOperationException("The application details cannot be retrieved.");

            var identity = new ClaimsIdentity(
                authenticationType: "Bearer",
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, await _applicationManager.GetClientIdAsync(application));
            identity.SetClaim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application));
            identity.SetScopes(request.GetScopes());
            identity.SetResources(await _scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync());
            identity.SetDestinations(_ => [Destinations.AccessToken]);

            _logger.LogInformation("Client credentials token issued for {ClientId}", request.ClientId);
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException($"The specified grant type '{request.GrantType}' is not supported.");
    }

    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Userinfo()
    {
        var user = await _userManager.FindByIdAsync(User.GetClaim(Claims.Subject)!);
        if (user == null)
            return Challenge(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = await _userManager.GetUserIdAsync(user),
            [Claims.Name] = await _userManager.GetUserNameAsync(user) ?? string.Empty
        };

        if (User.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = await _userManager.GetEmailAsync(user) ?? string.Empty;
            claims[Claims.EmailVerified] = user.EmailConfirmed;
        }

        if (User.HasScope(Scopes.Profile))
        {
            if (!string.IsNullOrWhiteSpace(user.FirstName))
                claims[Claims.GivenName] = user.FirstName;
            if (!string.IsNullOrWhiteSpace(user.LastName))
                claims[Claims.FamilyName] = user.LastName;
            if (!string.IsNullOrEmpty(user.FullName))
                claims["name"] = user.FullName;
            claims["preferred_username"] = user.UserName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(user.Department))
                claims["department"] = user.Department;
            claims["userId"] = await _userManager.GetUserIdAsync(user);
        }

        if (User.HasScope(Scopes.Roles) || User.HasScope("roles"))
        {
            var roles = await _userManager.GetRolesAsync(user);
            claims[Claims.Role] = roles;
        }

        return Ok(claims);
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        
        await _signInManager.SignOutAsync();
        
        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties 
            { 
                RedirectUri = request?.PostLogoutRedirectUri ?? "/" 
            });
    }

    private async Task<IActionResult> IssueAuthorizationResponse(
        ApplicationUser user,
        object application,
        OpenIddictRequest request,
        IList<object> authorizations)
    {
        var identity = await _claimsService.CreateClaimsIdentityAsync(user, request.GetScopes());
        identity.SetScopes(request.GetScopes());
        identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        var authorization = authorizations.LastOrDefault();
        authorization ??= await _authorizationManager.CreateAsync(
            principal: new ClaimsPrincipal(identity),
            subject: await _userManager.GetUserIdAsync(user),
            client: (await _applicationManager.GetIdAsync(application))!,
            type: AuthorizationTypes.Permanent,
            scopes: identity.GetScopes());

        identity.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));
        identity.SetDestinations(ClaimsService.GetDestinations);

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
