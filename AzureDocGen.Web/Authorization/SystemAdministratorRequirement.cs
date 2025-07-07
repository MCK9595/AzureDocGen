using Microsoft.AspNetCore.Authorization;
using AzureDocGen.Data.Enums;
using AzureDocGen.Web.Services;

namespace AzureDocGen.Web.Authorization;

/// <summary>
/// システム管理者権限の要件
/// </summary>
public class SystemAdministratorRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// システム管理者権限のハンドラー
/// </summary>
public class SystemAdministratorRequirementHandler : AuthorizationHandler<SystemAdministratorRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<SystemAdministratorRequirementHandler> _logger;

    public SystemAdministratorRequirementHandler(IPermissionService permissionService, ILogger<SystemAdministratorRequirementHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SystemAdministratorRequirement requirement)
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        _logger.LogDebug("SystemAdministratorRequirementHandler: UserId from claims: {UserId}", userId);
        _logger.LogDebug("SystemAdministratorRequirementHandler: User.Identity.Name: {Name}", context.User.Identity?.Name);
        _logger.LogDebug("SystemAdministratorRequirementHandler: User.Identity.IsAuthenticated: {IsAuthenticated}", context.User.Identity?.IsAuthenticated);
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogDebug("SystemAdministratorRequirementHandler: No userId found in claims, failing authorization");
            context.Fail();
            return;
        }

        var hasSystemRole = await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator);
        _logger.LogDebug("SystemAdministratorRequirementHandler: HasSystemRole result: {HasSystemRole}", hasSystemRole);
        
        if (hasSystemRole)
        {
            _logger.LogDebug("SystemAdministratorRequirementHandler: Authorization succeeded for userId: {UserId}", userId);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogDebug("SystemAdministratorRequirementHandler: Authorization failed for userId: {UserId}", userId);
        }
    }
}