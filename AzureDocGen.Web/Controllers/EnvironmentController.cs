using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AzureDocGen.Data.Entities;
using AzureDocGen.Web.Models;
using AzureDocGen.Web.Services;

namespace AzureDocGen.Web.Controllers;

[Authorize]
public class EnvironmentController : Controller
{
    private readonly IEnvironmentService _environmentService;
    private readonly IProjectService _projectService;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<EnvironmentController> _logger;

    public EnvironmentController(
        IEnvironmentService environmentService,
        IProjectService projectService,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        ILogger<EnvironmentController> logger)
    {
        _environmentService = environmentService;
        _projectService = projectService;
        _permissionService = permissionService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var environment = await _environmentService.GetEnvironmentByIdAsync(id);
        if (environment == null)
        {
            return NotFound();
        }

        // アクセス権限確認
        if (!await _environmentService.CanUserAccessEnvironmentAsync(userId, id))
        {
            return Forbid();
        }

        var canEdit = await _environmentService.CanUserEditEnvironmentAsync(userId, id);
        var hasDesignDocuments = await _environmentService.HasDesignDocumentsAsync(id);

        var viewModel = new EnvironmentDetailsViewModel
        {
            Id = environment.Id,
            ProjectId = environment.ProjectId,
            ProjectName = environment.Project?.Name ?? string.Empty,
            Name = environment.Name,
            Description = environment.Description,
            DisplayOrder = environment.DisplayOrder,
            CreatedAt = environment.CreatedAt,
            CreatedBy = environment.Creator?.Email ?? environment.CreatedBy,
            HasDesignDocuments = hasDesignDocuments,
            CanEdit = canEdit,
            CanDelete = canEdit && !hasDesignDocuments,
            DesignDocuments = new() // TODO: 設計書一覧の実装後に追加
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid projectId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var project = await _projectService.GetProjectByIdAsync(projectId);
        if (project == null)
        {
            return NotFound();
        }

        // プロジェクトの編集権限確認
        if (!await _permissionService.HasProjectRoleAsync(userId, projectId, Data.Enums.ProjectRoleType.ProjectManager))
        {
            return Forbid();
        }

        var viewModel = new EnvironmentCreateViewModel
        {
            ProjectId = projectId,
            ProjectName = project.Name
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EnvironmentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // プロジェクト名を再設定
            var project = await _projectService.GetProjectByIdAsync(model.ProjectId);
            if (project != null)
            {
                model.ProjectName = project.Name;
            }
            return View(model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // プロジェクトの編集権限確認
        if (!await _permissionService.HasProjectRoleAsync(userId, model.ProjectId, Data.Enums.ProjectRoleType.ProjectManager))
        {
            return Forbid();
        }

        try
        {
            var environment = await _environmentService.CreateEnvironmentAsync(
                model.ProjectId,
                model.Name,
                model.Description ?? string.Empty,
                userId);

            TempData["SuccessMessage"] = "環境を作成しました。";
            return RedirectToAction("Details", "Project", new { id = model.ProjectId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create environment");
            ModelState.AddModelError(string.Empty, ex.Message);
            
            // プロジェクト名を再設定
            var project = await _projectService.GetProjectByIdAsync(model.ProjectId);
            if (project != null)
            {
                model.ProjectName = project.Name;
            }
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating environment");
            ModelState.AddModelError(string.Empty, "環境の作成中にエラーが発生しました。");
            
            // プロジェクト名を再設定
            var project = await _projectService.GetProjectByIdAsync(model.ProjectId);
            if (project != null)
            {
                model.ProjectName = project.Name;
            }
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

        var environment = await _environmentService.GetEnvironmentByIdAsync(id);
        if (environment == null)
        {
            return NotFound();
        }

        // 編集権限確認
        if (!await _environmentService.CanUserEditEnvironmentAsync(userId, id))
        {
            return Forbid();
        }

        var hasDesignDocuments = await _environmentService.HasDesignDocumentsAsync(id);

        var viewModel = new EnvironmentEditViewModel
        {
            Id = environment.Id,
            ProjectId = environment.ProjectId,
            ProjectName = environment.Project?.Name ?? string.Empty,
            Name = environment.Name,
            Description = environment.Description,
            HasDesignDocuments = hasDesignDocuments
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EnvironmentEditViewModel model)
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

        // 編集権限確認
        if (!await _environmentService.CanUserEditEnvironmentAsync(userId, model.Id))
        {
            return Forbid();
        }

        try
        {
            await _environmentService.UpdateEnvironmentAsync(
                model.Id,
                model.Name,
                model.Description ?? string.Empty);

            TempData["SuccessMessage"] = "環境を更新しました。";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update environment {EnvironmentId}", model.Id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating environment {EnvironmentId}", model.Id);
            ModelState.AddModelError(string.Empty, "環境の更新中にエラーが発生しました。");
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

        var environment = await _environmentService.GetEnvironmentByIdAsync(id);
        if (environment == null)
        {
            return NotFound();
        }

        // 削除権限確認
        if (!await _environmentService.CanUserEditEnvironmentAsync(userId, id))
        {
            return Forbid();
        }

        try
        {
            var result = await _environmentService.DeleteEnvironmentAsync(id, userId);
            if (!result)
            {
                TempData["ErrorMessage"] = "環境の削除に失敗しました。";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = "環境を削除しました。";
            return RedirectToAction("Details", "Project", new { id = environment.ProjectId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete environment {EnvironmentId}", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting environment {EnvironmentId}", id);
            TempData["ErrorMessage"] = "環境の削除中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrder([FromBody] EnvironmentOrderUpdateViewModel model)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // プロジェクトの編集権限確認
        if (!await _permissionService.HasProjectRoleAsync(userId, model.ProjectId, Data.Enums.ProjectRoleType.ProjectManager))
        {
            return Forbid();
        }

        try
        {
            var result = await _environmentService.UpdateEnvironmentOrderAsync(
                model.ProjectId, 
                model.OrderedEnvironmentIds);

            if (result)
            {
                return Json(new { success = true, message = "環境の順序を更新しました。" });
            }
            else
            {
                return Json(new { success = false, message = "環境の順序更新に失敗しました。" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating environment order for project {ProjectId}", model.ProjectId);
            return Json(new { success = false, message = "環境の順序更新中にエラーが発生しました。" });
        }
    }
}