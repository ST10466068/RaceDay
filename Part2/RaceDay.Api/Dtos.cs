using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
namespace RaceDay.Api;

public record RegisterDto(
    [Required, MaxLength(50)] string FirstName,
    [Required, MaxLength(50)] string LastName,
    [Required, EmailAddress, MaxLength(100)] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string Role);

public record LoginDto([Required] string Email, [Required] string Password);

public record ProfileDto(
    [Required, MaxLength(50)] string FirstName,
    [Required, MaxLength(50)] string LastName,
    [Required, EmailAddress, MaxLength(100)] string Email);

public record EventDto(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description,
    [Required] DateTime EventDate,
    [Required, MaxLength(150)] string Location,
    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")] string EventType);

public record CategoryDto(
    [Required, MaxLength(50)] string Name,
    [Range(0.01, 999.99)] decimal DistanceKm,
    [Range(0, 99999)] decimal Fee);

public record ResultDto(
    [Required] TimeSpan FinishTime,
    [Range(1, int.MaxValue)] int? Position);

public static class ClaimsExtensions
{
    public static int UserId(this ClaimsPrincipal u) => int.Parse(u.FindFirst("uid")!.Value);
}