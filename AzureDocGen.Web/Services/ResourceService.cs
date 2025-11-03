using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using System.Text.Json;

namespace AzureDocGen.Web.Services;

/// <summary>
/// リソース管理サービスの実装
/// </summary>
public class ResourceService : IResourceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResourceService> _logger;

    public ResourceService(
        ApplicationDbContext context,
        ILogger<ResourceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Resource> CreateResourceAsync(Guid environmentId, string resourceType, string name, Position position)
    {
        var environment = await _context.Environments.FindAsync(environmentId);
        if (environment == null)
        {
            throw new InvalidOperationException($"Environment {environmentId} not found");
        }

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            EnvironmentId = environmentId,
            ResourceType = resourceType,
            Name = name,
            VisualPosition = position,
            PropertiesJson = JsonSerializer.Serialize(new Dictionary<string, object>())
        };

        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource {ResourceId} created in environment {EnvironmentId}", resource.Id, environmentId);

        return resource;
    }

    public async Task<Resource?> GetResourceByIdAsync(Guid resourceId)
    {
        return await _context.Resources
            .Include(r => r.Environment)
            .Include(r => r.Connections)
            .FirstOrDefaultAsync(r => r.Id == resourceId);
    }

    public async Task<List<Resource>> GetEnvironmentResourcesAsync(Guid environmentId)
    {
        return await _context.Resources
            .Where(r => r.EnvironmentId == environmentId)
            .ToListAsync();
    }

    public async Task<Resource> UpdateResourceAsync(Guid resourceId, string? name = null, Dictionary<string, object>? properties = null)
    {
        var resource = await _context.Resources.FindAsync(resourceId);
        if (resource == null)
        {
            throw new InvalidOperationException($"Resource {resourceId} not found");
        }

        if (name != null)
        {
            resource.Name = name;
        }

        if (properties != null)
        {
            resource.PropertiesJson = JsonSerializer.Serialize(properties);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource {ResourceId} updated", resourceId);

        return resource;
    }

    public async Task<Resource> UpdateResourcePositionAsync(Guid resourceId, Position position)
    {
        var resource = await _context.Resources.FindAsync(resourceId);
        if (resource == null)
        {
            throw new InvalidOperationException($"Resource {resourceId} not found");
        }

        resource.VisualPosition = position;
        await _context.SaveChangesAsync();

        _logger.LogDebug("Resource {ResourceId} position updated to ({X}, {Y})", resourceId, position.X, position.Y);

        return resource;
    }

    public async Task<bool> DeleteResourceAsync(Guid resourceId)
    {
        var resource = await _context.Resources
            .Include(r => r.Connections)
            .FirstOrDefaultAsync(r => r.Id == resourceId);

        if (resource == null)
        {
            return false;
        }

        // 関連する接続も削除
        _context.ResourceConnections.RemoveRange(resource.Connections);
        _context.Resources.Remove(resource);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource {ResourceId} deleted", resourceId);

        return true;
    }

    public async Task<ResourceConnection> CreateConnectionAsync(Guid sourceResourceId, Guid targetResourceId, string connectionType)
    {
        var sourceResource = await _context.Resources.FindAsync(sourceResourceId);
        var targetResource = await _context.Resources.FindAsync(targetResourceId);

        if (sourceResource == null || targetResource == null)
        {
            throw new InvalidOperationException("Source or target resource not found");
        }

        if (sourceResource.EnvironmentId != targetResource.EnvironmentId)
        {
            throw new InvalidOperationException("Resources must be in the same environment");
        }

        // 既存の接続をチェック
        var existingConnection = await _context.ResourceConnections
            .FirstOrDefaultAsync(c =>
                c.SourceResourceId == sourceResourceId &&
                c.TargetResourceId == targetResourceId);

        if (existingConnection != null)
        {
            throw new InvalidOperationException("Connection already exists");
        }

        var connection = new ResourceConnection
        {
            Id = Guid.NewGuid(),
            SourceResourceId = sourceResourceId,
            TargetResourceId = targetResourceId,
            ConnectionType = connectionType
        };

        _context.ResourceConnections.Add(connection);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Connection created from {SourceId} to {TargetId}", sourceResourceId, targetResourceId);

        return connection;
    }

    public async Task<List<ResourceConnection>> GetResourceConnectionsAsync(Guid resourceId)
    {
        return await _context.ResourceConnections
            .Include(c => c.SourceResource)
            .Include(c => c.TargetResource)
            .Where(c => c.SourceResourceId == resourceId || c.TargetResourceId == resourceId)
            .ToListAsync();
    }

    public async Task<List<ResourceConnection>> GetEnvironmentConnectionsAsync(Guid environmentId)
    {
        return await _context.ResourceConnections
            .Include(c => c.SourceResource)
            .Include(c => c.TargetResource)
            .Where(c => c.SourceResource!.EnvironmentId == environmentId)
            .ToListAsync();
    }

    public async Task<bool> DeleteConnectionAsync(Guid connectionId)
    {
        var connection = await _context.ResourceConnections.FindAsync(connectionId);
        if (connection == null)
        {
            return false;
        }

        _context.ResourceConnections.Remove(connection);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Connection {ConnectionId} deleted", connectionId);

        return true;
    }

    public async Task<List<Resource>> CreateResourcesFromTemplateAsync(Guid environmentId, Template template, Dictionary<string, string>? parameterValues = null)
    {
        var environment = await _context.Environments.FindAsync(environmentId);
        if (environment == null)
        {
            throw new InvalidOperationException($"Environment {environmentId} not found");
        }

        if (string.IsNullOrEmpty(template.StructureJson))
        {
            return new List<Resource>();
        }

        var resources = new List<Resource>();

        try
        {
            var structure = JsonSerializer.Deserialize<Dictionary<string, object>>(template.StructureJson);
            if (structure == null || !structure.ContainsKey("resources"))
            {
                return resources;
            }

            var resourcesElement = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(
                structure["resources"].ToString() ?? "[]");

            if (resourcesElement == null)
            {
                return resources;
            }

            var resourceIdMapping = new Dictionary<string, Guid>();

            // リソースを作成
            foreach (var resourceData in resourcesElement)
            {
                var resourceType = resourceData.ContainsKey("type") ? resourceData["type"].GetString() : "Unknown";
                var resourceName = resourceData.ContainsKey("name") ? resourceData["name"].GetString() : "Unnamed";

                Position? position = null;
                if (resourceData.ContainsKey("position"))
                {
                    var positionElement = resourceData["position"];
                    position = new Position
                    {
                        X = positionElement.TryGetProperty("x", out var x) ? x.GetDouble() : 0,
                        Y = positionElement.TryGetProperty("y", out var y) ? y.GetDouble() : 0,
                        Width = positionElement.TryGetProperty("width", out var w) ? w.GetDouble() : 100,
                        Height = positionElement.TryGetProperty("height", out var h) ? h.GetDouble() : 100
                    };
                }

                var resource = await CreateResourceAsync(environmentId, resourceType ?? "Unknown", resourceName ?? "Unnamed", position ?? new Position { Width = 100, Height = 100 });
                resources.Add(resource);

                if (resourceData.ContainsKey("id"))
                {
                    resourceIdMapping[resourceData["id"].GetString() ?? ""] = resource.Id;
                }
            }

            // 接続を作成
            if (structure.ContainsKey("connections"))
            {
                var connectionsElement = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(
                    structure["connections"].ToString() ?? "[]");

                if (connectionsElement != null)
                {
                    foreach (var connectionData in connectionsElement)
                    {
                        var sourceId = connectionData.ContainsKey("source") ? connectionData["source"].GetString() : null;
                        var targetId = connectionData.ContainsKey("target") ? connectionData["target"].GetString() : null;
                        var connectionType = connectionData.ContainsKey("type") ? connectionData["type"].GetString() : "default";

                        if (sourceId != null && targetId != null &&
                            resourceIdMapping.ContainsKey(sourceId) &&
                            resourceIdMapping.ContainsKey(targetId))
                        {
                            await CreateConnectionAsync(
                                resourceIdMapping[sourceId],
                                resourceIdMapping[targetId],
                                connectionType ?? "default");
                        }
                    }
                }
            }

            _logger.LogInformation("{Count} resources created from template {TemplateId} in environment {EnvironmentId}",
                resources.Count, template.Id, environmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating resources from template {TemplateId}", template.Id);
            throw;
        }

        return resources;
    }
}
