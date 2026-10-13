using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventRoute> EventRoutes => Set<EventRoute>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users: unique email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        // Events -> Organiser (no cascade, matches Part 1 SQL)
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser).WithMany(u => u.Events)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Categories and Routes are deleted with their event
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event).WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventRoute>()
            .HasOne(r => r.Event).WithMany(e => e.Routes)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enrolments: one entry per participant per category
        modelBuilder.Entity<Enrolment>()
            .HasIndex(e => new { e.ParticipantId, e.CategoryId }).IsUnique();

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Participant).WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Category).WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Results: exactly one result per enrolment
        modelBuilder.Entity<Result>()
            .HasIndex(r => r.EnrolmentId).IsUnique();

        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment).WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Result>()
            .HasOne(r => r.CapturedBy).WithMany()
            .HasForeignKey(r => r.CapturedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Money and distance precision
        modelBuilder.Entity<Event>().Property(e => e.DistanceKm).HasPrecision(6, 2);
        modelBuilder.Entity<Category>().Property(c => c.DistanceKm).HasPrecision(6, 2);
        modelBuilder.Entity<Category>().Property(c => c.EntryFee).HasPrecision(8, 2);
        modelBuilder.Entity<EventRoute>().Property(r => r.DistanceKm).HasPrecision(6, 2);
    }
}