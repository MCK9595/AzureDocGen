using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AzureDocGen.Web.Services;

namespace AzureDocGen.Web.Controllers;

/// <summary>
/// ドキュメント出力コントローラー
/// </summary>
[Authorize]
public class ExportController : Controller
{
    private readonly IDocumentExportService _exportService;
    private readonly IEnvironmentService _environmentService;
    private readonly IProjectService _projectService;
    private readonly ITemplateService _templateService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IDocumentExportService exportService,
        IEnvironmentService environmentService,
        IProjectService projectService,
        ITemplateService templateService,
        ILogger<ExportController> logger)
    {
        _exportService = exportService;
        _environmentService = environmentService;
        _projectService = projectService;
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// 環境をExcel形式で出力
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportEnvironmentExcel(Guid id)
    {
        try
        {
            var environment = await _environmentService.GetEnvironmentByIdAsync(id);
            if (environment == null)
            {
                return NotFound();
            }

            var fileBytes = await _exportService.ExportToExcelAsync(id);
            var fileName = $"{environment.Name}_設計書_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export environment {EnvironmentId} to Excel", id);
            TempData["ErrorMessage"] = $"Excel出力に失敗しました: {ex.Message}";
            return RedirectToAction("Details", "Environment", new { id });
        }
    }

    /// <summary>
    /// 環境をMarkdown形式で出力
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportEnvironmentMarkdown(Guid id)
    {
        try
        {
            var environment = await _environmentService.GetEnvironmentByIdAsync(id);
            if (environment == null)
            {
                return NotFound();
            }

            var markdown = await _exportService.ExportToMarkdownAsync(id);
            var fileName = $"{environment.Name}_設計書_{DateTime.Now:yyyyMMdd}.md";
            var fileBytes = System.Text.Encoding.UTF8.GetBytes(markdown);

            return File(fileBytes, "text/markdown", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export environment {EnvironmentId} to Markdown", id);
            TempData["ErrorMessage"] = $"Markdown出力に失敗しました: {ex.Message}";
            return RedirectToAction("Details", "Environment", new { id });
        }
    }

    /// <summary>
    /// 環境をPDF形式で出力
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportEnvironmentPdf(Guid id)
    {
        try
        {
            var environment = await _environmentService.GetEnvironmentByIdAsync(id);
            if (environment == null)
            {
                return NotFound();
            }

            var fileBytes = await _exportService.ExportToPdfAsync(id);
            var fileName = $"{environment.Name}_設計書_{DateTime.Now:yyyyMMdd}.pdf";

            return File(fileBytes, "application/pdf", fileName);
        }
        catch (NotImplementedException)
        {
            TempData["WarningMessage"] = "PDF出力機能は現在実装中です。";
            return RedirectToAction("Details", "Environment", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export environment {EnvironmentId} to PDF", id);
            TempData["ErrorMessage"] = $"PDF出力に失敗しました: {ex.Message}";
            return RedirectToAction("Details", "Environment", new { id });
        }
    }

    /// <summary>
    /// プロジェクト全体をExcel形式で出力
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportProjectExcel(Guid id)
    {
        try
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            var fileBytes = await _exportService.ExportProjectToExcelAsync(id);
            var fileName = $"{project.Name}_全環境設計書_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export project {ProjectId} to Excel", id);
            TempData["ErrorMessage"] = $"Excel出力に失敗しました: {ex.Message}";
            return RedirectToAction("Details", "Project", new { id });
        }
    }

    /// <summary>
    /// テンプレートをExcel形式で出力
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportTemplateExcel(Guid id)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            var fileBytes = await _exportService.ExportTemplateToExcelAsync(id);
            var fileName = $"テンプレート_{template.Name}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export template {TemplateId} to Excel", id);
            TempData["ErrorMessage"] = $"Excel出力に失敗しました: {ex.Message}";
            return RedirectToAction("Details", "Template", new { id });
        }
    }

    /// <summary>
    /// 出力プレビュー画面
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Preview(Guid environmentId)
    {
        try
        {
            var markdown = await _exportService.ExportToMarkdownAsync(environmentId);
            ViewBag.Markdown = markdown;
            ViewBag.EnvironmentId = environmentId;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate preview for environment {EnvironmentId}", environmentId);
            TempData["ErrorMessage"] = $"プレビュー生成に失敗しました: {ex.Message}";
            return RedirectToAction("Details", "Environment", new { id = environmentId });
        }
    }
}
