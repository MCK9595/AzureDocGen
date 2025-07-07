# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AzureDocGen is a .NET Aspire-based web application for creating Azure infrastructure design documents. It follows a monolithic architecture with Blazor frontend and ASP.NET Core backend, using Entity Framework Core for data access.

## Commands

### Build and Run
```bash
# Build the entire solution
dotnet build

# Run the application using Aspire orchestration
cd AzureDocGen.AppHost
dotnet run

# Run with specific launch profile
dotnet run --launch-profile https
```

### Database Operations
```bash
# Create new migration (from AzureDocGen.Web directory)
cd AzureDocGen.Web
dotnet ef migrations add MigrationName -p ../AzureDocGen.Data -s .

# Note: Migrations are automatically applied by the Migration Service when running via Aspire
# Manual migration is not needed in development
```

### Service URLs
- AppHost Dashboard: https://localhost:17019
- Web Frontend: https://localhost:7089 (redirects from http://localhost:5062)
- API Service: http://localhost:5368

## Architecture

### Service Structure
```
AzureDocGen.AppHost (Orchestrator)
├── SQL Server (Container/Azure SQL)
├── AzureDocGen.MigrationService
│   └── Database initialization and migrations
├── AzureDocGen.ApiService
│   └── Weather API endpoints (/weatherforecast)
└── AzureDocGen.Web (Frontend)
    ├── Blazor Components (Interactive UI)
    ├── MVC Controllers (Auth, Admin)
    └── Data Layer (EF Core)
```

### Key Architectural Patterns
1. **Service Discovery**: Uses Aspire's implicit service discovery. Services reference each other using `"https+http://servicename"` format (e.g., `"https+http://apiservice"`).

2. **Authentication**: Hybrid approach supporting both ASP.NET Core Identity and Azure AD. Components require `CascadingAuthenticationState` wrapper in App.razor.

3. **Routing Priority**: 
   - `MapDefaultEndpoints()` (health checks) - first
   - `MapControllerRoute()` (MVC) - second
   - `MapRazorComponents()` (Blazor) - last

4. **Cookie Security**: Development uses `CookieSecurePolicy.SameAsRequest` and `SameSiteMode.Lax` for Aspire compatibility.

### Project Dependencies
- **AzureDocGen.AppHost**: References all services, orchestrates with Aspire
- **AzureDocGen.Web**: References ServiceDefaults and Data projects
- **AzureDocGen.ApiService**: References ServiceDefaults only
- **AzureDocGen.MigrationService**: References ServiceDefaults and Data projects, runs database migrations
- **AzureDocGen.Data**: Standalone data access layer
- **AzureDocGen.ServiceDefaults**: Shared configuration (OpenTelemetry, health checks)

## Business Context

This application implements an Azure infrastructure design document creation tool with:
- Template management with versioning
- Project/environment hierarchy (1 project → N environments)
- Visual design interface with drag-and-drop
- Approval workflow system
- Export capabilities (Excel, PDF, Markdown)
- Role-based access control (Administrator, ProjectManager, Developer, Viewer)

## Development Notes

1. **Aspire Service Discovery**: Never hardcode service URLs. Use service names registered in AppHost.
2. **Health Checks**: Avoid custom health check paths. Use `MapDefaultEndpoints()` for automatic `/health` and `/alive` endpoints.
3. **Database Context**: ApplicationDbContext includes ASP.NET Identity tables and custom entities for projects, templates, and approvals.
4. **Database Initialization**: Database creation and migrations are handled automatically by the Migration Service on startup. Do not run migrations manually from Web project.
5. **Service Dependencies**: Migration Service runs first, then API and Web services wait for migrations to complete.
6. **Secret Management**: Use Secret Manager for development, Azure Key Vault for production (as specified in authentication-design.md).