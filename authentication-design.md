# Azure インフラ詳細設計書作成ツール 認証機能設計書

## 1. 概要
本設計書は、Azure インフラ詳細設計書作成ツールにおけるユーザー認証機能の設計を定義します。セキュリティリスクを最小化するため、C#/.NETの標準的な認証フレームワークを使用し、独自の認証実装は行いません。

## 2. 認証方式の選定

### 2.1 採用する認証フレームワーク
**ASP.NET Core Identity** と **Azure Active Directory (Azure AD)** の併用

#### 選定理由
1. **ASP.NET Core Identity**
   - Microsoftが提供する標準的な認証・認可フレームワーク
   - セキュリティのベストプラクティスが実装済み
   - パスワードハッシュ、アカウントロックアウト、二要素認証等の機能を標準提供
   - Entity Framework Coreとの統合が容易

2. **Azure Active Directory (Azure AD)**
   - エンタープライズ環境でのシングルサインオン（SSO）
   - 既存の組織アカウントとの連携
   - OAuth 2.0 / OpenID Connectの標準プロトコル使用
   - 条件付きアクセスポリシーによる高度なセキュリティ

### 2.2 認証フロー
```
開発環境: ASP.NET Core Identity（ローカルアカウント）+ Secret Manager Tool
本番環境: Azure AD（組織アカウント）+ Azure Key Vault
```

### 2.3 セキュリティ原則
Microsoft Docsのベストプラクティスに基づく実装：
1. **パスワードや機密情報をソースコードに含めない**
2. **開発環境ではSecret Manager Toolを使用**
3. **本番環境ではAzure Key VaultまたはManaged Identityを使用**
4. **JWTトークンはサーバー側で管理し、HttpOnly Cookieで送信**
5. **環境変数での機密情報の保存は避ける**

## 3. 実装設計

### 3.1 ASP.NET Core Identity の設定

#### 3.1.1 Program.cs の設定
```csharp
// Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
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
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie認証の設定
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
```

#### 3.1.2 Secret Manager設定（開発環境）
```bash
# プロジェクトディレクトリで実行
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
```

#### 3.1.3 Azure Key Vault設定（本番環境）
```csharp
// Program.cs - Azure Key Vault統合
if (!builder.Environment.IsDevelopment())
{
    var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri"));
    builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
}
```

#### 3.1.4 ユーザーモデル
```csharp
// Models/ApplicationUser.cs
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Department { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // ナビゲーションプロパティ
    public virtual ICollection<Project> Projects { get; set; }
    public virtual ICollection<AuditLog> AuditLogs { get; set; }
}

// Models/ApplicationRole.cs
public class ApplicationRole : IdentityRole
{
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3.2 Azure AD 統合

#### 3.2.1 Azure AD 認証の設定
```csharp
// Program.cs - Azure AD認証の追加
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        // 本番環境ではAzure Key Vaultから取得
        if (!builder.Environment.IsDevelopment())
        {
            options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
        }
    });

