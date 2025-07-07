using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AzureDocGen.Data.Entities;
using AzureDocGen.Web.Models;

namespace AzureDocGen.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            _logger.LogDebug("Attempting login for email: {Email}", model.Email);
            
            // Check if user exists
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt failed - user not found: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "ログインに失敗しました。");
                return View(model);
            }
            
            _logger.LogDebug("User found: {Email} (ID: {UserId}, Active: {IsActive})", user.Email, user.Id, user.IsActive);
            
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, 
                model.Password, 
                model.RememberMe, 
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully. UserID: {UserId}", model.Email, user.Id);
                
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                return LocalRedirect(returnUrl ?? "/");
            }
            
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out.", model.Email);
                return RedirectToAction("Lockout");
            }
            
            if (result.IsNotAllowed)
            {
                _logger.LogWarning("Login not allowed for user {Email}. Email confirmed: {EmailConfirmed}", model.Email, user.EmailConfirmed);
            }
            
            if (result.RequiresTwoFactor)
            {
                _logger.LogInformation("User {Email} requires two-factor authentication.", model.Email);
            }
            
            _logger.LogWarning("Login failed for user {Email}. Result: {Result}", model.Email, result);
            ModelState.AddModelError(string.Empty, "ログインに失敗しました。");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Department = model.Department,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} created.", model.Email);
                
                // 新しい権限システムでは、ユーザー作成時にはロールを割り当てません
                // プロジェクトオーナーやシステム管理者が後でプロジェクト権限を割り当てます
                
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Lockout()
    {
        return View();
    }
}