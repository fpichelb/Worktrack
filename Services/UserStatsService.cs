using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;

namespace Worktrack.Services;

public class UserStatsService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public UserStatsService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    // Gibt vollst�ndige Leaderboard-Daten zur�ck
    public async Task<List<UserStatsViewModel>> GetAllUserStatsAsync(bool includeAdmins = false)
    {
        await using var Db = await _factory.CreateDbContextAsync();
        var usersQuery = Db.Users.AsQueryable();

        if (!includeAdmins)
            usersQuery = usersQuery.Where(u => u.Role != "admin");

        var users = await usersQuery.ToListAsync();

        var entries = await Db.Set<TimeEntry>()
            .Include(t => t.UserId)
            .Where(t => t.CheckOut != null)
            .ToListAsync();

        var weekStart = DateTime.UtcNow.AddDays(-7);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var result = new List<UserStatsViewModel>();

        foreach (var user in users)
        {
            var userEntries = entries.Where(e => e.UserId == user.Id).ToList();

            double totalHours = userEntries.Sum(e => (e.CheckOut!.Value - e.CheckIn!.Value).TotalHours);
            double weekHours = userEntries
                .Where(e => e.CheckIn >= weekStart)
                .Sum(e => (e.CheckOut!.Value - e.CheckIn!.Value).TotalHours);
            double monthHours = userEntries
                .Where(e => e.CheckIn >= monthStart)
                .Sum(e => (e.CheckOut!.Value - e.CheckIn!.Value).TotalHours);

            int streak = CalculateStreak(userEntries);

            var achievements = new List<Achievement>();
            if (totalHours >= 100) achievements.Add(new("Century Club", "bg-yellow-100 text-yellow-700"));
            if (totalHours >= 50) achievements.Add(new("Half Century", "bg-blue-100 text-blue-700"));
            if (streak >= 7) achievements.Add(new($"{streak} Day Streak", "bg-orange-100 text-orange-700"));
            if (weekHours >= 40) achievements.Add(new("Full Week", "bg-green-100 text-green-700"));
            if (userEntries.Count >= 50) achievements.Add(new("Dedicated", "bg-purple-100 text-purple-700"));

            result.Add(new UserStatsViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                SecretCode = user.SecretCode,
                TotalHours = totalHours,
                WeekHours = weekHours,
                MonthHours = monthHours,
                Streak = streak,
                Achievements = achievements,
                Role = user.Role ?? "user"
            });
        }

        result = result.OrderByDescending(r => r.TotalHours).ToList();
        for (int i = 0; i < result.Count; i++)
            result[i].Rank = i + 1;

        return result;
    }

    public async Task<UserStatsViewModel?> GetUserStatsAsync(int userId)
    {
        var list = await GetAllUserStatsAsync(true);
        return list.FirstOrDefault(u => u.Id == userId);
    }

    private static int CalculateStreak(List<TimeEntry> entries)
    {
        var daysWorked = entries
            .Select(e => e.CheckIn?.Date)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .Distinct()
            .ToHashSet();

        var current = DateTime.UtcNow.Date;
        var streak = 0;
        while (daysWorked.Contains(current))
        {
            streak++;
            current = current.AddDays(-1);
        }
        return streak;
    }
}

public class UserStatsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string SecretCode { get; set; } = "";
    public string Role { get; set; } = "user";

    public int Rank { get; set; }
    public double TotalHours { get; set; }
    public double WeekHours { get; set; }
    public double MonthHours { get; set; }
    public int Streak { get; set; }

    public List<Achievement> Achievements { get; set; } = new();
}

public record Achievement(string Label, string Color);
