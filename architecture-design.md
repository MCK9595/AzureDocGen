# Azure インフラ詳細設計書作成ツール アーキテクチャ設計書

## 1. 概要
このドキュメントは、Azure インフラ詳細設計書作成ツールのアーキテクチャ設計を定義します。.NET Aspireを使用したモノリシックアーキテクチャで構成され、Azureインフラ開発における詳細設計書を効率的に作成するWebアプリケーションを実現します。

## 2. アーキテクチャ概要

### 2.1 システム構成
- **アーキテクチャスタイル**: モノリシック（フロントエンド + バックエンド統合）
- **技術スタック**: C# .NET Aspire
- **フロントエンド**: Blazor Server または Blazor WebAssembly
- **データストア**: 
  - ローカル開発環境: SQL Server Container（RunAsContainer）
  - 本番環境: Azure SQL Database
- **実行環境**: Azure App Service（本番環境）、Aspire（ローカル開発環境）

### 2.2 ソリューション構造
```
AzureDocGen/
├── AzureDocGen.sln                          # ソリューションファイル
├── src/
│   ├── AzureDocGen.AppHost/                # Aspire AppHost プロジェクト
│   │   ├── AzureDocGen.AppHost.csproj
│   │   ├── Program.cs                       # オーケストレーション定義
│   │   └── appsettings.json
│   ├── AzureDocGen.ServiceDefaults/        # サービス共通設定
│   │   ├── AzureDocGen.ServiceDefaults.csproj
│   │   └── Extensions.cs                    # 共通拡張メソッド
│   ├── AzureDocGen.Web/                    # Webアプリケーション
│   │   ├── AzureDocGen.Web.csproj
│   │   ├── Program.cs
│   │   ├── Components/                      # Blazorコンポーネント
│   │   │   ├── Layout/
│   │   │   ├── Pages/
│   │   │   └── Shared/
│   │   ├── Services/                        # ビジネスロジック
│   │   ├── Models/                          # ドメインモデル
│   │   └── wwwroot/                         # 静的ファイル
│   └── AzureDocGen.Data/                   # データアクセス層
│       ├── AzureDocGen.Data.csproj
│       ├── Contexts/                        # EF Core DbContext
│       ├── Entities/                        # エンティティクラス
│       ├── Migrations/                      # データベースマイグレーション
│       └── Repositories/                    # リポジトリパターン実装
├── tests/
│   ├── AzureDocGen.UnitTests/
│   └── AzureDocGen.IntegrationTests/
├── docs/                                    # 設計ドキュメント
│   ├── architecture-design.md               # このファイル
│   └── requirements.md                      # 要件定義書
└── README.md
```

## 3. コンポーネント設計

### 3.1 AppHost（オーケストレーター）
.NET Aspireのオーケストレーターとして機能し、アプリケーションとその依存関係を管理します。

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// SQL Serverの追加（環境に応じて自動切り替え）
var sqlServer = builder.AddAzureSqlServer("azuresql")
    .RunAsContainer(); // ローカル開発時はコンテナで実行

var database = sqlServer.AddDatabase("azuredocgen");

