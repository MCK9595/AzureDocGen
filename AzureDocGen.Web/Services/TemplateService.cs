using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Enums;
using System.Text.Json;

namespace AzureDocGen.Web.Services;

/// <summary>
/// テンプレート管理サービスの実装
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        ApplicationDbContext context,
        IPermissionService permissionService,
        ILogger<TemplateService> logger)
    {
        _context = context;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<Template> CreateTemplateAsync(string name, string description, SharingLevel sharingLevel, string createdBy)
    {
        var template = new Template
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            SharingLevel = sharingLevel,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            StructureJson = JsonSerializer.Serialize(new Dictionary<string, object>())
        };

        _context.Templates.Add(template);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Template {TemplateId} created by {UserId}", template.Id, createdBy);
        
        return template;
    }

    public async Task<Template?> GetTemplateByIdAsync(Guid templateId)
    {
        return await _context.Templates
            .Include(t => t.Parameters)
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == templateId);
    }

    public async Task<List<Template>> GetUserTemplatesAsync(string userId, Guid? projectId = null)
    {
        var query = _context.Templates.Include(t => t.Parameters).Include(t => t.Creator).AsQueryable();

        // システム管理者の場合は全テンプレートを返す
        if (await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator))
        {
            return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }

        // 権限に基づいてフィルター
        var accessibleTemplates = new List<Template>();

        // 1. 自分が作成したテンプレート
        var ownTemplates = await query
            .Where(t => t.CreatedBy == userId)
            .ToListAsync();
        accessibleTemplates.AddRange(ownTemplates);

        // 2. グローバル共有テンプレート
        var globalTemplates = await query
            .Where(t => t.SharingLevel == SharingLevel.Global && t.CreatedBy != userId)
            .ToListAsync();
        accessibleTemplates.AddRange(globalTemplates);

        // 3. プロジェクト共有テンプレート（ユーザーがアクセスできるプロジェクトのもの）
        var accessibleProjectIds = await _permissionService.GetAccessibleProjectIdsAsync(userId);
        var projectTemplates = await query
            .Where(t => t.SharingLevel == SharingLevel.Project && 
                       accessibleProjectIds.Contains(Guid.Empty) && // TODO: プロジェクトとテンプレートの関連付けを実装
                       t.CreatedBy != userId)
            .ToListAsync();
        accessibleTemplates.AddRange(projectTemplates);

        return accessibleTemplates
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
    }

    public async Task<Template> UpdateTemplateAsync(Guid templateId, string name, string description, SharingLevel sharingLevel)
    {
        var template = await _context.Templates.FindAsync(templateId);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        template.Name = name;
        template.Description = description;
        template.SharingLevel = sharingLevel;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Template {TemplateId} updated", templateId);
        
        return template;
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId, string deletedBy)
    {
        var template = await _context.Templates
            .Include(t => t.Parameters)
            .FirstOrDefaultAsync(t => t.Id == templateId);
            
        if (template == null)
        {
            return false;
        }

        _context.Templates.Remove(template);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Template {TemplateId} deleted by {UserId}", templateId, deletedBy);
        
        return true;
    }

    public async Task<TemplateParameter> AddParameterAsync(Guid templateId, string parameterName, string parameterType, bool isRequired, string? defaultValue = null)
    {
        var template = await _context.Templates.FindAsync(templateId);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        var parameter = new TemplateParameter
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Name = parameterName,
            Type = Enum.TryParse<AzureDocGen.Data.Entities.ParameterType>(parameterType, true, out var type) ? type : AzureDocGen.Data.Entities.ParameterType.Text,
            IsRequired = isRequired,
            DefaultValue = defaultValue ?? string.Empty
        };

        _context.TemplateParameters.Add(parameter);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Parameter {ParameterName} added to template {TemplateId}", parameterName, templateId);
        
        return parameter;
    }

    public async Task<TemplateParameter> UpdateParameterAsync(Guid parameterId, string parameterName, string parameterType, bool isRequired, string? defaultValue = null)
    {
        var parameter = await _context.TemplateParameters.FindAsync(parameterId);
        if (parameter == null)
        {
            throw new InvalidOperationException($"Parameter {parameterId} not found");
        }

        parameter.Name = parameterName;
        parameter.Type = Enum.TryParse<AzureDocGen.Data.Entities.ParameterType>(parameterType, true, out var type) ? type : AzureDocGen.Data.Entities.ParameterType.Text;
        parameter.IsRequired = isRequired;
        parameter.DefaultValue = defaultValue ?? string.Empty;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Parameter {ParameterId} updated", parameterId);
        
        return parameter;
    }

    public async Task<bool> DeleteParameterAsync(Guid parameterId)
    {
        var parameter = await _context.TemplateParameters.FindAsync(parameterId);
        if (parameter == null)
        {
            return false;
        }

        _context.TemplateParameters.Remove(parameter);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Parameter {ParameterId} deleted", parameterId);
        
        return true;
    }

    public async Task<List<TemplateParameter>> GetTemplateParametersAsync(Guid templateId)
    {
        return await _context.TemplateParameters
            .Where(p => p.TemplateId == templateId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Template> CreateVersionAsync(Guid baseTemplateId, string createdBy)
    {
        var baseTemplate = await GetTemplateByIdAsync(baseTemplateId);
        if (baseTemplate == null)
        {
            throw new InvalidOperationException($"Base template {baseTemplateId} not found");
        }

        // 同名テンプレートの最新バージョンを取得
        var latestVersion = await _context.Templates
            .Where(t => t.Name == baseTemplate.Name && t.CreatedBy == createdBy)
            .MaxAsync(t => (int?)t.Version) ?? 0;

        var newTemplate = new Template
        {
            Id = Guid.NewGuid(),
            Name = baseTemplate.Name,
            Description = baseTemplate.Description,
            SharingLevel = baseTemplate.SharingLevel,
            Version = latestVersion + 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            StructureJson = baseTemplate.StructureJson
        };

        _context.Templates.Add(newTemplate);

        // パラメーターもコピー
        foreach (var param in baseTemplate.Parameters)
        {
            var newParam = new TemplateParameter
            {
                Id = Guid.NewGuid(),
                TemplateId = newTemplate.Id,
                Name = param.Name,
                Type = param.Type,
                IsRequired = param.IsRequired,
                DefaultValue = param.DefaultValue
            };
            _context.TemplateParameters.Add(newParam);
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Template version {Version} created from {BaseTemplateId}", newTemplate.Version, baseTemplateId);
        
        return newTemplate;
    }

    public async Task<List<Template>> GetTemplateVersionsAsync(string templateName, string createdBy)
    {
        return await _context.Templates
            .Where(t => t.Name == templateName && t.CreatedBy == createdBy)
            .OrderByDescending(t => t.Version)
            .ToListAsync();
    }

    public async Task<Template> UpdateTemplateStructureAsync(Guid templateId, Dictionary<string, object> structure)
    {
        var template = await _context.Templates.FindAsync(templateId);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        template.StructureJson = JsonSerializer.Serialize(structure);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Template {TemplateId} structure updated", templateId);
        
        return template;
    }

    public async Task<Template> DuplicateTemplateAsync(Guid templateId, string newName, string createdBy)
    {
        var originalTemplate = await GetTemplateByIdAsync(templateId);
        if (originalTemplate == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        var duplicatedTemplate = new Template
        {
            Id = Guid.NewGuid(),
            Name = newName,
            Description = $"{originalTemplate.Description} (コピー)",
            SharingLevel = SharingLevel.Private, // 複製は常にプライベートから開始
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            StructureJson = originalTemplate.StructureJson
        };

        _context.Templates.Add(duplicatedTemplate);

        // パラメーターも複製
        foreach (var param in originalTemplate.Parameters)
        {
            var duplicatedParam = new TemplateParameter
            {
                Id = Guid.NewGuid(),
                TemplateId = duplicatedTemplate.Id,
                Name = param.Name,
                Type = param.Type,
                IsRequired = param.IsRequired,
                DefaultValue = param.DefaultValue
            };
            _context.TemplateParameters.Add(duplicatedParam);
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Template {TemplateId} duplicated as {NewName} by {UserId}", templateId, newName, createdBy);
        
        return duplicatedTemplate;
    }

    public async Task<TemplateStatistics> GetTemplateStatisticsAsync(string userId)
    {
        var templates = await GetUserTemplatesAsync(userId);
        
        var stats = new TemplateStatistics
        {
            TotalTemplates = templates.Count,
            PrivateTemplates = templates.Count(t => t.SharingLevel == SharingLevel.Private),
            ProjectTemplates = templates.Count(t => t.SharingLevel == SharingLevel.Project),
            GlobalTemplates = templates.Count(t => t.SharingLevel == SharingLevel.Global),
            RecentlyUsedCount = templates.Count(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
            LastCreatedDate = templates.Any() ? templates.Max(t => t.CreatedAt) : null
        };

        return stats;
    }
}