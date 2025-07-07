using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using AzureDocGen.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;

namespace AzureDocGen.MigrationService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await RunMigrationAsync(dbContext, stoppingToken);
            await SeedDataAsync(dbContext, stoppingToken);

            _logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database");
            activity?.SetStatus(ActivityStatusCode.Error, ex.ToString());
            throw;
        }
        finally
        {
            // Stop the application once the work is completed
            _hostApplicationLifetime.StopApplication();
        }
    }

    private async Task RunMigrationAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            try
            {
                // Check if database exists and if we need to recreate it
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                
                if (canConnect)
                {
                    // Check for migration conflicts by comparing applied vs available migrations
                    var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
                    var allMigrations = dbContext.Database.GetMigrations();
                    
                    // If there are applied migrations not in current migration list, we need to recreate
                    var obsoleteMigrations = appliedMigrations.Except(allMigrations);
                    
                    if (obsoleteMigrations.Any())
                    {
                        _logger.LogWarning("Found obsolete migrations in database: {ObsoleteMigrations}. Recreating database.", 
                            string.Join(", ", obsoleteMigrations));
                        
                        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
                        _logger.LogInformation("Database deleted due to migration conflicts");
                    }
                }
                
                // Apply migrations (this will create the database if it doesn't exist)
                _logger.LogInformation("Checking for pending migrations");
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
                
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                        pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                    await dbContext.Database.MigrateAsync(cancellationToken);
                    _logger.LogInformation("Migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("No pending migrations found");
                }
            }
            catch (Exception ex) when (ex.Message.Contains("already an object named"))
            {
                _logger.LogWarning("Database schema conflict detected. Recreating database.");
                await dbContext.Database.EnsureDeletedAsync(cancellationToken);
                await dbContext.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Database recreated and migrations applied successfully");
            }
        });
    }

    private async Task SeedDataAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Check if we need to seed initial data
            var userCount = await dbContext.Users.CountAsync(cancellationToken);
            var systemRoleCount = await dbContext.SystemRoles.CountAsync(cancellationToken);
            
            _logger.LogInformation("Database state: {UserCount} users, {SystemRoleCount} system roles", userCount, systemRoleCount);
            
            // Check if system admin user exists
            var adminUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == "admin@azuredocgen.com", cancellationToken);
            
            if (adminUser == null)
            {
                _logger.LogInformation("Creating system administrator user");
                await SeedSystemAdminAsync(dbContext, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("System administrator user created");
            }
            else
            {
                _logger.LogInformation("System administrator user already exists: {Email} (ID: {Id})", adminUser.Email, adminUser.Id);
                
                // Check if system admin role exists for this user
                var hasSystemAdminRole = await dbContext.SystemRoles
                    .AnyAsync(sr => sr.UserId == adminUser.Id && sr.RoleType == SystemRoleType.SystemAdministrator && sr.IsActive, cancellationToken);
                
                if (!hasSystemAdminRole)
                {
                    _logger.LogInformation("Adding system administrator role to existing user");
                    var systemRole = new SystemRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = adminUser.Id,
                        RoleType = SystemRoleType.SystemAdministrator,
                        GrantedAt = DateTime.UtcNow,
                        GrantedBy = adminUser.Id,
                        IsActive = true
                    };
                    
                    dbContext.SystemRoles.Add(systemRole);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("=== SYSTEM ADMINISTRATOR ROLE ADDED ===");
                    _logger.LogInformation("Added system administrator role to existing user:");
                    _logger.LogInformation("  - User Email: {Email}", adminUser.Email);
                    _logger.LogInformation("  - User ID: {UserId}", adminUser.Id);
                    _logger.LogInformation("  - Role ID: {RoleId}", systemRole.Id);
                    _logger.LogInformation("  - Role Type: {RoleType}", systemRole.RoleType);
                    _logger.LogInformation("  - Granted At: {GrantedAt}", systemRole.GrantedAt);
                    _logger.LogInformation("  - Is Active: {IsActive}", systemRole.IsActive);
                    _logger.LogInformation("========================================");
                }
                else
                {
                    _logger.LogInformation("System administrator role already exists for user {Email}", adminUser.Email);
                }
            }
        });
    }

    private async Task SeedSystemAdminAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "admin@azuredocgen.com",
            NormalizedUserName = "ADMIN@AZUREDOCGEN.COM",
            Email = "admin@azuredocgen.com",
            NormalizedEmail = "ADMIN@AZUREDOCGEN.COM",
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            Department = "IT",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        // Set a default password (should be changed on first login)
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");

        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Grant system administrator role
        var systemRole = new SystemRole
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            RoleType = SystemRoleType.SystemAdministrator,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = adminUser.Id, // Self-granted for initial setup
            IsActive = true
        };
        
        dbContext.SystemRoles.Add(systemRole);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("=== SYSTEM ADMINISTRATOR CREATED ===");
        _logger.LogInformation("Created system administrator user:");
        _logger.LogInformation("  - Email: {Email}", adminUser.Email);
        _logger.LogInformation("  - User ID: {UserId}", adminUser.Id);
        _logger.LogInformation("  - Username: {UserName}", adminUser.UserName);
        _logger.LogInformation("  - Full Name: {FirstName} {LastName}", adminUser.FirstName, adminUser.LastName);
        _logger.LogInformation("Created system administrator role:");
        _logger.LogInformation("  - Role ID: {RoleId}", systemRole.Id);
        _logger.LogInformation("  - Role Type: {RoleType}", systemRole.RoleType);
        _logger.LogInformation("  - User ID: {UserId}", systemRole.UserId);
        _logger.LogInformation("  - Granted At: {GrantedAt}", systemRole.GrantedAt);
        _logger.LogInformation("  - Is Active: {IsActive}", systemRole.IsActive);
        _logger.LogInformation("=====================================");
    }
}