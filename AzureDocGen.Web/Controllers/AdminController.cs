using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Constants;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Enums;
using AzureDocGen.Web.Models;

namespace AzureDocGen.Web.Controllers;

[Authorize(Policy = "RequireSystemAdministrator")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users
            .Include(u => u.AuditLogs)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        var userViewModels = new List<UserManagementViewModel>();

        foreach (var user in users)
        {
            // Get user roles from new permission system
            var systemRoles = await _context.SystemRoles
                .Where(sr => sr.UserId == user.Id && sr.IsActive)
                .Select(sr => sr.RoleType.ToString())
                .ToListAsync();

            var projectRoles = await _context.ProjectUserRoles
                .Where(pr => pr.UserId == user.Id && pr.IsActive &&
                            (pr.ExpiresAt == null || pr.ExpiresAt > DateTime.UtcNow))
                .Include(pr => pr.Project)
                .Select(pr => $"{pr.RoleType} ({pr.Project!.Name})")
                .ToListAsync();

            var allRoles = new List<string>();
            allRoles.AddRange(systemRoles);
            allRoles.AddRange(projectRoles);

            userViewModels.Add(new UserManagementViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Department,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = allRoles
            });
        }

        return View(userViewModels);
    }

    [HttpGet]
    public async Task<IActionResult> UserDetails(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Get assigned roles from new permission system
        var assignedRoles = new List<string>();

        // Get System Roles
        var systemRoles = await _context.SystemRoles
            .Where(sr => sr.UserId == id && sr.IsActive)
            .ToListAsync();
        
        foreach (var systemRole in systemRoles)
        {
            assignedRoles.Add($"システム: {GetSystemRoleDisplayName(systemRole.RoleType)}");
        }

        // Get Project Roles
        var projectRoles = await _context.ProjectUserRoles
            .Where(pr => pr.UserId == id && pr.IsActive &&
                        (pr.ExpiresAt == null || pr.ExpiresAt > DateTime.UtcNow))
            .Include(pr => pr.Project)
            .ToListAsync();
        
        foreach (var projectRole in projectRoles)
        {
            assignedRoles.Add($"プロジェクト: {GetProjectRoleDisplayName(projectRole.RoleType)} ({projectRole.Project?.Name ?? "不明"})");
        }

        // Get Environment Roles
        var environmentRoles = await _context.EnvironmentUserRoles
            .Where(er => er.UserId == id && er.IsActive &&
                        (er.ExpiresAt == null || er.ExpiresAt > DateTime.UtcNow))
            .Include(er => er.Environment)
            .ThenInclude(e => e!.Project)
            .ToListAsync();
        
        foreach (var environmentRole in environmentRoles)
        {
            var projectName = environmentRole.Environment?.Project?.Name ?? "不明";
            var environmentName = environmentRole.Environment?.Name ?? "不明";
            assignedRoles.Add($"環境: {GetEnvironmentRoleDisplayName(environmentRole.RoleType)} ({projectName} - {environmentName})");
        }

        // Get available roles that can be assigned
        var availableRoles = new List<string>();
        
        // Add all system role types
        foreach (SystemRoleType roleType in Enum.GetValues<SystemRoleType>())
        {
            availableRoles.Add($"システム: {GetSystemRoleDisplayName(roleType)}");
        }
        
        // Note: Project and Environment roles require specific project/environment context
        // For now, we'll only allow assignment of system roles in this interface
        // Project and environment roles should be assigned through project-specific interfaces

        var viewModel = new UserDetailsViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Department = user.Department,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            AssignedRoles = assignedRoles,
            AvailableRoles = availableRoles
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "ユーザーが見つかりませんでした。";
            return RedirectToAction("Users");
        }

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            TempData["ErrorMessage"] = "現在のユーザーIDが取得できませんでした。";
            return RedirectToAction("UserDetails", new { id = userId });
        }

        try
        {
            // For now, only handle system roles through this interface
            if (roleName.StartsWith("システム: "))
            {
                var roleDisplayName = roleName.Substring("システム: ".Length);
                var systemRoleType = GetSystemRoleTypeFromDisplayName(roleDisplayName);
                
                if (systemRoleType == null)
                {
                    TempData["ErrorMessage"] = "無効なロールタイプです。";
                    return RedirectToAction("UserDetails", new { id = userId });
                }

                // Check if user already has this role
                var existingRole = await _context.SystemRoles
                    .FirstOrDefaultAsync(sr => sr.UserId == userId && 
                                              sr.RoleType == systemRoleType.Value && 
                                              sr.IsActive);

                if (existingRole != null)
                {
                    TempData["InfoMessage"] = "ユーザーは既にこのロールを持っています。";
                    return RedirectToAction("UserDetails", new { id = userId });
                }

                // Create new system role
                var systemRole = new SystemRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleType = systemRoleType.Value,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = currentUserId,
                    IsActive = true
                };

                _context.SystemRoles.Add(systemRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("System role {RoleType} granted to user {UserId} by {GrantedBy}", 
                    systemRoleType.Value, userId, currentUserId);
                TempData["SuccessMessage"] = $"ロール「{roleDisplayName}」を正常に追加しました。";
            }
            else
            {
                TempData["ErrorMessage"] = "プロジェクトロールと環境ロールは、該当するプロジェクト管理画面から設定してください。";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", roleName, userId);
            TempData["ErrorMessage"] = $"ロールの追加中にエラーが発生しました: {ex.Message}";
        }

        return RedirectToAction("UserDetails", new { id = userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "ユーザーが見つかりませんでした。";
            return RedirectToAction("Users");
        }

        try
        {
            // For now, only handle system roles through this interface
            if (roleName.StartsWith("システム: "))
            {
                var roleDisplayName = roleName.Substring("システム: ".Length);
                var systemRoleType = GetSystemRoleTypeFromDisplayName(roleDisplayName);
                
                if (systemRoleType == null)
                {
                    TempData["ErrorMessage"] = "無効なロールタイプです。";
                    return RedirectToAction("UserDetails", new { id = userId });
                }

                var existingRole = await _context.SystemRoles
                    .FirstOrDefaultAsync(sr => sr.UserId == userId && 
                                              sr.RoleType == systemRoleType.Value && 
                                              sr.IsActive);

                if (existingRole == null)
                {
                    TempData["InfoMessage"] = "指定されたロールが見つかりませんでした。";
                    return RedirectToAction("UserDetails", new { id = userId });
                }

                // Deactivate the role instead of deleting it for audit purposes
                existingRole.IsActive = false;
                await _context.SaveChangesAsync();

                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("System role {RoleType} removed from user {UserId} by {RemovedBy}", 
                    systemRoleType.Value, userId, currentUserId);
                TempData["SuccessMessage"] = $"ロール「{roleDisplayName}」を正常に削除しました。";
            }
            else
            {
                TempData["ErrorMessage"] = "プロジェクトロールと環境ロールは、該当するプロジェクト管理画面から削除してください。";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", roleName, userId);
            TempData["ErrorMessage"] = $"ロールの削除中にエラーが発生しました: {ex.Message}";
        }

        return RedirectToAction("UserDetails", new { id = userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var status = user.IsActive ? "有効" : "無効";
            _logger.LogInformation("User {Email} status changed to {Status}", user.Email, status);
            TempData["SuccessMessage"] = $"ユーザーのステータスを{status}に変更しました。";
        }
        else
        {
            TempData["ErrorMessage"] = "ユーザーステータスの変更に失敗しました。";
        }

        return RedirectToAction("Users");
    }


    #region Helper Methods

    private static string GetSystemRoleDisplayName(SystemRoleType roleType)
    {
        return roleType switch
        {
            SystemRoleType.SystemAdministrator => "システム管理者",
            SystemRoleType.GlobalViewer => "グローバル閲覧者",
            _ => roleType.ToString()
        };
    }

    private static SystemRoleType? GetSystemRoleTypeFromDisplayName(string displayName)
    {
        return displayName switch
        {
            "システム管理者" => SystemRoleType.SystemAdministrator,
            "グローバル閲覧者" => SystemRoleType.GlobalViewer,
            _ => null
        };
    }

    private static string GetProjectRoleDisplayName(ProjectRoleType roleType)
    {
        return roleType switch
        {
            ProjectRoleType.ProjectOwner => "プロジェクト所有者",
            ProjectRoleType.ProjectManager => "プロジェクト管理者",
            ProjectRoleType.ProjectReviewer => "プロジェクトレビューアー",
            ProjectRoleType.ProjectDeveloper => "プロジェクト開発者",
            ProjectRoleType.ProjectViewer => "プロジェクト閲覧者",
            _ => roleType.ToString()
        };
    }

    private static string GetEnvironmentRoleDisplayName(EnvironmentRoleType roleType)
    {
        return roleType switch
        {
            EnvironmentRoleType.EnvironmentManager => "環境管理者",
            EnvironmentRoleType.EnvironmentDeveloper => "環境開発者",
            EnvironmentRoleType.EnvironmentViewer => "環境閲覧者",
            _ => roleType.ToString()
        };
    }

    #endregion
}