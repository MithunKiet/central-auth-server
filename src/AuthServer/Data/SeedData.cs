using AuthServer.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
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
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

        try
        {
            await SeedRolesAsync(roleManager, logger);
            await SeedUsersAsync(userManager, configuration, logger);
            await SeedScopesAsync(scopeManager, logger);
            await SeedApplicationsAsync(applicationManager, configuration, logger);
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

    private static async Task SeedUsersAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // Passwords are read from configuration (SeedData:AdminPassword / SeedData:UserPassword).
        // Set these via environment variables or user secrets — never commit real passwords to source control.
        var adminPassword = configuration["SeedData:AdminPassword"];
        if (string.IsNullOrEmpty(adminPassword))
        {
            logger.LogWarning("SeedData:AdminPassword is not configured — skipping admin user seed.");
            adminPassword = null;
        }

        const string adminEmail = "admin@sso.local";
        if (adminPassword != null && await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                Department = "IT",
                // Email confirmation is auto-set for the seed admin; use proper email verification in production.
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
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

        var userPassword = configuration["SeedData:UserPassword"];
        if (string.IsNullOrEmpty(userPassword))
        {
            logger.LogWarning("SeedData:UserPassword is not configured — skipping standard user seed.");
            userPassword = null;
        }

        const string userEmail = "user@authserver.local";
        if (userPassword != null && await userManager.FindByEmailAsync(userEmail) == null)
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
            var result = await userManager.CreateAsync(user, userPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "User");
                logger.LogInformation("Created test user: {Email}", userEmail);
            }
        }

        var managerPassword = configuration["SeedData:ManagerPassword"];
        if (string.IsNullOrEmpty(managerPassword))
        {
            logger.LogWarning("SeedData:ManagerPassword is not configured — skipping manager user seed.");
            managerPassword = null;
        }

        const string managerEmail = "manager@authserver.local";
        if (managerPassword != null && await userManager.FindByEmailAsync(managerEmail) == null)
        {
            var manager = new ApplicationUser
            {
                UserName = "manager",
                Email = managerEmail,
                FirstName = "Operations",
                LastName = "Manager",
                Department = "Operations",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(manager, managerPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(manager, ["Manager", "User"]);
                logger.LogInformation("Created manager user: {Email}", managerEmail);
            }
            else
            {
                logger.LogError("Failed to create manager: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        const string devEmail = "developer@authserver.local";
        if (userPassword != null && await userManager.FindByEmailAsync(devEmail) == null)
        {
            var developer = new ApplicationUser
            {
                UserName = "developer",
                Email = devEmail,
                FirstName = "Dev",
                LastName = "User",
                Department = "Engineering",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(developer, userPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(developer, "User");
                logger.LogInformation("Created developer user: {Email}", devEmail);
            }
        }

        const string salesEmail = "sales@authserver.local";
        if (userPassword != null && await userManager.FindByEmailAsync(salesEmail) == null)
        {
            var sales = new ApplicationUser
            {
                UserName = "salesuser",
                Email = salesEmail,
                FirstName = "Sales",
                LastName = "User",
                Department = "Sales",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(sales, userPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(sales, "User");
                logger.LogInformation("Created sales user: {Email}", salesEmail);
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
                Resources = { "api" }
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

    private static async Task SeedApplicationsAsync(
        IOpenIddictApplicationManager applicationManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // Client secrets are read from configuration (SeedData:WebClientSecret / SeedData:M2MClientSecret).
        // Set these via environment variables or user secrets — never commit real secrets to source control.
        var webClientSecret = configuration["SeedData:WebClientSecret"]
            ?? throw new InvalidOperationException(
                "SeedData:WebClientSecret is not configured. Set it via environment variable or user secrets.");

        var m2mClientSecret = configuration["SeedData:M2MClientSecret"]
            ?? throw new InvalidOperationException(
                "SeedData:M2MClientSecret is not configured. Set it via environment variable or user secrets.");

        if (await applicationManager.FindByClientIdAsync("web-client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "web-client",
                ClientSecret = webClientSecret,
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
                ClientSecret = m2mClientSecret,
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

        if (await applicationManager.FindByClientIdAsync("nextjs-client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "nextjs-client",
                DisplayName = "Next.js Client",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                RedirectUris =
                {
                    // Auth.js v5 callback path for provider id "custom-sso"
                    new Uri("http://localhost:3000/api/auth/callback/custom-sso"),
                    new Uri("https://oauth.pstmn.io/v1/callback")
                },
                PostLogoutRedirectUris = { new Uri("http://localhost:3000/") },
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
            logger.LogInformation("Created OpenIddict application: nextjs-client");
        }
    }
}
