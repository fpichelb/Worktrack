using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Worktrack.Models;
using Worktrack.Services;

namespace Worktrack.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<TimeEntry> TimeEntry { get; set; }

    public DbSet<Season> Seasons {get; set; }

    public DbSet<ImportBatch> Imports { get; set; }
    public DbSet<PrivacyPolicy> PrivacyPolicies { get; set; }
    public DbSet<Impressum> ImpressumData { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivityRegistration> ActivityRegistrations { get; set; }
    public DbSet<NewsItem> NewsItems { get; set; }
    public DbSet<ActivityGroup> ActivityGroups { get; set; }
    public DbSet<ActivityGroupLink> ActivityGroupLinks { get; set; }
    public DbSet<TrainingRoom> TrainingRooms { get; set; }
    public DbSet<TrainingEvent> TrainingEvents { get; set; }
    public DbSet<TrainingEventParticipant> TrainingEventParticipants { get; set; }
    public DbSet<TrainingSeries> TrainingSeries { get; set; }
    public DbSet<TrainingSeriesException> TrainingSeriesExceptions { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
         base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<ActivityGroupLink>()
        .HasKey(x => new { x.ActivityId, x.GroupId });

    modelBuilder.Entity<ActivityGroupLink>()
        .HasOne(x => x.Activity)
        .WithMany(a => a.GroupLinks)
        .HasForeignKey(x => x.ActivityId);

    modelBuilder.Entity<ActivityGroupLink>()
        .HasOne(x => x.Group)
        .WithMany(g => g.ActivityLinks)
        .HasForeignKey(x => x.GroupId);

    modelBuilder.Entity<TrainingRoom>()
        .Property(x => x.Name)
        .HasMaxLength(120);

    modelBuilder.Entity<TrainingEvent>()
        .Property(x => x.Title)
        .HasMaxLength(180);

    modelBuilder.Entity<TrainingSeries>()
        .Property(x => x.Title)
        .HasMaxLength(180);

    modelBuilder.Entity<TrainingSeries>()
        .Property(x => x.RecurrencePattern)
        .HasMaxLength(20);

    modelBuilder.Entity<TrainingEventParticipant>()
        .Property(x => x.DisplayName)
        .HasMaxLength(120);

    modelBuilder.Entity<TrainingEventParticipant>()
        .Property(x => x.ParticipantType)
        .HasMaxLength(40);

    modelBuilder.Entity<TrainingEventParticipant>()
        .Property(x => x.SourceGroupName)
        .HasMaxLength(120);

    modelBuilder.Entity<TrainingSeriesException>()
        .Property(x => x.Reason)
        .HasMaxLength(240);

    modelBuilder.Entity<TrainingEvent>()
        .HasOne(x => x.TrainingRoom)
        .WithMany(x => x.Events)
        .HasForeignKey(x => x.TrainingRoomId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<TrainingSeries>()
        .HasOne(x => x.TrainingRoom)
        .WithMany(x => x.Series)
        .HasForeignKey(x => x.TrainingRoomId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<TrainingEvent>()
        .HasOne(x => x.TrainingSeries)
        .WithMany(x => x.MaterializedEvents)
        .HasForeignKey(x => x.TrainingSeriesId)
        .OnDelete(DeleteBehavior.SetNull);

    modelBuilder.Entity<TrainingEventParticipant>()
        .HasOne(x => x.TrainingEvent)
        .WithMany(x => x.Participants)
        .HasForeignKey(x => x.TrainingEventId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<TrainingEventParticipant>()
        .HasOne(x => x.User)
        .WithMany()
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.SetNull);

    modelBuilder.Entity<TrainingSeriesException>()
        .HasOne(x => x.TrainingSeries)
        .WithMany(x => x.Exceptions)
        .HasForeignKey(x => x.TrainingSeriesId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<TrainingSeriesException>()
        .HasIndex(x => new { x.TrainingSeriesId, x.OccurrenceDate })
        .IsUnique();
    }
}
