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
        try
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            _logger.LogInformation("SystemAdministratorRequirementHandler: Starting authorization check");
            _logger.LogInformation("SystemAdministratorRequirementHandler: UserId from claims: {UserId}", userId);
            _logger.LogInformation("SystemAdministratorRequirementHandler: User.Identity.Name: {Name}", context.User.Identity?.Name);
            _logger.LogInformation("SystemAdministratorRequirementHandler: User.Identity.IsAuthenticated: {IsAuthenticated}", context.User.Identity?.IsAuthenticated);
            
            // すべてのクレームをログ出力
            var claims = context.User.Claims.ToList();
            _logger.LogInformation("SystemAdministratorRequirementHandler: User has {ClaimCount} claims", claims.Count);
            foreach (var claim in claims)
            {
                _logger.LogDebug("SystemAdministratorRequirementHandler: Claim - Type: {Type}, Value: {Value}", claim.Type, claim.Value);
            }
            
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("SystemAdministratorRequirementHandler: No userId found in claims, failing authorization");
                _logger.LogWarning("SystemAdministratorRequirementHandler: Available claim types: {ClaimTypes}", 
                    string.Join(", ", claims.Select(c => c.Type)));
                context.Fail();
                return;
            }

            _logger.LogInformation("SystemAdministratorRequirementHandler: Checking system role for userId: {UserId}", userId);
            var hasSystemRole = await _permissionService.HasSystemRoleAsync(userId, SystemRoleType.SystemAdministrator);
            _logger.LogInformation("SystemAdministratorRequirementHandler: HasSystemRole result: {HasSystemRole} for userId: {UserId}", hasSystemRole, userId);
            
            if (hasSystemRole)
            {
                _logger.LogInformation("SystemAdministratorRequirementHandler: Authorization succeeded for userId: {UserId}", userId);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("SystemAdministratorRequirementHandler: Authorization failed for userId: {UserId} - user does not have SystemAdministrator role", userId);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SystemAdministratorRequirementHandler: Exception occurred during authorization check");
            context.Fail();
        }
    }
}