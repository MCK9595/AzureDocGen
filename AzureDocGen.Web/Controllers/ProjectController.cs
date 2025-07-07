using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Enums;
using AzureDocGen.Web.Models;
using AzureDocGen.Web.Services;

namespace AzureDocGen.Web.Controllers;

[Authorize]
public class ProjectController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IEnvironmentService _environmentService;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(
        IProjectService projectService,
        IEnvironmentService environmentService,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        ILogger<ProjectController> logger)
    {
        _projectService = projectService;
        _environmentService = environmentService;
        _permissionService = permissionService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ProjectSearchViewModel? searchModel, int page = 1, int pageSize = 10)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        searchModel ??= new ProjectSearchViewModel();
        
        var (projects, totalCount) = await _projectService.SearchUserProjectsAsync(userId, searchModel, page, pageSize);
        
        var viewModels = new List<ProjectListViewModel>();
        foreach (var project in projects)
        {
            var stats = await _projectService.GetProjectStatisticsAsync(project.Id);
            var userRole = await _permissionService.GetProjectRoleAsync(userId, project.Id);
            
            viewModels.Add(new ProjectListViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                CreatedBy = project.Creator?.Email ?? project.CreatedBy,
                MemberCount = stats.MemberCount,
                EnvironmentCount = stats.EnvironmentCount,
                LastActivityDate = stats.LastActivityDate,
                UserRole = userRole
            });
        }

        var indexViewModel = new ProjectIndexViewModel
        {
            Projects = viewModels,
            SearchModel = searchModel,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return View(indexViewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // プロジェクトへのアクセス権限を確認
        if (!await _permissionService.CanAccessProjectAsync(userId, id))
        {
            return Forbid();
        }

        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        var members = await _projectService.GetProjectMembersAsync(id);
        var environments = await _environmentService.GetProjectEnvironmentsAsync(id);
        var userRole = await _permissionService.GetProjectRoleAsync(userId, id);

        var viewModel = new ProjectDetailsViewModel
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            CreatedBy = project.Creator?.Email ?? project.CreatedBy,
            Members = members.Select(m => new ProjectMemberViewModel
            {
                UserId = m.UserId,
                Email = m.User?.Email ?? "Unknown",
                Name = m.User != null ? $"{m.User.LastName} {m.User.FirstName}" : "Unknown",
                RoleType = m.RoleType,
                GrantedAt = m.GrantedAt
            }).ToList(),
            Environments = environments.Select(e => new EnvironmentViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                DisplayOrder = e.DisplayOrder,
                DesignDocumentCount = 0, // TODO: 設計書数を取得する実装
                CreatedAt = e.CreatedAt,
                CreatedBy = e.Creator?.Email ?? e.CreatedBy,
                CanEdit = userRole.HasValue && 
                         (userRole.Value == ProjectRoleType.ProjectOwner || 
                          userRole.Value == ProjectRoleType.ProjectManager),
                CanDelete = userRole.HasValue && 
                           (userRole.Value == ProjectRoleType.ProjectOwner || 
                            userRole.Value == ProjectRoleType.ProjectManager)
            }).ToList(),
            CurrentUserRole = userRole,
            CanEdit = userRole.HasValue && 
                     (userRole.Value == ProjectRoleType.ProjectOwner || 
                      userRole.Value == ProjectRoleType.ProjectManager)
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        // システム管理者またはプロジェクト作成権限を持つユーザーのみ
        return View(new ProjectCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var project = await _projectService.CreateProjectAsync(
                model.Name,
                model.Description ?? string.Empty,
                userId);

            TempData["SuccessMessage"] = "プロジェクトを作成しました。";
            return RedirectToAction(nameof(Details), new { id = project.Id });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("User") && ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "User {UserId} not found when creating project", userId);
            ModelState.AddModelError(string.Empty, "ユーザー情報が見つかりません。再度ログインしてください。");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            ModelState.AddModelError(string.Empty, "プロジェクトの作成中にエラーが発生しました。");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 編集権限を確認
        if (!await _permissionService.HasProjectRoleOrHigherAsync(userId, id, ProjectRoleType.ProjectManager))
        {
            return Forbid();
        }

        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        var viewModel = new ProjectEditViewModel
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 編集権限を確認
        if (!await _permissionService.HasProjectRoleOrHigherAsync(userId, model.Id, ProjectRoleType.ProjectManager))
        {
            return Forbid();
        }

        try
        {
            await _projectService.UpdateProjectAsync(
                model.Id,
                model.Name,
                model.Description ?? string.Empty);

            TempData["SuccessMessage"] = "プロジェクトを更新しました。";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", model.Id);
            ModelState.AddModelError(string.Empty, "プロジェクトの更新中にエラーが発生しました。");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // 削除権限を確認（プロジェクトオーナーのみ）
        if (!await _permissionService.HasProjectRoleAsync(userId, id, ProjectRoleType.ProjectOwner))
        {
            return Forbid();
        }

        try
        {
            var result = await _projectService.DeleteProjectAsync(id, userId);
            if (!result)
            {
                TempData["ErrorMessage"] = "プロジェクトの削除に失敗しました。";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = "プロジェクトを削除しました。";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", id);
            TempData["ErrorMessage"] = "プロジェクトの削除中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(Guid projectId, string userEmail, ProjectRoleType roleType)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        // メンバー追加権限を確認
        if (!await _permissionService.HasProjectRoleOrHigherAsync(currentUserId, projectId, ProjectRoleType.ProjectManager))
        {
            return Forbid();
        }

        try
        {
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                TempData["ErrorMessage"] = "指定されたユーザーが見つかりませんでした。";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }

            await _projectService.AddProjectMemberAsync(projectId, user.Id, roleType, currentUserId);
            
            TempData["SuccessMessage"] = $"{user.Email} をプロジェクトに追加しました。";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to project {ProjectId}", projectId);
            TempData["ErrorMessage"] = "メンバーの追加中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(Guid projectId, string userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        // メンバー削除権限を確認
        if (!await _permissionService.HasProjectRoleOrHigherAsync(currentUserId, projectId, ProjectRoleType.ProjectManager))
        {
            return Forbid();
        }

        try
        {
            // 自分自身は削除できない
            if (userId == currentUserId)
            {
                TempData["ErrorMessage"] = "自分自身をプロジェクトから削除することはできません。";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }

            await _projectService.RemoveProjectMemberAsync(projectId, userId, currentUserId);
            
            TempData["SuccessMessage"] = "メンバーをプロジェクトから削除しました。";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member from project {ProjectId}", projectId);
            TempData["ErrorMessage"] = "メンバーの削除中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEnvironment(Guid projectId, string environmentName)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        // 環境追加権限を確認
        if (!await _permissionService.HasProjectRoleOrHigherAsync(currentUserId, projectId, ProjectRoleType.ProjectManager))
        {
            return Forbid();
        }

        try
        {
            await _projectService.AddEnvironmentAsync(projectId, environmentName, currentUserId);
            
            TempData["SuccessMessage"] = $"環境「{environmentName}」を追加しました。";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding environment to project {ProjectId}", projectId);
            TempData["ErrorMessage"] = "環境の追加中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }
    }
}