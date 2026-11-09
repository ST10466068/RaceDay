using System.ComponentModel.DataAnnotations;
namespace RaceDay.Web.Models;

public class LoginVm
{
    [Required(ErrorMessage = "Please enter your email."), EmailAddress(ErrorMessage = "Please enter a valid email.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class RegisterVm
{
    [Required(ErrorMessage = "First name is required."), StringLength(50)]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required."), StringLength(50)]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Please enter a valid email.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required."), MinLength(8, ErrorMessage = "Password must be at least 8 characters."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password."), DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Please choose a role.")]
    public string Role { get; set; } = "";
}