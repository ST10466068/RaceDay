

using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
namespace RaceDay.Api.Data;

public class RaceDayContext(DbContextOptions<RaceDayContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
        b.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" });

        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<User>().HasOne(u => u.Role).WithMany().HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Event>().HasOne(e => e.Organiser).WithMany().HasForeignKey(e => e.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Category>().Property(c => c.DistanceKm).HasPrecision(5, 2);
        b.Entity<Category>().Property(c => c.Fee).HasPrecision(8, 2);

        b.Entity<Enrolment>().HasIndex(e => new { e.CategoryId, e.ParticipantId }).IsUnique();
        b.Entity<Enrolment>().HasOne(e => e.Participant).WithMany().HasForeignKey(e => e.ParticipantId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Result>().HasIndex(r => r.EnrolmentId).IsUnique();
    }
}