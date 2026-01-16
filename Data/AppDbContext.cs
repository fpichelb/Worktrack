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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // optional: Fluent API
    }
}