// Webアプリケーションの追加
var web = builder.AddProject<Projects.AzureDocGen_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
```

### 3.2 ServiceDefaults（共通設定）
すべてのプロジェクトで共有される設定とサービスを提供します。

主な機能：
- OpenTelemetry（ロギング、メトリクス、トレーシング）
- ヘルスチェック
- サービスディスカバリー
- 共通のミドルウェア設定

### 3.3 Webアプリケーション（AzureDocGen.Web）

#### 3.3.1 レイヤー構成
1. **プレゼンテーション層（Blazorコンポーネント）**
   - ビジュアル設計インターフェース
   - フォーム入力UI
   - ダッシュボード
   - 管理画面

2. **ビジネスロジック層（Services）**
   - テンプレート管理サービス
   - プロジェクト管理サービス
   - 設計書生成サービス
   - ワークフロー管理サービス
   - 通知サービス
   - 監査サービス

3. **データアクセス層（Repositories）**
   - Entity Framework Coreを使用
   - リポジトリパターンの実装
   - Unit of Workパターンの実装

#### 3.3.2 主要コンポーネント

##### ビジュアル設計エディタ
- ドラッグ&ドロップによるリソース配置
- リアルタイム構成図生成
- 接続関係の視覚的管理

技術選定：
- Blazor Interactive Server（リアルタイム更新）
- JavaScript Interop（高度なUI操作）
- SVG.js または Fabric.js（図形描画）

##### テンプレート管理
- テンプレートの作成・編集・削除
- バージョン管理
- 共有設定管理

##### ワークフロー管理
- 承認フローの実装
- 状態管理（作成中、レビュー中、承認済み）
- 通知連携

### 3.4 データモデル設計

#### 3.4.1 主要エンティティ
```csharp
// プロジェクト
public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public List<Environment> Environments { get; set; }
    public List<NamingRule> NamingRules { get; set; }
}

// 環境
public class Environment
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } // 開発、検証、本番など
    public List<Resource> Resources { get; set; }
}

// リソース
public class Resource
{
    public Guid Id { get; set; }
    public Guid EnvironmentId { get; set; }
    public string ResourceType { get; set; } // App Service, SQL Database等
    public string Name { get; set; }
    public JsonDocument Properties { get; set; } // 動的プロパティ
    public List<ResourceConnection> Connections { get; set; }
    public Position VisualPosition { get; set; } // ビジュアルエディタ上の位置
}

// テンプレート
public class Template
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public JsonDocument Structure { get; set; } // テンプレート構造
    public List<TemplateParameter> Parameters { get; set; }
    public int Version { get; set; }
    public SharingLevel SharingLevel { get; set; }
}

// 設計書
public class DesignDocument
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; }
    public WorkflowStatus Status { get; set; }
    public List<DocumentVersion> Versions { get; set; }
}
```

## 4. 技術選定詳細

### 4.1 フロントエンド技術
- **Blazor Server**: リアルタイムな更新が必要なビジュアルエディタに最適
- **Bootstrap 5**: レスポンシブデザイン
- **JavaScript Interop**: 高度なUI操作（ドラッグ&ドロップ、SVG操作）

### 4.2 バックエンド技術
- **.NET 8/9**: 最新のC#言語機能とパフォーマンス
- **Entity Framework Core**: データアクセス
- **MediatR**: CQRSパターンの実装（オプション）
- **FluentValidation**: 入力検証

### 4.3 インフラストラクチャ

#### 4.3.1 データベース構成
- **ローカル開発環境**: 
  - SQL Server Container（.NET Aspire RunAsContainer）
  - Dockerコンテナとして自動起動
  - 開発専用の一時的なデータストレージ
  
- **本番環境**: 
  - Azure SQL Database
  - 高可用性、自動バックアップ
  - スケーラビリティとセキュリティの確保

#### 4.3.2 その他のAzureサービス
- **Azure App Service**: Webアプリケーションホスティング
- **Azure Blob Storage**: ファイル保存（エクスポートされた設計書等）
- **Application Insights**: 監視とロギング
- **Azure Key Vault**: 機密情報の管理

## 5. セキュリティ設計

### 5.1 認証・認可

#### 5.1.1 認証システム
- **ASP.NET Core Identity**: 基本認証システム
- **Azure Active Directory (AAD)**: エンタープライズ環境での統合認証（将来実装）

#### 5.1.2 階層的権限管理システム
新しい権限システムは3つのレベルで権限を管理します：

##### システムレベル権限
- **SystemAdministrator**: 全システムの管理権限
  - すべてのプロジェクト・環境への無制限アクセス
  - システム設定とユーザー管理
  - グローバルテンプレート管理

##### プロジェクトレベル権限
- **ProjectOwner**: プロジェクトの所有者
  - プロジェクトの削除権限
  - プロジェクトメンバーの管理（追加・削除・権限変更）
  - プロジェクト設定の変更
  - すべての環境への無制限アクセス

- **ProjectManager**: プロジェクトの日常管理者
  - プロジェクト設定の変更
  - テンプレート管理
  - 設計書の作成・編集・削除
  - ワークフロー管理

- **ProjectReviewer**: レビューと承認担当者
  - レビューワークフローでの承認・差し戻し権限
  - 設計書の参照権限
  - コメント・フィードバック権限

- **ProjectDeveloper**: 設計書作成担当者
  - 設計書の作成・編集権限
  - テンプレートの使用・作成
  - リソース配置とビジュアル設計

- **ProjectViewer**: 参照のみ
  - プロジェクト内容の参照権限のみ

##### 環境レベル権限
特定の環境（開発・検証・本番など）に対する細かい権限制御：

- **EnvironmentManager**: 環境管理者
  - 特定環境での無制限アクセス
  - 環境設定の変更

- **EnvironmentDeveloper**: 環境作業者
  - 特定環境での設計書作成・編集
  - リソース管理

- **EnvironmentViewer**: 環境参照者
  - 特定環境の参照権限のみ

#### 5.1.3 権限継承とオーバーライド
- **システム権限**: 全システムへのアクセスを提供
- **プロジェクト権限**: システム権限がない場合に適用
- **環境権限**: プロジェクト権限を特定環境で制限・拡張

#### 5.1.4 実装アーキテクチャ
```csharp
// 権限チェック例
public class PermissionService : IPermissionService
{
    // システム権限チェック
    public async Task<bool> HasSystemRoleAsync(string userId, SystemRoleType roleType);
    
