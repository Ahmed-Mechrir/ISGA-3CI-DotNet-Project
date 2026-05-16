using AsvsSecurityAuditor.Models.Identity;
using AsvsSecurityAuditor.Security;
using AsvsSecurityAuditor.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AsvsSecurityAuditor.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        PopulateDevSeedForLoginPage();

        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        PopulateDevSeedForLoginPage();
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe,
            lockoutOnFailure: true);
        if (result.Succeeded)
            return RedirectToLocal(returnUrl);

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        PopulateDevSeedForLoginPage();
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return View(model);
        }

        if (!await _roleManager.RoleExistsAsync(Roles.Auditor))
            await _roleManager.CreateAsync(new IdentityRole(Roles.Auditor));

        await _userManager.AddToRoleAsync(user, Roles.Auditor);
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Assessment");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    private void PopulateDevSeedForLoginPage()
    {
        if (!_environment.IsDevelopment())
            return;
        ViewData["SeedAdminEmail"] = _configuration["AdminSeed:Email"];
        ViewData["SeedAdminPassword"] = _configuration["AdminSeed:Password"];
    }
}
