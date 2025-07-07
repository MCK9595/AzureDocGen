# Azure インフラ詳細設計書作成ツール 実装計画書

## 目次
1. [現在の実装状況](#現在の実装状況)
2. [未実装機能一覧](#未実装機能一覧)
3. [実装フェーズ計画](#実装フェーズ計画)
4. [技術実装アプローチ](#技術実装アプローチ)
5. [推奨開発スケジュール](#推奨開発スケジュール)

## 現在の実装状況

### ✅ 実装済み機能

#### 認証・認可システム
- ASP.NET Core Identity による基本認証
- 階層的権限管理システム
  - システムレベル権限（SystemAdministrator, GlobalViewer）
  - プロジェクトレベル権限（Owner, Manager, Reviewer, Developer, Viewer）
  - 環境レベル権限（Manager, Developer, Viewer）
- カスタム認可ハンドラー実装
- ログイン・ログアウト機能

#### データモデル層
- 全エンティティクラス実装
  - Project, Environment, Resource
  - Template, TemplateParameter
  - DesignDocument, DocumentVersion
  - ReviewWorkflow, ReviewAssignment, WorkflowHistory
  - SystemRole, ProjectUserRole, EnvironmentUserRole
  - NamingRule, AuditLog

#### サービス層
- PermissionService（権限チェックサービス）
- ReviewWorkflowService（レビューワークフロー管理）
- 基本的なCRUD操作

#### 管理機能
- AdminController（ユーザー管理、ロール管理）
- ユーザー一覧表示
- ユーザー詳細・ロール編集

#### インフラストラクチャ
- .NET Aspire統合
  - AppHost（オーケストレーション）
  - ServiceDefaults（共通設定）
  - MigrationService（DB初期化）
- SQL Server（コンテナ/Azure SQL）
- Entity Framework Core設定

## 未実装機能一覧

### 🔥 最優先機能
1. **プロジェクト・環境管理 UI**
2. **テンプレート管理システム**
3. **ビジュアル設計インターフェース**
4. **設計書作成・編集機能**

### ⚡ 高優先度機能
5. **レビューワークフロー UI**
6. **出力機能（Excel/PDF/Markdown）**
7. **命名規則管理**

### ⭐ 中優先度機能
8. **通知機能（アプリ内）**
9. **監査・履歴管理の詳細機能**
10. **データエクスポート・インポート**

## 実装フェーズ計画

### Phase 1: コア機能基盤（2-3週間）
**目標**: プロジェクトとテンプレートの基本管理機能を構築

#### 1.1 プロジェクト・環境管理
- **コンポーネント**:
  - `ProjectListPage.razor` - プロジェクト一覧
  - `ProjectDetailsPage.razor` - プロジェクト詳細・編集
  - `EnvironmentManager.razor` - 環境管理コンポーネント
  - `ProjectMemberManager.razor` - メンバー管理

- **コントローラー**:
  - `ProjectController.cs` - プロジェクトCRUD API
  - `EnvironmentController.cs` - 環境管理 API

- **サービス**:
  - `IProjectService.cs` / `ProjectService.cs`
  - `IEnvironmentService.cs` / `EnvironmentService.cs`

#### 1.2 テンプレート管理
- **コンポーネント**:
  - `TemplateListPage.razor` - テンプレート一覧
  - `TemplateEditor.razor` - テンプレート作成・編集
  - `ParameterDefiner.razor` - パラメーター定義
  - `TemplateVersionHistory.razor` - バージョン管理

- **サービス**:
  - `ITemplateService.cs` / `TemplateService.cs`
  - `ITemplateVersionService.cs` / `TemplateVersionService.cs`

### Phase 2: ビジュアル設計機能（4-6週間）
**目標**: ドラッグ&ドロップによる直感的な設計インターフェース

#### 2.1 ビジュアルデザイナー基盤
- **技術選定**:
  - Blazor InteractiveServer（リアルタイム更新）
  - JavaScript Interop
  - SVG.js または Fabric.js（描画ライブラリ）

- **コンポーネント**:
  - `VisualDesigner.razor` - メインデザイナー
  - `ResourcePalette.razor` - リソースパレット
  - `DesignCanvas.razor` - 描画キャンバス
  - `PropertyPanel.razor` - プロパティ編集パネル

#### 2.2 リソース管理
- **機能**:
  - Azureリソースアイコンライブラリ
  - ドラッグ&ドロップ機能
  - リソース間接続設定
  - 自動配置・グリッドスナップ

#### 2.3 設計書作成
- **コンポーネント**:
  - `DesignDocumentEditor.razor` - 設計書エディター
  - `ParameterForm.razor` - パラメーター入力フォーム
  - `ValidationSummary.razor` - バリデーション結果表示

### Phase 3: ワークフロー・出力機能（3-4週間）
**目標**: レビュープロセスと各種フォーマットでの出力

#### 3.1 レビューワークフロー UI
- **コンポーネント**:
  - `WorkflowDashboard.razor` - ワークフロー概要
  - `ReviewInterface.razor` - レビュー画面
  - `ApprovalHistory.razor` - 承認履歴
  - `WorkflowNotifications.razor` - 通知表示

#### 3.2 出力機能
- **技術スタック**:
  - ClosedXML（Excel出力）
  - iTextSharp（PDF出力）
  - カスタムMarkdownジェネレーター

- **サービス**:
  - `IDocumentExportService.cs` / `DocumentExportService.cs`
  - `IConfigurationMaskService.cs` / `ConfigurationMaskService.cs`

### Phase 4: 管理・運用機能（2-3週間）
**目標**: 運用に必要な管理機能の実装

#### 4.1 命名規則管理
- **コンポーネント**:
  - `NamingRuleEditor.razor` - 命名規則エディター
  - `NamingPreview.razor` - 命名プレビュー
  - `NamingValidation.razor` - 命名検証

#### 4.2 監査・履歴管理
- **コンポーネント**:
  - `AuditLogViewer.razor` - 監査ログビューアー
  - `HistoryTimeline.razor` - 変更履歴タイムライン
  - `DiffViewer.razor` - 差分表示

## 技術実装アプローチ

### ビジュアル設計機能の段階的実装

#### Stage 1: 静的配置（Week 5-6）
- 基本的なSVGレンダリング
- リソースアイコンの表示
- 静的な配置機能

#### Stage 2: インタラクティブ機能（Week 7）
- ドラッグ&ドロップ実装
- リアルタイム位置更新
- 選択・削除機能

#### Stage 3: 接続機能（Week 8）
- リソース間の接続線描画
- 接続ポイントの自動計算
- 接続バリデーション

#### Stage 4: 高度な機能（Week 9）
- 自動配置アルゴリズム
- グループ化機能
- ズーム・パン機能

### JavaScript Interop 実装例
```csharp
// VisualDesigner.razor.cs
[JSInvokable]
public async Task OnResourceDropped(string resourceType, double x, double y)
{
    var resource = new Resource
    {
        ResourceType = resourceType,
        VisualPosition = new Position { X = x, Y = y }
    };
    
    await ResourceService.AddResourceAsync(resource);
    await InvokeAsync(StateHasChanged);
}
```

## 推奨開発スケジュール

### 16週間開発計画

| 週 | 実装内容 | 成果物 |
|---|---------|--------|
| 1-2 | プロジェクト管理UI | プロジェクトCRUD機能 |
| 3-4 | テンプレート管理UI | テンプレート作成・バージョン管理 |
| 5-6 | ビジュアルデザイナー基盤 | 静的リソース配置 |
| 7-8 | インタラクティブ機能 | ドラッグ&ドロップ、接続機能 |
| 9-10 | 設計書作成機能 | パラメーター入力、バリデーション |
| 11-12 | ワークフロー機能 | レビュー・承認プロセス |
| 13-14 | 出力機能 | Excel/PDF/Markdown出力 |
| 15-16 | 管理機能・最終調整 | 命名規則、監査機能 |

### マイルストーン
1. **M1 (Week 4)**: 基本的なプロジェクト・テンプレート管理完成
2. **M2 (Week 8)**: ビジュアル設計機能のアルファ版完成
3. **M3 (Week 12)**: コア機能完成（設計書作成まで）
4. **M4 (Week 16)**: 全機能実装完了

## 次のアクション

### 即座に着手すべきタスク
1. `ProjectController.cs` の実装
2. `ProjectListPage.razor` の作成
3. `IProjectService.cs` インターフェース定義
4. プロジェクト作成フォームの実装

### 準備作業
1. Blazorページ構造の設計
2. ルーティング設定
3. ナビゲーションメニューの更新
4. 権限チェックの統合

## 技術的な注意事項

### Blazor InteractiveServer 使用時の考慮点
- SignalR接続の管理
- 大量データ処理時のパフォーマンス
- オフライン対応の検討

### セキュリティ考慮事項
- すべてのAPIエンドポイントで権限チェック
- XSS対策（特にビジュアルデザイナー）
- SQLインジェクション対策（EF Core使用）

### パフォーマンス最適化
- 遅延読み込みの活用
- キャッシング戦略
- 非同期処理の徹底