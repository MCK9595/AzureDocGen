using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 設計書管理サービスのインターフェース
/// </summary>
public interface IDesignDocumentService
{
    /// <summary>
    /// 設計書を作成する
    /// </summary>
    Task<DesignDocument> CreateDocumentAsync(Guid projectId, string title, string createdBy);

    /// <summary>
    /// 設計書をIDで取得する
    /// </summary>
    Task<DesignDocument?> GetDocumentByIdAsync(Guid documentId);

    /// <summary>
    /// プロジェクトの設計書一覧を取得する
    /// </summary>
    Task<List<DesignDocument>> GetProjectDocumentsAsync(Guid projectId);

    /// <summary>
    /// 設計書を更新する
    /// </summary>
    Task<DesignDocument> UpdateDocumentAsync(Guid documentId, string title);

    /// <summary>
    /// 設計書を削除する
    /// </summary>
    Task<bool> DeleteDocumentAsync(Guid documentId, string deletedBy);

    /// <summary>
    /// 設計書の新しいバージョンを作成する
    /// </summary>
    Task<DocumentVersion> CreateVersionAsync(Guid documentId, string content, string comment, string createdBy);

    /// <summary>
    /// 設計書のバージョン一覧を取得する
    /// </summary>
    Task<List<DocumentVersion>> GetDocumentVersionsAsync(Guid documentId);

    /// <summary>
    /// 設計書のステータスを更新する
    /// </summary>
    Task<DesignDocument> UpdateStatusAsync(Guid documentId, WorkflowStatus status, string? updatedBy = null);

    /// <summary>
    /// 設計書を承認する
    /// </summary>
    Task<DesignDocument> ApproveDocumentAsync(Guid documentId, string approvedBy);

    /// <summary>
    /// 設計書を却下する
    /// </summary>
    Task<DesignDocument> RejectDocumentAsync(Guid documentId, string rejectedBy, string reason);
}
