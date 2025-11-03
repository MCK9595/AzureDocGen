using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Services;

/// <summary>
/// ドキュメント出力サービスのインターフェース
/// </summary>
public interface IDocumentExportService
{
    /// <summary>
    /// 設計書をExcel形式で出力
    /// </summary>
    Task<byte[]> ExportToExcelAsync(Guid environmentId);

    /// <summary>
    /// 設計書をPDF形式で出力
    /// </summary>
    Task<byte[]> ExportToPdfAsync(Guid environmentId);

    /// <summary>
    /// 設計書をMarkdown形式で出力
    /// </summary>
    Task<string> ExportToMarkdownAsync(Guid environmentId);

    /// <summary>
    /// プロジェクト全体を出力（全環境）
    /// </summary>
    Task<byte[]> ExportProjectToExcelAsync(Guid projectId);

    /// <summary>
    /// テンプレートを出力
    /// </summary>
    Task<byte[]> ExportTemplateToExcelAsync(Guid templateId);

    /// <summary>
    /// リソース情報を取得
    /// </summary>
    Task<EnvironmentExportData> GetEnvironmentDataAsync(Guid environmentId);
}

/// <summary>
/// 環境出力用データ
/// </summary>
public class EnvironmentExportData
{
    public Environment Environment { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public List<Resource> Resources { get; set; } = new();
    public List<ResourceConnection> Connections { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 出力形式
/// </summary>
public enum ExportFormat
{
    Excel,
    Pdf,
    Markdown
}
