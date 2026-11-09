using System.ComponentModel.DataAnnotations;
namespace RaceDay.Api.Models;

public class Role
{
    public int RoleId { get; set; }
    [MaxLength(20)] public string RoleName { get; set; } = "";
}

public class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    [MaxLength(50)] public string FirstName { get; set; } = "";
    [MaxLength(50)] public string LastName { get; set; } = "";
    [MaxLength(100)] public string Email { get; set; } = "";
    [MaxLength(255)] public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public User Organiser { get; set; } = null!;
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(500)] public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    [MaxLength(150)] public string Location { get; set; } = "";
    [MaxLength(10)] public string EventType { get; set; } = "Run";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Category> Categories { get; set; } = new();
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    [MaxLength(50)] public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public decimal Fee { get; set; }
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int ParticipantId { get; set; }
    public User Participant { get; set; } = null!;
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    [MaxLength(20)] public string Status { get; set; } = "Confirmed";
}

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
    public TimeSpan FinishTime { get; set; }
    public int? Position { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}