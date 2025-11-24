using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Worktrack.Models;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // optional: Fluent API
    }
}