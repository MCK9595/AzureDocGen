var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server database
var sqlServer = builder.AddSqlServer("sqldata");
var database = sqlServer.AddDatabase("azuredocgen");

// Add Migration Service to run database migrations
var migrationService = builder.AddProject<Projects.AzureDocGen_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

// Add API service with automatic health checks via MapDefaultEndpoints()
var apiService = builder.AddProject<Projects.AzureDocGen_ApiService>("apiservice")
    .WithReference(database)
    .WaitFor(migrationService);

// Add Web frontend with service references
builder.AddProject<Projects.AzureDocGen_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReference(database)
    .WaitFor(apiService)
    .WaitFor(migrationService);

builder.Build().Run();
