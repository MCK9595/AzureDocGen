using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using System.Text;
using ClosedXML.Excel;

namespace AzureDocGen.Web.Services;

/// <summary>
/// ドキュメント出力サービスの実装
/// </summary>
public class DocumentExportService : IDocumentExportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentExportService> _logger;

    public DocumentExportService(
        ApplicationDbContext context,
        ILogger<DocumentExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EnvironmentExportData> GetEnvironmentDataAsync(Guid environmentId)
    {
        var environment = await _context.Environments
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == environmentId);

        if (environment == null)
        {
            throw new InvalidOperationException($"Environment {environmentId} not found");
        }

        var resources = await _context.Resources
            .Where(r => r.EnvironmentId == environmentId)
            .OrderBy(r => r.ResourceType)
            .ThenBy(r => r.Name)
            .ToListAsync();

        var connections = await _context.ResourceConnections
            .Include(c => c.SourceResource)
            .Include(c => c.TargetResource)
            .Where(c => c.SourceResource!.EnvironmentId == environmentId)
            .ToListAsync();

        return new EnvironmentExportData
        {
            Environment = environment,
            Project = environment.Project!,
            Resources = resources,
            Connections = connections,
            Metadata = new Dictionary<string, object>
            {
                { "ExportedAt", DateTime.UtcNow },
                { "ExportedBy", "System" },
                { "TotalResources", resources.Count },
                { "TotalConnections", connections.Count }
            }
        };
    }

    public async Task<byte[]> ExportToExcelAsync(Guid environmentId)
    {
        _logger.LogInformation("Exporting environment {EnvironmentId} to Excel", environmentId);

        var data = await GetEnvironmentDataAsync(environmentId);

        using var workbook = new XLWorkbook();

        // サマリーシート
        CreateSummarySheet(workbook, data);

        // リソースシート
        CreateResourcesSheet(workbook, data);

        // 接続シート
        CreateConnectionsSheet(workbook, data);

        // パラメーターシート
        CreateParametersSheet(workbook, data);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        _logger.LogInformation("Excel export completed for environment {EnvironmentId}", environmentId);

        return stream.ToArray();
    }

    private void CreateSummarySheet(XLWorkbook workbook, EnvironmentExportData data)
    {
        var worksheet = workbook.Worksheets.Add("サマリー");

        // ヘッダー
        worksheet.Cell("A1").Value = "Azure インフラ設計書";
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;

        // プロジェクト情報
        int row = 3;
        worksheet.Cell(row, 1).Value = "プロジェクト名";
        worksheet.Cell(row, 2).Value = data.Project.Name;
        row++;

        worksheet.Cell(row, 1).Value = "環境名";
        worksheet.Cell(row, 2).Value = data.Environment.Name;
        row++;

        worksheet.Cell(row, 1).Value = "環境タイプ";
        worksheet.Cell(row, 2).Value = data.Environment.EnvironmentType.ToString();
        row++;

        worksheet.Cell(row, 1).Value = "説明";
        worksheet.Cell(row, 2).Value = data.Environment.Description ?? "";
        row++;

        worksheet.Cell(row, 1).Value = "出力日時";
        worksheet.Cell(row, 2).Value = data.Metadata["ExportedAt"];
        row++;

        // 統計情報
        row++;
        worksheet.Cell(row, 1).Value = "リソース統計";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        worksheet.Cell(row, 1).Value = "総リソース数";
        worksheet.Cell(row, 2).Value = data.Metadata["TotalResources"];
        row++;

        worksheet.Cell(row, 1).Value = "接続数";
        worksheet.Cell(row, 2).Value = data.Metadata["TotalConnections"];
        row++;

        // リソースタイプ別カウント
        var resourceTypeCounts = data.Resources
            .GroupBy(r => r.ResourceType)
            .OrderByDescending(g => g.Count())
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToList();

        row++;
        worksheet.Cell(row, 1).Value = "リソースタイプ別内訳";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        foreach (var typeCount in resourceTypeCounts)
        {
            worksheet.Cell(row, 1).Value = typeCount.Type;
            worksheet.Cell(row, 2).Value = typeCount.Count;
            row++;
        }

        // 列幅の自動調整
        worksheet.Columns().AdjustToContents();
    }

    private void CreateResourcesSheet(XLWorkbook workbook, EnvironmentExportData data)
    {
        var worksheet = workbook.Worksheets.Add("リソース一覧");

        // ヘッダー
        worksheet.Cell(1, 1).Value = "リソース名";
        worksheet.Cell(1, 2).Value = "リソースタイプ";
        worksheet.Cell(1, 3).Value = "位置 (X)";
        worksheet.Cell(1, 4).Value = "位置 (Y)";
        worksheet.Cell(1, 5).Value = "プロパティ";
        worksheet.Cell(1, 6).Value = "作成日時";

        // ヘッダースタイル
        var headerRange = worksheet.Range("A1:F1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // データ行
        int row = 2;
        foreach (var resource in data.Resources)
        {
            worksheet.Cell(row, 1).Value = resource.Name;
            worksheet.Cell(row, 2).Value = resource.ResourceType;
            worksheet.Cell(row, 3).Value = resource.VisualPosition?.X ?? 0;
            worksheet.Cell(row, 4).Value = resource.VisualPosition?.Y ?? 0;
            worksheet.Cell(row, 5).Value = resource.PropertiesJson ?? "{}";
            worksheet.Cell(row, 6).Value = resource.CreatedAt;

            row++;
        }

        // 列幅の自動調整
        worksheet.Columns().AdjustToContents();

        // テーブル化
        if (data.Resources.Any())
        {
            var dataRange = worksheet.Range(1, 1, row - 1, 6);
            dataRange.CreateTable();
        }
    }

    private void CreateConnectionsSheet(XLWorkbook workbook, EnvironmentExportData data)
    {
        var worksheet = workbook.Worksheets.Add("リソース接続");

        // ヘッダー
        worksheet.Cell(1, 1).Value = "接続元リソース";
        worksheet.Cell(1, 2).Value = "接続先リソース";
        worksheet.Cell(1, 3).Value = "接続タイプ";
        worksheet.Cell(1, 4).Value = "作成日時";

        // ヘッダースタイル
        var headerRange = worksheet.Range("A1:D1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // データ行
        int row = 2;
        foreach (var connection in data.Connections)
        {
            worksheet.Cell(row, 1).Value = connection.SourceResource?.Name ?? "N/A";
            worksheet.Cell(row, 2).Value = connection.TargetResource?.Name ?? "N/A";
            worksheet.Cell(row, 3).Value = connection.ConnectionType;
            worksheet.Cell(row, 4).Value = connection.CreatedAt;

            row++;
        }

        // 列幅の自動調整
        worksheet.Columns().AdjustToContents();

        // テーブル化
        if (data.Connections.Any())
        {
            var dataRange = worksheet.Range(1, 1, row - 1, 4);
            dataRange.CreateTable();
        }
    }

    private void CreateParametersSheet(XLWorkbook workbook, EnvironmentExportData data)
    {
        var worksheet = workbook.Worksheets.Add("パラメーター詳細");

        // ヘッダー
        worksheet.Cell(1, 1).Value = "リソース名";
        worksheet.Cell(1, 2).Value = "パラメーター名";
        worksheet.Cell(1, 3).Value = "値";
        worksheet.Cell(1, 4).Value = "型";

        // ヘッダースタイル
        var headerRange = worksheet.Range("A1:D1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // データ行
        int row = 2;
        foreach (var resource in data.Resources)
        {
            if (resource.Properties != null && resource.Properties.Any())
            {
                foreach (var property in resource.Properties)
                {
                    worksheet.Cell(row, 1).Value = resource.Name;
                    worksheet.Cell(row, 2).Value = property.Key;
                    worksheet.Cell(row, 3).Value = property.Value?.ToString() ?? "";
                    worksheet.Cell(row, 4).Value = property.Value?.GetType().Name ?? "string";

                    row++;
                }
            }
        }

        // 列幅の自動調整
        worksheet.Columns().AdjustToContents();

        // テーブル化
        if (row > 2)
        {
            var dataRange = worksheet.Range(1, 1, row - 1, 4);
            dataRange.CreateTable();
        }
    }

    public async Task<byte[]> ExportProjectToExcelAsync(Guid projectId)
    {
        _logger.LogInformation("Exporting project {ProjectId} to Excel", projectId);

        var project = await _context.Projects
            .Include(p => p.Environments)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        using var workbook = new XLWorkbook();

        // プロジェクトサマリーシート
        CreateProjectSummarySheet(workbook, project);

        // 各環境のデータを出力
        foreach (var environment in project.Environments.OrderBy(e => e.EnvironmentType))
        {
            var data = await GetEnvironmentDataAsync(environment.Id);

            var envWorksheet = workbook.Worksheets.Add($"{environment.Name}");
            CreateEnvironmentDetailSheet(envWorksheet, data);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        _logger.LogInformation("Excel export completed for project {ProjectId}", projectId);

        return stream.ToArray();
    }

    private void CreateProjectSummarySheet(XLWorkbook workbook, Project project)
    {
        var worksheet = workbook.Worksheets.Add("プロジェクト概要");

        worksheet.Cell("A1").Value = "プロジェクト概要";
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;

        int row = 3;
        worksheet.Cell(row, 1).Value = "プロジェクト名";
        worksheet.Cell(row, 2).Value = project.Name;
        row++;

        worksheet.Cell(row, 1).Value = "説明";
        worksheet.Cell(row, 2).Value = project.Description ?? "";
        row++;

        worksheet.Cell(row, 1).Value = "作成日";
        worksheet.Cell(row, 2).Value = project.CreatedAt;
        row++;

        worksheet.Cell(row, 1).Value = "環境数";
        worksheet.Cell(row, 2).Value = project.Environments.Count;
        row++;

        row++;
        worksheet.Cell(row, 1).Value = "環境一覧";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        worksheet.Cell(row, 1).Value = "環境名";
        worksheet.Cell(row, 2).Value = "環境タイプ";
        worksheet.Cell(row, 3).Value = "説明";
        row++;

        foreach (var env in project.Environments.OrderBy(e => e.EnvironmentType))
        {
            worksheet.Cell(row, 1).Value = env.Name;
            worksheet.Cell(row, 2).Value = env.EnvironmentType.ToString();
            worksheet.Cell(row, 3).Value = env.Description ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();
    }

    private void CreateEnvironmentDetailSheet(IXLWorksheet worksheet, EnvironmentExportData data)
    {
        worksheet.Cell("A1").Value = $"{data.Environment.Name} - リソース詳細";
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 14;

        // リソース一覧
        int row = 3;
        worksheet.Cell(row, 1).Value = "リソース名";
        worksheet.Cell(row, 2).Value = "タイプ";
        worksheet.Cell(row, 3).Value = "プロパティ";

        var headerRange = worksheet.Range(row, 1, row, 3);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        row++;

        foreach (var resource in data.Resources)
        {
            worksheet.Cell(row, 1).Value = resource.Name;
            worksheet.Cell(row, 2).Value = resource.ResourceType;
            worksheet.Cell(row, 3).Value = resource.PropertiesJson ?? "{}";
            row++;
        }

        worksheet.Columns().AdjustToContents();
    }

    public async Task<byte[]> ExportTemplateToExcelAsync(Guid templateId)
    {
        _logger.LogInformation("Exporting template {TemplateId} to Excel", templateId);

        var template = await _context.Templates
            .Include(t => t.Parameters)
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("テンプレート");

        // テンプレート情報
        worksheet.Cell("A1").Value = "テンプレート情報";
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;

        int row = 3;
        worksheet.Cell(row, 1).Value = "テンプレート名";
        worksheet.Cell(row, 2).Value = template.Name;
        row++;

        worksheet.Cell(row, 1).Value = "説明";
        worksheet.Cell(row, 2).Value = template.Description ?? "";
        row++;

        worksheet.Cell(row, 1).Value = "バージョン";
        worksheet.Cell(row, 2).Value = template.Version;
        row++;

        worksheet.Cell(row, 1).Value = "作成者";
        worksheet.Cell(row, 2).Value = template.Creator?.UserName ?? "N/A";
        row++;

        // パラメーター一覧
        row += 2;
        worksheet.Cell(row, 1).Value = "パラメーター一覧";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        worksheet.Cell(row, 1).Value = "パラメーター名";
        worksheet.Cell(row, 2).Value = "型";
        worksheet.Cell(row, 3).Value = "必須";
        worksheet.Cell(row, 4).Value = "デフォルト値";
        worksheet.Cell(row, 5).Value = "説明";

        var headerRange = worksheet.Range(row, 1, row, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
        row++;

        foreach (var param in template.Parameters.OrderBy(p => p.DisplayOrder))
        {
            worksheet.Cell(row, 1).Value = param.Name;
            worksheet.Cell(row, 2).Value = param.Type;
            worksheet.Cell(row, 3).Value = param.IsRequired ? "はい" : "いいえ";
            worksheet.Cell(row, 4).Value = param.DefaultValue ?? "";
            worksheet.Cell(row, 5).Value = param.Description ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        _logger.LogInformation("Excel export completed for template {TemplateId}", templateId);

        return stream.ToArray();
    }

    public async Task<byte[]> ExportToPdfAsync(Guid environmentId)
    {
        // PDF出力はフェーズ3の後半で実装
        _logger.LogWarning("PDF export is not yet implemented");
        throw new NotImplementedException("PDF export is not yet implemented");
    }

    public async Task<string> ExportToMarkdownAsync(Guid environmentId)
    {
        _logger.LogInformation("Exporting environment {EnvironmentId} to Markdown", environmentId);

        var data = await GetEnvironmentDataAsync(environmentId);
        var sb = new StringBuilder();

        // ヘッダー
        sb.AppendLine($"# {data.Project.Name} - {data.Environment.Name}");
        sb.AppendLine();
        sb.AppendLine($"**環境タイプ**: {data.Environment.EnvironmentType}");
        sb.AppendLine($"**説明**: {data.Environment.Description ?? "なし"}");
        sb.AppendLine($"**出力日時**: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
        sb.AppendLine();

        // サマリー
        sb.AppendLine("## 概要");
        sb.AppendLine();
        sb.AppendLine($"- 総リソース数: {data.Resources.Count}");
        sb.AppendLine($"- 接続数: {data.Connections.Count}");
        sb.AppendLine();

        // リソース一覧
        sb.AppendLine("## リソース一覧");
        sb.AppendLine();

        var resourcesByType = data.Resources
            .GroupBy(r => r.ResourceType)
            .OrderBy(g => g.Key);

        foreach (var group in resourcesByType)
        {
            sb.AppendLine($"### {group.Key}");
            sb.AppendLine();

            foreach (var resource in group.OrderBy(r => r.Name))
            {
                sb.AppendLine($"#### {resource.Name}");
                sb.AppendLine();

                if (resource.Properties != null && resource.Properties.Any())
                {
                    sb.AppendLine("**プロパティ**:");
                    foreach (var prop in resource.Properties)
                    {
                        sb.AppendLine($"- {prop.Key}: `{prop.Value}`");
                    }
                    sb.AppendLine();
                }
            }
        }

        // 接続情報
        if (data.Connections.Any())
        {
            sb.AppendLine("## リソース接続");
            sb.AppendLine();

            sb.AppendLine("| 接続元 | 接続先 | 接続タイプ |");
            sb.AppendLine("|--------|--------|-----------|");

            foreach (var conn in data.Connections)
            {
                sb.AppendLine($"| {conn.SourceResource?.Name ?? "N/A"} | {conn.TargetResource?.Name ?? "N/A"} | {conn.ConnectionType} |");
            }
            sb.AppendLine();
        }

        _logger.LogInformation("Markdown export completed for environment {EnvironmentId}", environmentId);

        return sb.ToString();
    }
}