    // プロジェクト権限チェック（継承考慮）
    public async Task<bool> HasProjectRoleOrHigherAsync(string userId, Guid projectId, ProjectRoleType minimumRole);
    
    // 環境権限チェック（継承考慮）
    public async Task<bool> HasEnvironmentRoleOrHigherAsync(string userId, Guid environmentId, EnvironmentRoleType minimumRole);
}

// カスタム認可ハンドラー
public class ProjectAccessRequirementHandler : AuthorizationHandler<ProjectAccessRequirement, Guid>
{
    // プロジェクト固有の認可処理
}
```

### 5.2 レビューワークフローシステム

#### 5.2.1 ワークフロー概要
- **プロジェクトベース**: レビューアーはプロジェクトごとに割り当て
- **1名承認制**: 複数のレビューアーがいても1名の承認で完了
- **状態管理**: Draft → InReview → Approved/Rejected/Cancelled

#### 5.2.2 ワークフロー実装
```csharp
public class ReviewWorkflowService : IReviewWorkflowService
{
    // ワークフロー作成
    public async Task<ReviewWorkflow> CreateWorkflowAsync(ReviewTargetType targetType, Guid targetId, Guid projectId, string title, string description, string createdBy);
    
    // レビューアー割り当て
    public async Task AssignReviewersAsync(Guid workflowId, List<string> reviewerIds, string assignedBy);
    
