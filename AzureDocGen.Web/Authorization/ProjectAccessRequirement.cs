using Microsoft.AspNetCore.Authorization;
using AzureDocGen.Data.Enums;
using AzureDocGen.Web.Services;

namespace AzureDocGen.Web.Authorization;

/// <summary>
/// プロジェクトアクセス権限の要件
/// </summary>
public class ProjectAccessRequirement : IAuthorizationRequirement
{
    public ProjectRoleType MinimumRole { get; }

    public ProjectAccessRequirement(ProjectRoleType minimumRole)
    {
        MinimumRole = minimumRole;
    }
}

/// <summary>
/// プロジェクトアクセス権限のハンドラー
/// </summary>
public class ProjectAccessRequirementHandler : AuthorizationHandler<ProjectAccessRequirement, Guid>
{
    private readonly IPermissionService _permissionService;

    public ProjectAccessRequirementHandler(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectAccessRequirement requirement,
        Guid projectId)
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            context.Fail();
            return;
        }

        if (await _permissionService.HasProjectRoleOrHigherAsync(userId, projectId, requirement.MinimumRole))
        {
            context.Succeed(requirement);
        }
    }
}