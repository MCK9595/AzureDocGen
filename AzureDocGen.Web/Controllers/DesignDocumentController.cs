using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AzureDocGen.Data.Entities;
using AzureDocGen.Web.Models;
using AzureDocGen.Web.Services;

namespace AzureDocGen.Web.Controllers;

[Authorize]
public class DesignDocumentController : Controller
{
    private readonly IDesignDocumentService _documentService;
    private readonly IProjectService _projectService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<DesignDocumentController> _logger;

    public DesignDocumentController(
        IDesignDocumentService documentService,
        IProjectService projectService,
        IPermissionService permissionService,
        ILogger<DesignDocumentController> logger)
    {
        _documentService = documentService;
        _projectService = projectService;
        _permissionService = permissionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid projectId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // プロジェクトへのアクセス権限を確認
        if (!await _permissionService.CanAccessProjectAsync(userId, projectId))
        {
            return Forbid();
        }

        var project = await _projectService.GetProjectByIdAsync(projectId);
        if (project == null)
        {
            return NotFound();
        }

        var documents = await _documentService.GetProjectDocumentsAsync(projectId);

        var viewModels = documents.Select(d => new DesignDocumentListViewModel
        {
            Id = d.Id,
            Title = d.Title,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            CreatedBy = d.Creator?.Email ?? d.CreatedBy,
            ApprovedAt = d.ApprovedAt,
            ApprovedBy = d.Approver?.Email ?? d.ApprovedBy,
            VersionCount = d.Versions.Count
        }).ToList();

        ViewBag.ProjectId = projectId;
        ViewBag.ProjectName = project.Name;

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        // プロジェクトへのアクセス権限を確認
        if (!await _permissionService.CanAccessProjectAsync(userId, document.ProjectId))
        {
            return Forbid();
        }

        var userRole = await _permissionService.GetProjectRoleAsync(userId, document.ProjectId);
        var canEdit = document.Status == WorkflowStatus.Draft &&
                     (document.CreatedBy == userId ||
                      (userRole.HasValue && (userRole.Value == Data.Enums.ProjectRoleType.ProjectOwner ||
                                            userRole.Value == Data.Enums.ProjectRoleType.ProjectManager)));

        var canApprove = document.Status == WorkflowStatus.InReview &&
                        userRole.HasValue &&
                        (userRole.Value == Data.Enums.ProjectRoleType.ProjectOwner ||
                         userRole.Value == Data.Enums.ProjectRoleType.ProjectManager ||
                         userRole.Value == Data.Enums.ProjectRoleType.ProjectReviewer);

        var canDelete = document.Status == WorkflowStatus.Draft && document.CreatedBy == userId;

        var viewModel = new DesignDocumentDetailsViewModel
        {
            Id = document.Id,
            ProjectId = document.ProjectId,
            ProjectName = document.Project?.Name ?? string.Empty,
            Title = document.Title,
            Status = document.Status,
            CreatedAt = document.CreatedAt,
            CreatedBy = document.Creator?.Email ?? document.CreatedBy,
            ApprovedAt = document.ApprovedAt,
            ApprovedBy = document.Approver?.Email ?? document.ApprovedBy,
            Versions = document.Versions.Select(v => new DocumentVersionViewModel
            {
                Id = v.Id,
                Version = v.Version,
                CreatedAt = v.CreatedAt,
                CreatedBy = v.Creator?.Email ?? v.CreatedBy,
                Comment = v.Comment
            }).ToList(),
            CanEdit = canEdit,
            CanApprove = canApprove,
            CanDelete = canDelete
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

        // プロジェクトへのアクセス権限を確認
        if (!await _permissionService.CanAccessProjectAsync(userId, projectId))
        {
            return Forbid();
        }

        var project = await _projectService.GetProjectByIdAsync(projectId);
        if (project == null)
        {
            return NotFound();
        }

        var viewModel = new DesignDocumentCreateViewModel
        {
            ProjectId = projectId
        };

        ViewBag.ProjectName = project.Name;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DesignDocumentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var project = await _projectService.GetProjectByIdAsync(model.ProjectId);
            ViewBag.ProjectName = project?.Name ?? string.Empty;
            return View(model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // プロジェクトへのアクセス権限を確認
        if (!await _permissionService.CanAccessProjectAsync(userId, model.ProjectId))
        {
            return Forbid();
        }

        try
        {
            var document = await _documentService.CreateDocumentAsync(model.ProjectId, model.Title, userId);

            TempData["SuccessMessage"] = "設計書を作成しました。";
            return RedirectToAction(nameof(Details), new { id = document.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating design document");
            ModelState.AddModelError(string.Empty, "設計書の作成中にエラーが発生しました。");

            var project = await _projectService.GetProjectByIdAsync(model.ProjectId);
            ViewBag.ProjectName = project?.Name ?? string.Empty;
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

        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        // 編集権限を確認
        var userRole = await _permissionService.GetProjectRoleAsync(userId, document.ProjectId);
        var canEdit = document.Status == WorkflowStatus.Draft &&
                     (document.CreatedBy == userId ||
                      (userRole.HasValue && (userRole.Value == Data.Enums.ProjectRoleType.ProjectOwner ||
                                            userRole.Value == Data.Enums.ProjectRoleType.ProjectManager)));

        if (!canEdit)
        {
            return Forbid();
        }

        var viewModel = new DesignDocumentEditViewModel
        {
            Id = document.Id,
            Title = document.Title
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DesignDocumentEditViewModel model)
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

        var document = await _documentService.GetDocumentByIdAsync(model.Id);
        if (document == null)
        {
            return NotFound();
        }

        // 編集権限を確認
        var userRole = await _permissionService.GetProjectRoleAsync(userId, document.ProjectId);
        var canEdit = document.Status == WorkflowStatus.Draft &&
                     (document.CreatedBy == userId ||
                      (userRole.HasValue && (userRole.Value == Data.Enums.ProjectRoleType.ProjectOwner ||
                                            userRole.Value == Data.Enums.ProjectRoleType.ProjectManager)));

        if (!canEdit)
        {
            return Forbid();
        }

        try
        {
            await _documentService.UpdateDocumentAsync(model.Id, model.Title);

            TempData["SuccessMessage"] = "設計書を更新しました。";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating design document {DocumentId}", model.Id);
            ModelState.AddModelError(string.Empty, "設計書の更新中にエラーが発生しました。");
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

        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        // 削除権限を確認（ドラフトかつ作成者のみ）
        if (document.Status != WorkflowStatus.Draft || document.CreatedBy != userId)
        {
            return Forbid();
        }

        try
        {
            var result = await _documentService.DeleteDocumentAsync(id, userId);
            if (!result)
            {
                TempData["ErrorMessage"] = "設計書の削除に失敗しました。";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = "設計書を削除しました。";
            return RedirectToAction(nameof(Index), new { projectId = document.ProjectId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete design document {DocumentId}", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting design document {DocumentId}", id);
            TempData["ErrorMessage"] = "設計書の削除中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForReview(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            await _documentService.UpdateStatusAsync(id, WorkflowStatus.InReview, userId);
            TempData["SuccessMessage"] = "設計書をレビューに提出しました。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting document {DocumentId} for review", id);
            TempData["ErrorMessage"] = "レビュー提出中にエラーが発生しました。";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            await _documentService.ApproveDocumentAsync(id, userId);
            TempData["SuccessMessage"] = "設計書を承認しました。";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to approve document {DocumentId}", id);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving document {DocumentId}", id);
            TempData["ErrorMessage"] = "承認処理中にエラーが発生しました。";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string reason)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "却下理由を入力してください。";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _documentService.RejectDocumentAsync(id, userId, reason);
            TempData["SuccessMessage"] = "設計書を却下しました。";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to reject document {DocumentId}", id);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting document {DocumentId}", id);
            TempData["ErrorMessage"] = "却下処理中にエラーが発生しました。";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
