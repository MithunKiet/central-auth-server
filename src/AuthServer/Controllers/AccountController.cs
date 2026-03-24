using AuthServer.Entities;
using AuthServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

/// <summary>
/// Handles user account operations: login, registration, logout, and password management.
/// </summary>
[Route("account")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Displays the login page. Clears any external authentication session before rendering.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after a successful login.</param>
    [HttpGet("login")]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Processes the login form. Supports lookup by username or email address.
    /// Enforces account lockout after repeated failures and rejects inactive accounts.
    /// </summary>
    /// <param name="model">The login credentials submitted by the user.</param>
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = model.UsernameOrEmail.Contains('@')
            ? await _userManager.FindByEmailAsync(model.UsernameOrEmail)
            : await _userManager.FindByNameAsync(model.UsernameOrEmail);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("User {UserName} logged in", user.UserName);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {UserName} account locked out", user.UserName);
            ModelState.AddModelError(string.Empty, "Account is locked. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid credentials.");
        return View(model);
    }

    /// <summary>
    /// Displays the registration page.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful registration.</param>
    [HttpGet("register")]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Processes the registration form. Creates the user, assigns the <c>User</c> role,
    /// and signs them in immediately. Email confirmation is bypassed in the current implementation.
    /// </summary>
    /// <param name="model">The registration details submitted by the user.</param>
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            // Email verification is not implemented; set to true to allow immediate login.
            // Add a proper email confirmation flow before deploying to production.
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("New user registered: {UserName}", user.UserName);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    /// <summary>
    /// Signs the current user out and redirects to the home page.
    /// Preferred over the GET variant for direct UI use because it is CSRF-protected.
    /// </summary>
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");
        return RedirectToAction("Index", "Home");
    }

    // GET logout is provided for OIDC post-logout redirect flows where no form submission is possible.
    // It signs out the session and redirects home. The POST endpoint (with CSRF) is preferred for direct UI use.
    /// <summary>
    /// Signs the current user out via a GET request. Intended for OIDC post-logout redirect
    /// flows where the authorization server cannot perform a form POST. Use the
    /// CSRF-protected <see cref="Logout"/> POST endpoint for direct UI interactions.
    /// </summary>
    [HttpGet("logout")]
    public async Task<IActionResult> LogoutCallback()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Displays the change-password page. Requires the user to be authenticated.
    /// </summary>
    [HttpGet("change-password")]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    /// <summary>
    /// Processes the change-password form. Refreshes the authentication cookie on success
    /// so the user is not signed out. Requires the user to be authenticated.
    /// </summary>
    /// <param name="model">The current and new passwords submitted by the user.</param>
    [HttpPost("change-password")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User {UserName} changed password", user.UserName);
            TempData["StatusMessage"] = "Your password has been changed successfully.";
            return RedirectToAction("ChangePassword");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    /// <summary>
    /// Displays the access-denied page, shown when a user lacks the required permissions.
    /// </summary>
    [HttpGet("access-denied")]
    public IActionResult AccessDenied() => View();
}