builder.Services.AddAuthorization(options =>
{
    // ハイブリッド認証ポリシー
    options.AddPolicy("RequireAuthenticatedUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
    
    // 管理者ポリシー
    options.AddPolicy("RequireAdministrator", policy =>
    {
        policy.RequireRole("Administrator", "全体管理者");
    });
    
    // プロジェクト管理者ポリシー
    options.AddPolicy("RequireProjectManager", policy =>
    {
        policy.RequireRole("ProjectManager", "プロジェクト管理者", "Administrator", "全体管理者");
    });
});

// appsettings.json (ClientSecretは含めない)
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourdomain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

### 3.3 ロールベースアクセス制御（RBAC）

#### 3.3.1 ロール定義
```csharp
public static class ApplicationRoles
{
    public const string Administrator = "Administrator";           // 全体管理者
    public const string ProjectManager = "ProjectManager";         // プロジェクト管理者
    public const string Developer = "Developer";                   // 一般ユーザー（開発者）
    public const string Viewer = "Viewer";                        // 閲覧のみ
}
```

#### 3.3.2 ロール別権限マトリックス
| 機能 | 全体管理者 | プロジェクト管理者 | 一般ユーザー | 閲覧者 |
|------|-----------|------------------|------------|--------|
| ユーザー管理 | ✓ | - | - | - |
| プロジェクト作成 | ✓ | ✓ | - | - |
| プロジェクト設定変更 | ✓ | ✓（自プロジェクトのみ） | - | - |
| テンプレート作成 | ✓ | ✓ | ✓ | - |
| テンプレート共有設定 | ✓ | ✓（プロジェクト内） | - | - |
| 設計書作成・編集 | ✓ | ✓ | ✓ | - |
| 設計書承認 | ✓ | ✓ | ✓（レビューアー指定時） | - |
| 設計書閲覧 | ✓ | ✓ | ✓ | ✓ |
| 監査ログ閲覧 | ✓ | ✓（自プロジェクトのみ） | - | - |

### 3.4 認証フロー実装

#### 3.4.1 ログインページ
```razor
@page "/Account/Login"
@model LoginModel
@using Microsoft.AspNetCore.Authentication

<div class="row">
    <div class="col-md-6">
        <h4>ローカルアカウントでログイン</h4>
        <form method="post">
            <div asp-validation-summary="All" class="text-danger"></div>
            <div class="form-group">
                <label asp-for="Input.Email"></label>
                <input asp-for="Input.Email" class="form-control" />
                <span asp-validation-for="Input.Email" class="text-danger"></span>
            </div>
            <div class="form-group">
                <label asp-for="Input.Password"></label>
                <input asp-for="Input.Password" class="form-control" />
                <span asp-validation-for="Input.Password" class="text-danger"></span>
            </div>
            <div class="form-group">
                <div class="checkbox">
                    <label asp-for="Input.RememberMe">
                        <input asp-for="Input.RememberMe" />
                        @Html.DisplayNameFor(m => m.Input.RememberMe)
                    </label>
                </div>
            </div>
            <button type="submit" class="btn btn-primary">ログイン</button>
        </form>
    </div>
    
    <div class="col-md-6">
        <h4>組織アカウントでログイン</h4>
        <form asp-action="ExternalLogin" asp-controller="Account" method="post">
            <button type="submit" class="btn btn-primary" name="provider" value="AzureAD">
                Azure ADでログイン
            </button>
        </form>
    </div>
</div>
```

#### 3.4.2 コントローラー実装
```csharp
[Authorize]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            // アカウントロックアウトを考慮したサインイン
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, 
                model.Password, 
                model.RememberMe, 
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // 監査ログの記録
                await _auditService.LogAsync(new AuditLog
                {
                    UserId = user.Id,
                    Action = "Login",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });

                return LocalRedirect(returnUrl ?? "/");
            }
            
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            
            ModelState.AddModelError(string.Empty, "ログインに失敗しました。");
        }

        return View(model);
    }

    [HttpPost]
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", 
            new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(
            provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }
}
```

### 3.5 セキュリティ対策

#### 3.5.1 機密情報の保護
```csharp
// 開発環境での設定例
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 開発環境ではSecret Managerから取得
        if (_env.IsDevelopment())
        {
            var secretValue = Configuration["MySecret"];
        }
        // 本番環境ではAzure Key Vaultから取得
        else
        {
            // Azure Key Vaultの設定は既にProgram.csで実施済み
            var secretValue = Configuration["MySecret"];
        }
    }
}
```

#### 3.5.2 二要素認証（2FA）}
```csharp
// 二要素認証の有効化
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
});

// TOTP (Time-based One-Time Password) の実装
public async Task<IActionResult> EnableAuthenticator()
{
    var user = await _userManager.GetUserAsync(User);
    var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
    
    if (string.IsNullOrEmpty(unformattedKey))
    {
        await _userManager.ResetAuthenticatorKeyAsync(user);
        unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
    }

    var model = new EnableAuthenticatorViewModel
    {
        SharedKey = FormatKey(unformattedKey),
        AuthenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey)
    };

    return View(model);
}
```

#### 3.5.3 セッション管理
```csharp
// セッションのセキュリティ設定
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// 同時ログイン制限
public class ConcurrentLoginMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity.IsAuthenticated)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sessionId = context.Session.Id;
            
            // Redisやメモリキャッシュでセッション管理
            if (!await IsValidSession(userId, sessionId))
            {
                await context.SignOutAsync();
                context.Response.Redirect("/Account/Login");
                return;
            }
        }
        
        await _next(context);
    }
}
```

### 3.6 監査とロギング

#### 3.6.1 認証イベントの監査
```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public bool Success { get; set; }
    public string Details { get; set; }
}

// 監査サービス
public interface IAuditService
{
    Task LogLoginAsync(string userId, bool success, string ipAddress);
    Task LogLogoutAsync(string userId);
    Task LogPasswordChangeAsync(string userId);
    Task LogRoleChangeAsync(string userId, string changedBy, string newRole);
}
```

## 4. エラーハンドリング

### 4.1 認証エラーの処理
```csharp
public class AuthenticationErrorHandler
{
    private readonly ILogger<AuthenticationErrorHandler> _logger;

    public async Task HandleAuthenticationError(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "認証エラーが発生しました");

        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Authentication failed",
            message = "認証に失敗しました。再度ログインしてください。"
        });
    }
}
```

## 5. テスト戦略

### 5.1 単体テスト
```csharp
[Fact]
public async Task Login_ValidCredentials_ReturnsSuccess()
{
    // Arrange
    var userManager = GetMockUserManager();
    var signInManager = GetMockSignInManager(userManager);
    var controller = new AccountController(signInManager, userManager);

    // Act
    var result = await controller.Login(new LoginViewModel
    {
        Email = "test@example.com",
        Password = "Test123!@#"
    });

    // Assert
    Assert.IsType<RedirectResult>(result);
}

[Fact]
public async Task Login_InvalidCredentials_ReturnsError()
{
    // テスト実装
}
```

### 5.2 結合テスト
```csharp
public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task AuthenticatedUser_CanAccessProtectedResource()
    {
        // DistributedApplicationTestingBuilderを使用した結合テスト
    }
}
```

## 6. Microsoft Docsベストプラクティスの実装

### 6.1 セキュリティガイドライン遵守項目
Microsoft Docsの推奨事項に基づく実装：

1. **機密情報の安全な管理**
   - 開発環境：Secret Manager Tool使用
   - 本番環境：Azure Key Vault使用
   - ソースコードに機密情報を含めない

2. **認証フローの標準化**
   - OpenID Connect / OAuth 2.0 標準プロトコル使用
   - Managed Identity優先（Azureサービス連携時）
   - Resource Owner Password Credentials Grantは使用しない

3. **Cookie セキュリティ**
   - HttpOnly = true
   - Secure = true（HTTPS必須）
   - SameSite = Strict

4. **セッション管理**
   - 短いタイムアウト設定
   - 同時ログイン制限

### 6.2 セキュリティチェックリスト
- [ ] appsettings.jsonに機密情報が含まれていない
- [ ] Secret Manager Toolの初期化完了
- [ ] Azure Key Vaultの設定完了
- [ ] HTTPS強制設定
- [ ] Cookie セキュリティ設定
- [ ] 二要素認証の実装
- [ ] セッション管理の実装
- [ ] 監査ログの実装

## 7. まとめ
本設計では、Microsoft Docsのセキュリティベストプラクティスに完全準拠し、ASP.NET Core IdentityとAzure ADを使用した堅牢な認証システムを実現します。独自実装を避け、Microsoftが提供する標準的なフレームワークと推奨セキュリティ手法を使用することで、セキュリティリスクを最小化しながら、エンタープライズレベルの認証機能を提供します。