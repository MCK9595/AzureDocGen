using System.ComponentModel.DataAnnotations;

namespace AzureDocGen.Web.Models;

public class ProfileViewModel
{
    [Display(Name = "メールアドレス")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "名前は必須です")]
    [Display(Name = "名前")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "苗字は必須です")]
    [Display(Name = "苗字")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "部署")]
    [StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Display(Name = "登録日時")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "最終ログイン")]
    public DateTime? LastLoginAt { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "現在のパスワードは必須です")]
    [DataType(DataType.Password)]
    [Display(Name = "現在のパスワード")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "新しいパスワードは必須です")]
    [StringLength(100, ErrorMessage = "{0}は{2}文字以上{1}文字以下で入力してください", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "新しいパスワード")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "新しいパスワード確認")]
    [Compare("NewPassword", ErrorMessage = "パスワードと確認用パスワードが一致しません")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class DeleteAccountViewModel
{
    [Display(Name = "メールアドレス")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "名前")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "苗字")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "パスワードは必須です")]
    [DataType(DataType.Password)]
    [Display(Name = "パスワード")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "確認が必要です")]
    [Compare("ConfirmationText", ErrorMessage = "確認テキストが一致しません")]
    [Display(Name = "確認")]
    public string Confirmation { get; set; } = string.Empty;

    public string ConfirmationText => "アカウントを削除";
}