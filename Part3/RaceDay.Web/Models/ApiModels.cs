using System.ComponentModel.DataAnnotations;
namespace RaceDay.Web.Models;

public class CategoryVm
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public decimal Fee { get; set; }
}

public class EventVm
{
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string EventType { get; set; } = "";
    public decimal? DistanceKm { get; set; }
    public string? BannerUrl { get; set; }
    public int OrganiserId { get; set; }
    public List<CategoryVm> Categories { get; set; } = new();
}

public class EnrolmentVm
{
    public int EnrolmentId { get; set; }
    public string Status { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
}

public class ResultVm
{
    public int ResultId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int? Position { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public int Finishers { get; set; }
}

public class ProfileVm
{
    [Required(ErrorMessage = "First name is required."), StringLength(50)]
    public string FirstName { get; set; } = "";
    [Required(ErrorMessage = "Last name is required."), StringLength(50)]
    public string LastName { get; set; } = "";
    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Please enter a valid email.")]
    public string Email { get; set; } = "";
    public string? Role { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class EventFormVm
{
    public int EventId { get; set; }
    [Required(ErrorMessage = "Event name is required."), StringLength(100)]
    public string Name { get; set; } = "";
    [StringLength(500)]
    public string? Description { get; set; }
    [Required(ErrorMessage = "Event date is required.")]
    public DateTime EventDate { get; set; }
    [Required(ErrorMessage = "Location is required."), StringLength(150)]
    public string Location { get; set; } = "";
    [Required(ErrorMessage = "Choose an event type.")]
    public string EventType { get; set; } = "Run";
    [Required(ErrorMessage = "Distance is required."), Range(0.01, 999.99, ErrorMessage = "Enter a distance between 0.01 and 999.99 km.")]
    public decimal? DistanceKm { get; set; }
    public string? BannerUrl { get; set; }
    public IFormFile? Banner { get; set; }
}

public class CategoryFormVm
{
    [Required(ErrorMessage = "Category name is required."), StringLength(50)]
    public string Name { get; set; } = "";
    [Range(0.01, 999.99, ErrorMessage = "Enter a valid distance.")]
    public decimal DistanceKm { get; set; }
    [Range(0, 99999, ErrorMessage = "Enter a valid fee.")]
    public decimal Fee { get; set; }
}