    // 承認・差し戻し
    public async Task<bool> ApproveReviewAsync(Guid workflowId, string reviewerId, string? comment = null);
    public async Task<bool> RejectReviewAsync(Guid workflowId, string reviewerId, string comment);
}
```

#### 5.2.3 履歴管理
- **WorkflowHistory**: すべての状態変化を記録
- **ReviewAssignment**: レビューアーごとの割り当て状況
- **通知システム**: 将来の拡張ポイント（現在はログ記録のみ）

### 5.3 データ保護
- **接続文字列の暗号化**: Azure Key Vault使用
- **機密情報のマスキング**: 出力時の自動マスク処理
- **監査ログ**: すべての操作を記録
- **権限履歴**: 権限変更の追跡

## 6. パフォーマンス設計

### 6.1 キャッシング戦略
- **In-Memory Cache**: 頻繁にアクセスされるテンプレート
- **Redis Cache**: セッション状態（将来的な拡張）

### 6.2 非同期処理
- **設計書生成**: バックグラウンドジョブとして実行
- **通知送信**: キューベースの非同期処理

## 7. 開発・デプロイメント

### 7.1 開発環境
- Visual Studio 2022 または Visual Studio Code
- .NET Aspire ワークロード
- Docker Desktop（ローカル開発用）

### 7.2 CI/CDパイプライン
- **GitHub Actions / Azure DevOps**:
  - ビルド
  - テスト実行
  - コード品質チェック
  - デプロイメント

### 7.3 環境構成

#### 7.3.1 開発環境
- **実行方式**: ローカル（.NET Aspire）
- **データベース**: SQL Server Container（自動起動）
- **認証**: ASP.NET Core Identity + Secret Manager
- **必要ツール**: Docker Desktop, .NET 8/9, Visual Studio/VS Code

#### 7.3.2 ステージング環境
- **ホスティング**: Azure App Service（Basic）
- **データベース**: Azure SQL Database（Basic）
- **認証**: Azure AD + Azure Key Vault
- **設定管理**: Azure App Configuration

#### 7.3.3 本番環境
- **ホスティング**: Azure App Service（Standard以上）
- **データベース**: Azure SQL Database（Standard以上）
- **認証**: Azure AD + Azure Key Vault
- **監視**: Application Insights
- **バックアップ**: 自動バックアップ有効

## 8. 監視・運用

### 8.1 監視
- **Application Insights**: APM
- **Azure Monitor**: インフラ監視
- **カスタムダッシュボード**: 業務メトリクス

### 8.2 バックアップ
- **データベース**: 自動バックアップ（日次）
- **Blob Storage**: geo冗長ストレージ

## 9. 今後の拡張性

### 9.1 マイクロサービス化への移行パス
現在のモノリシックアーキテクチャから、必要に応じて以下のサービスに分割可能：
- テンプレート管理サービス
- ワークフロー管理サービス
- 設計書生成サービス
- 通知サービス

### 9.2 IaCツール連携
- Terraform/Bicepエクスポート機能
- ARMテンプレート生成

### 9.3 AI/ML統合
- リソース配置の最適化提案
- 命名規則の自動提案

## 10. データベース実装詳細

### 10.1 .NET Aspire SQL Server統合

#### 10.1.1 AppHostプロジェクト設定
```csharp
// AzureDocGen.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Azure SQL Server統合
var sqlServer = builder.AddAzureSqlServer("azuresql")
    .RunAsContainer(); // ローカル開発時はコンテナとして実行

var database = sqlServer.AddDatabase("azuredocgen");

// Webプロジェクトへの参照
var web = builder.AddProject<Projects.AzureDocGen_Web>("web")
    .WithReference(database);

builder.Build().Run();
```

#### 10.1.2 Webプロジェクト設定
```csharp
// AzureDocGen.Web/Program.cs
var builder = WebApplication.CreateBuilder(args);

// .NET Aspire SQL Server Client統合
builder.AddSqlServerClient("azuredocgen");

// Entity Framework Coreの設定
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("azuredocgen");
    options.UseSqlServer(connectionString);
});

builder.AddServiceDefaults();

var app = builder.Build();
app.MapDefaultEndpoints();
```

#### 10.1.3 実行時の動作
- **ローカル開発**: SQL Server 2022コンテナが自動起動
- **本番環境**: Azure SQL Databaseに自動接続
- **接続文字列**: .NET Aspireが自動で管理・注入

### 10.2 Entity Framework Core設定
```csharp
// ApplicationDbContext.cs
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<DesignDocument> DesignDocuments { get; set; }
    // その他のエンティティ...
}
```

## 11. まとめ
このアーキテクチャ設計は、要件定義書に基づき、.NET Aspireのベストプラクティスに従って構成されています。特に、SQL Serverについては：

- **ローカル開発**: RunAsContainerによる自動コンテナ起動
- **本番環境**: Azure SQL Databaseによる高可用性・スケーラビリティ

この設計により、開発環境から本番環境まで一貫したデータベース体験を提供し、モノリシックアーキテクチャの利点を活かした迅速な開発と、将来的な拡張性を両立させています。