using AzureDocGen.Web;
using AzureDocGen.Web.Components;
using AzureDocGen.Web.Services;
using AzureDocGen.Web.Authorization;
using AzureDocGen.Data.Contexts;
using AzureDocGen.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add SQL Server client integration (.NET Aspire)
try
{
    builder.AddSqlServerDbContext<ApplicationDbContext>("azuredocgen");
}
catch (Exception)
{
    // Fallback for when running without Aspire AppHost
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("azuredocgen");
        options.UseSqlServer(connectionString);
    });
}

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // パスワード要件
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 6;

    // アカウントロックアウト設定
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // ユーザー設定
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = 
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // サインイン設定
    options.SignIn.RequireConfirmedEmail = false; // 開発環境では無効
    options.SignIn.RequireConfirmedAccount = false; // 開発環境では無効
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie認証の設定
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    // 開発環境では HTTPS を強制しない（Aspire では HTTP も使用される）
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax; // API calls との互換性のため Lax に変更
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// 認可ポリシーの設定
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
    
    // システムレベルポリシー
    options.AddPolicy("RequireSystemAdministrator", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new SystemAdministratorRequirement());
    });
    
    // プロジェクトレベルポリシー
    options.AddPolicy("RequireProjectOwner", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ProjectAccessRequirement(AzureDocGen.Data.Enums.ProjectRoleType.ProjectOwner));
    });
    
    options.AddPolicy("RequireProjectManager", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ProjectAccessRequirement(AzureDocGen.Data.Enums.ProjectRoleType.ProjectManager));
    });
    
    options.AddPolicy("RequireProjectReviewer", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ProjectAccessRequirement(AzureDocGen.Data.Enums.ProjectRoleType.ProjectReviewer));
    });
    
    options.AddPolicy("RequireProjectDeveloper", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ProjectAccessRequirement(AzureDocGen.Data.Enums.ProjectRoleType.ProjectDeveloper));
    });
    
    options.AddPolicy("RequireProjectViewer", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ProjectAccessRequirement(AzureDocGen.Data.Enums.ProjectRoleType.ProjectViewer));
    });
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

// 権限管理サービス
builder.Services.AddScoped<IPermissionService, PermissionService>();

// レビューワークフローサービス
builder.Services.AddScoped<IReviewWorkflowService, ReviewWorkflowService>();

// 認可ハンドラー
builder.Services.AddScoped<IAuthorizationHandler, SystemAdministratorRequirementHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ProjectAccessRequirementHandler>();

// Add WeatherApiClient with Aspire service discovery
// The service discovery will automatically resolve "apiservice" through the AppHost reference
builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler(); // Add resilience patterns for service calls

// Add logging for service discovery debugging
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // 開発環境では詳細なエラー情報を表示
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

// Map Aspire default endpoints first (health checks, etc.)
app.MapDefaultEndpoints();

// Map MVC controllers with specific routes to avoid conflicts
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Blazor components last to handle remaining routes
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
