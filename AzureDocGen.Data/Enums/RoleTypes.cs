namespace AzureDocGen.Data.Enums;

/// <summary>
/// システムレベルのロールタイプ
/// </summary>
public enum SystemRoleType
{
    /// <summary>
    /// システム管理者: システム全体の管理権限
    /// </summary>
    SystemAdministrator,
    
    /// <summary>
    /// グローバル閲覧者: すべてのプロジェクトの閲覧権限（監査目的）
    /// </summary>
    GlobalViewer
}

/// <summary>
/// プロジェクトレベルのロールタイプ
/// </summary>
public enum ProjectRoleType
{
    /// <summary>
    /// プロジェクト所有者: プロジェクトの最終責任者
    /// </summary>
    ProjectOwner,
    
    /// <summary>
    /// プロジェクト管理者: 日常的な管理業務担当者
    /// </summary>
    ProjectManager,
    
    /// <summary>
    /// プロジェクトレビューアー: レビュー専門権限
    /// </summary>
    ProjectReviewer,
    
    /// <summary>
    /// プロジェクト開発者: 開発作業権限
    /// </summary>
    ProjectDeveloper,
    
    /// <summary>
    /// プロジェクト閲覧者: 閲覧のみ
    /// </summary>
    ProjectViewer
}

/// <summary>
/// 環境レベルのロールタイプ
/// </summary>
public enum EnvironmentRoleType
{
    /// <summary>
    /// 環境管理者: 特定環境の管理者
    /// </summary>
    EnvironmentManager,
    
    /// <summary>
    /// 環境開発者: 特定環境での開発権限
    /// </summary>
    EnvironmentDeveloper,
    
    /// <summary>
    /// 環境閲覧者: 特定環境の閲覧権限
    /// </summary>
    EnvironmentViewer
}

/// <summary>
/// レビューワークフローの状態
/// </summary>
public enum ReviewWorkflowStatus
{
    /// <summary>
    /// 下書き
    /// </summary>
    Draft,
    
    /// <summary>
    /// レビュー中
    /// </summary>
    InReview,
    
    /// <summary>
    /// 承認済み
    /// </summary>
    Approved,
    
    /// <summary>
    /// 差し戻し
    /// </summary>
    Rejected,
    
    /// <summary>
    /// キャンセル
    /// </summary>
    Cancelled
}

/// <summary>
/// レビュー割り当ての状態
/// </summary>
public enum ReviewAssignmentStatus
{
    /// <summary>
    /// レビュー待ち
    /// </summary>
    Pending,
    
    /// <summary>
    /// 承認
    /// </summary>
    Approved,
    
    /// <summary>
    /// 差し戻し
    /// </summary>
    Rejected,
    
    /// <summary>
    /// スキップ（他のレビューアーが承認したため不要）
    /// </summary>
    Skipped
}

/// <summary>
/// レビュー対象のタイプ
/// </summary>
public enum ReviewTargetType
{
    /// <summary>
    /// 設計書
    /// </summary>
    DesignDocument,
    
    /// <summary>
    /// テンプレート
    /// </summary>
    Template
}