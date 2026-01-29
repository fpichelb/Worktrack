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
    }
}