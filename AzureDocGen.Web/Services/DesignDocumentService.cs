using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;

namespace AzureDocGen.Web.Services;

/// <summary>
/// 設計書管理サービスの実装
/// </summary>
public class DesignDocumentService : IDesignDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DesignDocumentService> _logger;

    public DesignDocumentService(
        ApplicationDbContext context,
        ILogger<DesignDocumentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DesignDocument> CreateDocumentAsync(Guid projectId, string title, string createdBy)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        var document = new DesignDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title,
            Status = WorkflowStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.DesignDocuments.Add(document);

        // 初期バージョンを作成
        var initialVersion = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DesignDocumentId = document.Id,
            Version = 1,
            Content = "{}",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            Comment = "初期バージョン"
        };

        _context.DocumentVersions.Add(initialVersion);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Design document {DocumentId} created by {UserId}", document.Id, createdBy);

        return document;
    }

    public async Task<DesignDocument?> GetDocumentByIdAsync(Guid documentId)
    {
        return await _context.DesignDocuments
            .Include(d => d.Project)
            .Include(d => d.Creator)
            .Include(d => d.Approver)
            .Include(d => d.Versions.OrderByDescending(v => v.Version))
            .FirstOrDefaultAsync(d => d.Id == documentId);
    }

    public async Task<List<DesignDocument>> GetProjectDocumentsAsync(Guid projectId)
    {
        return await _context.DesignDocuments
            .Include(d => d.Creator)
            .Include(d => d.Approver)
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<DesignDocument> UpdateDocumentAsync(Guid documentId, string title)
    {
        var document = await _context.DesignDocuments.FindAsync(documentId);
        if (document == null)
        {
            throw new InvalidOperationException($"Design document {documentId} not found");
        }

        document.Title = title;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Design document {DocumentId} updated", documentId);

        return document;
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, string deletedBy)
    {
        var document = await _context.DesignDocuments.FindAsync(documentId);
        if (document == null)
        {
            return false;
        }

        // ドラフトまたはアーカイブ済みの設計書のみ削除可能
        if (document.Status != WorkflowStatus.Draft && document.Status != WorkflowStatus.Archived)
        {
            throw new InvalidOperationException("承認済みまたはレビュー中の設計書は削除できません。");
        }

        _context.DesignDocuments.Remove(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Design document {DocumentId} deleted by {UserId}", documentId, deletedBy);

        return true;
    }

    public async Task<DocumentVersion> CreateVersionAsync(Guid documentId, string content, string comment, string createdBy)
    {
        var document = await _context.DesignDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            throw new InvalidOperationException($"Design document {documentId} not found");
        }

        var latestVersion = document.Versions.OrderByDescending(v => v.Version).FirstOrDefault();
        var newVersionNumber = (latestVersion?.Version ?? 0) + 1;

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DesignDocumentId = documentId,
            Version = newVersionNumber,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            Comment = comment
        };

        _context.DocumentVersions.Add(version);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document version {Version} created for document {DocumentId}", newVersionNumber, documentId);

        return version;
    }

    public async Task<List<DocumentVersion>> GetDocumentVersionsAsync(Guid documentId)
    {
        return await _context.DocumentVersions
            .Include(v => v.Creator)
            .Where(v => v.DesignDocumentId == documentId)
            .OrderByDescending(v => v.Version)
            .ToListAsync();
    }

    public async Task<DesignDocument> UpdateStatusAsync(Guid documentId, WorkflowStatus status, string? updatedBy = null)
    {
        var document = await _context.DesignDocuments.FindAsync(documentId);
        if (document == null)
        {
            throw new InvalidOperationException($"Design document {documentId} not found");
        }

        document.Status = status;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Design document {DocumentId} status updated to {Status} by {UserId}",
            documentId, status, updatedBy ?? "system");

        return document;
    }

    public async Task<DesignDocument> ApproveDocumentAsync(Guid documentId, string approvedBy)
    {
        var document = await _context.DesignDocuments.FindAsync(documentId);
        if (document == null)
        {
            throw new InvalidOperationException($"Design document {documentId} not found");
        }

        if (document.Status != WorkflowStatus.InReview)
        {
            throw new InvalidOperationException("レビュー中の設計書のみ承認できます。");
        }

        document.Status = WorkflowStatus.Approved;
        document.ApprovedAt = DateTime.UtcNow;
        document.ApprovedBy = approvedBy;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Design document {DocumentId} approved by {UserId}", documentId, approvedBy);

        return document;
    }

    public async Task<DesignDocument> RejectDocumentAsync(Guid documentId, string rejectedBy, string reason)
    {
        var document = await _context.DesignDocuments.FindAsync(documentId);
        if (document == null)
        {
            throw new InvalidOperationException($"Design document {documentId} not found");
        }

        if (document.Status != WorkflowStatus.InReview)
        {
            throw new InvalidOperationException("レビュー中の設計書のみ却下できます。");
        }

        document.Status = WorkflowStatus.Rejected;

        // 却下理由を新しいバージョンのコメントとして記録
        var rejectionVersion = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DesignDocumentId = documentId,
            Version = document.Versions.Max(v => v.Version) + 1,
            Content = document.Versions.OrderByDescending(v => v.Version).First().Content,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = rejectedBy,
            Comment = $"却下: {reason}"
        };

        _context.DocumentVersions.Add(rejectionVersion);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Design document {DocumentId} rejected by {UserId}: {Reason}",
            documentId, rejectedBy, reason);

        return document;
    }
}
