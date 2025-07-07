using System.ComponentModel.DataAnnotations;

namespace AzureDocGen.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "メールアドレスは必須です")]
    [EmailAddress(ErrorMessage = "有効なメールアドレスを入力してください")]
    [Display(Name = "メールアドレス")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "パスワードは必須です")]
    [DataType(DataType.Password)]
    [Display(Name = "パスワード")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "ログイン状態を保持する")]
    public bool RememberMe { get; set; }
}