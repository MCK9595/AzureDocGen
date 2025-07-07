using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AzureDocGen.Data.Entities;

namespace AzureDocGen.Data.Contexts;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<Entities.Environment> Environments { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ResourceConnection> ResourceConnections { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<TemplateParameter> TemplateParameters { get; set; }
    public DbSet<DesignDocument> DesignDocuments { get; set; }
    public DbSet<DocumentVersion> DocumentVersions { get; set; }
    public DbSet<NamingRule> NamingRules { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    // New permission entities
    public DbSet<SystemRole> SystemRoles { get; set; }
    public DbSet<ProjectUserRole> ProjectUserRoles { get; set; }
    public DbSet<EnvironmentUserRole> EnvironmentUserRoles { get; set; }
    public DbSet<ReviewWorkflow> ReviewWorkflows { get; set; }
    public DbSet<ReviewAssignment> ReviewAssignments { get; set; }
    public DbSet<WorkflowHistory> WorkflowHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // SystemRole
        builder.Entity<SystemRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GrantedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.GrantedByUser)
                .WithMany()
                .HasForeignKey(e => e.GrantedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProjectUserRole
        builder.Entity<ProjectUserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GrantedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Project)
                .WithMany(p => p.ProjectUserRoles)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.GrantedByUser)
                .WithMany()
                .HasForeignKey(e => e.GrantedBy)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Unique constraint: user can have only one role per project
            entity.HasIndex(e => new { e.UserId, e.ProjectId, e.IsActive })
                .HasFilter("[IsActive] = 1")
                .IsUnique();
        });

        // EnvironmentUserRole
        builder.Entity<EnvironmentUserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GrantedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Environment)
                .WithMany()
                .HasForeignKey(e => e.EnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.GrantedByUser)
                .WithMany()
                .HasForeignKey(e => e.GrantedBy)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Unique constraint: user can have only one role per environment
            entity.HasIndex(e => new { e.UserId, e.EnvironmentId, e.IsActive })
                .HasFilter("[IsActive] = 1")
                .IsUnique();
        });

        // ReviewWorkflow
        builder.Entity<ReviewWorkflow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ReviewAssignment
        builder.Entity<ReviewAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Comment).HasMaxLength(2000);
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.Workflow)
                .WithMany(w => w.ReviewAssignments)
                .HasForeignKey(e => e.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Reviewer)
                .WithMany()
                .HasForeignKey(e => e.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.AssignedByUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WorkflowHistory
        builder.Entity<WorkflowHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Comment).HasMaxLength(2000);
            entity.Property(e => e.ActionAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.Workflow)
                .WithMany(w => w.WorkflowHistories)
                .HasForeignKey(e => e.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Actor)
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Project
        builder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.Creator)
                .WithMany(u => u.Projects)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Environment
        builder.Entity<Entities.Environment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Environments)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Resource
        builder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PropertiesJson)
                .HasColumnType("nvarchar(max)");
            
            entity.OwnsOne(e => e.VisualPosition, pos =>
            {
                pos.Property(p => p.X);
                pos.Property(p => p.Y);
                pos.Property(p => p.Width);
                pos.Property(p => p.Height);
            });
            
            entity.HasOne(e => e.Environment)
                .WithMany(env => env.Resources)
                .HasForeignKey(e => e.EnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ResourceConnection
        builder.Entity<ResourceConnection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConnectionType).HasMaxLength(100);
            
            entity.HasOne(e => e.SourceResource)
                .WithMany(r => r.Connections)
                .HasForeignKey(e => e.SourceResourceId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.TargetResource)
                .WithMany()
                .HasForeignKey(e => e.TargetResourceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Template
        builder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Version).HasDefaultValue(1);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.StructureJson)
                .HasColumnType("nvarchar(max)");
            
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TemplateParameter
        builder.Entity<TemplateParameter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.DefaultValue).HasMaxLength(500);
            entity.Property(e => e.ValidationRule).HasMaxLength(1000);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasOne(e => e.Template)
                .WithMany(t => t.Parameters)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DesignDocument
        builder.Entity<DesignDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DocumentVersion
        builder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.DesignDocument)
                .WithMany(d => d.Versions)
                .HasForeignKey(e => e.DesignDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // NamingRule
        builder.Entity<NamingRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Pattern).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Example).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.Project)
                .WithMany(p => p.NamingRules)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}