using System.ComponentModel.DataAnnotations;

namespace AzureDocGen.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "メールアドレスは必須です")]
    [EmailAddress(ErrorMessage = "有効なメールアドレスを入力してください")]
    [Display(Name = "メールアドレス")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "パスワードは必須です")]
    [StringLength(100, ErrorMessage = "{0}は{2}文字以上{1}文字以下で入力してください", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "パスワード")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "パスワード確認")]
    [Compare("Password", ErrorMessage = "パスワードと確認用パスワードが一致しません")]
    public string ConfirmPassword { get; set; } = string.Empty;

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
}