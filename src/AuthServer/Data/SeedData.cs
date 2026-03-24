using AuthServer.Entities;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

        try
        {
            await SeedRolesAsync(roleManager, logger);
            await SeedUsersAsync(userManager, logger);
            await SeedScopesAsync(scopeManager, logger);
            await SeedApplicationsAsync(applicationManager, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database seeding");
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        string[] roles = ["Admin", "User", "Manager"];
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole(roleName)
                {
                    Description = $"{roleName} role"
                };
                await roleManager.CreateAsync(role);
                logger.LogInformation("Created role: {RoleName}", roleName);
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminEmail = "admin@authserver.local";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123456!");
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(admin, ["Admin", "User"]);
                logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Failed to create admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        const string userEmail = "user@authserver.local";
        if (await userManager.FindByEmailAsync(userEmail) == null)
        {
            var user = new ApplicationUser
            {
                UserName = "testuser",
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "User@123456!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "User");
                logger.LogInformation("Created test user: {Email}", userEmail);
            }
        }
    }

    private static async Task SeedScopesAsync(IOpenIddictScopeManager scopeManager, ILogger logger)
    {
        if (await scopeManager.FindByNameAsync("api") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api",
                DisplayName = "API Access",
                Resources = { "resource_server" }
            });
            logger.LogInformation("Created scope: api");
        }

        if (await scopeManager.FindByNameAsync("roles") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "roles",
                DisplayName = "Roles"
            });
            logger.LogInformation("Created scope: roles");
        }
    }

    private static async Task SeedApplicationsAsync(IOpenIddictApplicationManager applicationManager, ILogger logger)
    {
        if (await applicationManager.FindByClientIdAsync("web-client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "web-client",
                ClientSecret = "web-client-secret",
                DisplayName = "Web Client Application",
                RedirectUris = { new Uri("https://localhost:5001/signin-oidc"), new Uri("https://oauth.pstmn.io/v1/callback") },
                PostLogoutRedirectUris = { new Uri("https://localhost:5001/signout-callback-oidc") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    $"{Permissions.Prefixes.Scope}api",
                    $"{Permissions.Prefixes.Scope}roles"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            });
            logger.LogInformation("Created OpenIddict application: web-client");
        }

        if (await applicationManager.FindByClientIdAsync("m2m-client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "m2m-client",
                ClientSecret = "m2m-client-secret",
                DisplayName = "Machine-to-Machine Client",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.ClientCredentials,
                    $"{Permissions.Prefixes.Scope}api"
                }
            });
            logger.LogInformation("Created OpenIddict application: m2m-client");
        }

        if (await applicationManager.FindByClientIdAsync("spa-client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "spa-client",
                DisplayName = "Single Page Application",
                ClientType = ClientTypes.Public,
                RedirectUris = { new Uri("https://localhost:3000/callback"), new Uri("https://oauth.pstmn.io/v1/callback") },
                PostLogoutRedirectUris = { new Uri("https://localhost:3000/") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    $"{Permissions.Prefixes.Scope}api",
                    $"{Permissions.Prefixes.Scope}roles"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            });
            logger.LogInformation("Created OpenIddict application: spa-client");
        }
    }
}
