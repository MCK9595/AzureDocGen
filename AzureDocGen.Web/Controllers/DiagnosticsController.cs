using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Enums;
using AzureDocGen.Web.Services;
using System.Security.Claims;

namespace AzureDocGen.Web.Controllers;

[AllowAnonymous]
public class DiagnosticsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IPermissionService permissionService,
        ILogger<DiagnosticsController> logger)
    {
        _userManager = userManager;
        _context = context;
        _permissionService = permissionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> DatabaseState()
    {
        var result = new
        {
            DatabaseExists = await _context.Database.CanConnectAsync(),
            UserCount = await _context.Users.CountAsync(),
            SystemRoleCount = await _context.SystemRoles.CountAsync(),
            ProjectCount = await _context.Projects.CountAsync(),
            Users = await _context.Users.Select(u => new {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive
            }).ToListAsync(),
            SystemRoles = await _context.SystemRoles.Select(sr => new {
                sr.Id,
                sr.UserId,
                RoleType = sr.RoleType.ToString(),
                sr.IsActive,
                sr.GrantedAt
            }).ToListAsync()
        };

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> UserPermissions()
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            return Json(new { 
                IsAuthenticated = false,
                Message = "User is not authenticated"
            });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User.Identity.Name;

        _logger.LogInformation("Diagnostics: Checking permissions for user {UserId} ({UserName})", userId, userName);

        if (string.IsNullOrEmpty(userId))
        {
            return Json(new {
                IsAuthenticated = true,
                HasUserId = false,
                Message = "User ID not found in claims",
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
            });
        }

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            var hasSystemAdminRole = await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator);
            
            // Get all system roles for this user
            var systemRoles = await _context.SystemRoles
                .Where(sr => sr.UserId == userId && sr.IsActive)
                .Select(sr => new {
                    sr.Id,
                    RoleType = sr.RoleType.ToString(),
                    sr.GrantedAt,
                    sr.GrantedBy
                })
                .ToListAsync();

            return Json(new {
                IsAuthenticated = true,
                HasUserId = true,
                UserId = userId,
                UserName = userName,
                UserExists = user != null,
                UserEmail = user?.Email,
                HasSystemAdminRole = hasSystemAdminRole,
                SystemRoles = systemRoles,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user permissions for userId: {UserId}", userId);
            
            return Json(new {
                IsAuthenticated = true,
                HasUserId = true,
                UserId = userId,
                Error = ex.Message,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> AuthorizationTest()
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            return Json(new { 
                IsAuthenticated = false,
                Message = "User is not authenticated"
            });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Test the authorization policy directly
        var authService = HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        var authResult = await authService.AuthorizeAsync(User, "RequireSystemAdministrator");

        return Json(new {
            IsAuthenticated = true,
            UserId = userId,
            UserName = User.Identity.Name,
            PolicyName = "RequireSystemAdministrator",
            AuthorizationSucceeded = authResult.Succeeded,
            FailureReasons = authResult.Failure?.FailureReasons?.Select(f => f.Message).ToArray() ?? Array.Empty<string>()
        });
    }
}