using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Enums;
using AzureDocGen.Web.Models;
using AzureDocGen.Web.Services;

namespace AzureDocGen.Web.Controllers;

[Authorize]
public class TemplateController : Controller
{
    private readonly ITemplateService _templateService;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(
        ITemplateService templateService,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        ILogger<TemplateController> logger)
    {
        _templateService = templateService;
        _permissionService = permissionService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? category = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var templates = await _templateService.GetUserTemplatesAsync(userId);
        var statistics = await _templateService.GetTemplateStatisticsAsync(userId);

        // カテゴリーフィルター
        if (!string.IsNullOrEmpty(category))
        {
            templates = category.ToLower() switch
            {
                "private" => templates.Where(t => t.SharingLevel == SharingLevel.Private).ToList(),
                "project" => templates.Where(t => t.SharingLevel == SharingLevel.Project).ToList(),
                "global" => templates.Where(t => t.SharingLevel == SharingLevel.Global).ToList(),
                "recent" => templates.Where(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-30)).ToList(),
                _ => templates
            };
        }

        var viewModels = templates.Select(t => new TemplateListViewModel
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            SharingLevel = t.SharingLevel,
            Version = t.Version,
            CreatedAt = t.CreatedAt,
            CreatedBy = t.Creator?.Email ?? t.CreatedBy,
            ParameterCount = t.Parameters?.Count ?? 0,
            IsOwner = t.CreatedBy == userId
        }).ToList();

        var indexViewModel = new TemplateIndexViewModel
        {
            Templates = viewModels,
            Statistics = statistics,
            CurrentCategory = category
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

        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null)
        {
            return NotFound();
        }

        // アクセス権限を確認（簡易実装）
        var canAccess = template.CreatedBy == userId || 
                       template.SharingLevel == SharingLevel.Global ||
                       await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator);

        if (!canAccess)
        {
            return Forbid();
        }

        var parameters = await _templateService.GetTemplateParametersAsync(id);
        var versions = await _templateService.GetTemplateVersionsAsync(template.Name, template.CreatedBy);

        var viewModel = new TemplateDetailsViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            SharingLevel = template.SharingLevel,
            Version = template.Version,
            CreatedAt = template.CreatedAt,
            CreatedBy = template.Creator?.Email ?? template.CreatedBy,
            Parameters = parameters.Select(p => new TemplateParameterViewModel
            {
                Id = p.Id,
                Name = p.Name,
                ParameterType = p.Type.ToString(),
                IsRequired = p.IsRequired,
                DefaultValue = p.DefaultValue
            }).ToList(),
            Versions = versions.Select(v => new TemplateVersionViewModel
            {
                Id = v.Id,
                Version = v.Version,
                CreatedAt = v.CreatedAt
            }).ToList(),
            Structure = template.Structure,
            IsOwner = template.CreatedBy == userId,
            CanEdit = template.CreatedBy == userId || 
                     await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator)
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new TemplateCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TemplateCreateViewModel model)
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
            var template = await _templateService.CreateTemplateAsync(
                model.Name,
                model.Description ?? string.Empty,
                model.SharingLevel,
                userId);

            TempData["SuccessMessage"] = "テンプレートを作成しました。";
            return RedirectToAction(nameof(Details), new { id = template.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            ModelState.AddModelError(string.Empty, "テンプレートの作成中にエラーが発生しました。");
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

        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null)
        {
            return NotFound();
        }

        // 編集権限を確認
        var canEdit = template.CreatedBy == userId || 
                     await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator);

        if (!canEdit)
        {
            return Forbid();
        }

        var viewModel = new TemplateEditViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            SharingLevel = template.SharingLevel
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TemplateEditViewModel model)
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

        var template = await _templateService.GetTemplateByIdAsync(model.Id);
        if (template == null)
        {
            return NotFound();
        }

        // 編集権限を確認
        var canEdit = template.CreatedBy == userId || 
                     await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator);

        if (!canEdit)
        {
            return Forbid();
        }

        try
        {
            await _templateService.UpdateTemplateAsync(
                model.Id,
                model.Name,
                model.Description ?? string.Empty,
                model.SharingLevel);

            TempData["SuccessMessage"] = "テンプレートを更新しました。";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", model.Id);
            ModelState.AddModelError(string.Empty, "テンプレートの更新中にエラーが発生しました。");
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

        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null)
        {
            return NotFound();
        }

        // 削除権限を確認
        var canDelete = template.CreatedBy == userId || 
                       await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator);

        if (!canDelete)
        {
            return Forbid();
        }

        try
        {
            var result = await _templateService.DeleteTemplateAsync(id, userId);
            if (!result)
            {
                TempData["ErrorMessage"] = "テンプレートの削除に失敗しました。";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = "テンプレートを削除しました。";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", id);
            TempData["ErrorMessage"] = "テンプレートの削除中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(Guid id, string newName)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            TempData["ErrorMessage"] = "新しいテンプレート名を入力してください。";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var duplicatedTemplate = await _templateService.DuplicateTemplateAsync(id, newName, userId);
            
            TempData["SuccessMessage"] = $"テンプレート「{newName}」を作成しました。";
            return RedirectToAction(nameof(Details), new { id = duplicatedTemplate.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating template {TemplateId}", id);
            TempData["ErrorMessage"] = "テンプレートの複製中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVersion(Guid id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null)
        {
            return NotFound();
        }

        // バージョン作成権限を確認（所有者のみ）
        if (template.CreatedBy != userId)
        {
            return Forbid();
        }

        try
        {
            var newVersion = await _templateService.CreateVersionAsync(id, userId);
            
            TempData["SuccessMessage"] = $"バージョン {newVersion.Version} を作成しました。";
            return RedirectToAction(nameof(Details), new { id = newVersion.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template version for {TemplateId}", id);
            TempData["ErrorMessage"] = "新しいバージョンの作成中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddParameter(Guid templateId, string parameterName, string parameterType, bool isRequired, string? defaultValue)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var template = await _templateService.GetTemplateByIdAsync(templateId);
        if (template == null)
        {
            return NotFound();
        }

        // 編集権限を確認
        var canEdit = template.CreatedBy == userId || 
                     await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator);

        if (!canEdit)
        {
            return Forbid();
        }

        try
        {
            await _templateService.AddParameterAsync(templateId, parameterName, parameterType, isRequired, defaultValue);
            
            TempData["SuccessMessage"] = $"パラメーター「{parameterName}」を追加しました。";
            return RedirectToAction(nameof(Details), new { id = templateId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding parameter to template {TemplateId}", templateId);
            TempData["ErrorMessage"] = "パラメーターの追加中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id = templateId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteParameter(Guid parameterId, Guid templateId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var template = await _templateService.GetTemplateByIdAsync(templateId);
        if (template == null)
        {
            return NotFound();
        }

        // 編集権限を確認
        var canEdit = template.CreatedBy == userId || 
                     await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator);

        if (!canEdit)
        {
            return Forbid();
        }

        try
        {
            var result = await _templateService.DeleteParameterAsync(parameterId);
            if (result)
            {
                TempData["SuccessMessage"] = "パラメーターを削除しました。";
            }
            else
            {
                TempData["ErrorMessage"] = "パラメーターの削除に失敗しました。";
            }
            
            return RedirectToAction(nameof(Details), new { id = templateId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting parameter {ParameterId}", parameterId);
            TempData["ErrorMessage"] = "パラメーターの削除中にエラーが発生しました。";
            return RedirectToAction(nameof(Details), new { id = templateId });
        }
    }
}