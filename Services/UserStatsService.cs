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

    public async Task<UserDashboardVm?> GetDashboardAsync(int userId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
            return null;

        var entries = await db.TimeEntry
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.CheckOut != null && !e.IsArchived)
            .Include(e => e.Event)
            .ToListAsync(ct);

        var achievements = await db.UserAchievementHistories
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsPermanent)
            .ThenByDescending(x => x.ArchiveYear)
            .ThenByDescending(x => x.AwardedAtUtc)
            .ToListAsync(ct);
        achievements = CollapseDashboardAchievements(achievements);

        var totalHours = entries.Sum(e => e.DurationHours ?? 0) + user.BonusHours;
        var eventsVisited = entries
            .Select(e => e.EventId)
            .Distinct()
            .Count();

        return new UserDashboardVm
        {
            User = user,
            TotalHours = totalHours,
            EventsVisited = eventsVisited,
            ArchivedAchievements = achievements
                .Select(x => new UserArchivedAchievementVm
                {
                    Kind = x.Kind,
                    Label = x.Label,
                    BadgeText = x.BadgeText,
                    ColorCss = x.ColorCss,
                    ArchiveYear = x.ArchiveYear,
                    IsPermanent = x.IsPermanent
                })
                .ToList(),
            Events = entries
                .Select(g => new UserDashboardEventVm
                {
                    EventId = g.Id,
                    EventName = g.Event?.Name ?? "",
                    Location = g.Event?.Location ?? "",
                    Date = g.CheckIn ?? DateTime.MinValue,
                    Hours = g.DurationHours ?? 0,
                    Description = g.Task ?? ""
                })
                .OrderByDescending(x => x.Date)
                .ToList()
        };
    }

    public async Task<Dictionary<int, List<UserArchivedAchievementVm>>> GetAchievementHistoryMapAsync(
        IEnumerable<int> userIds,
        CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, List<UserArchivedAchievementVm>>();

        await using var db = await _factory.CreateDbContextAsync(ct);
        var achievements = await db.UserAchievementHistories
            .AsNoTracking()
            .Where(x => ids.Contains(x.UserId))
            .OrderByDescending(x => x.IsPermanent)
            .ThenByDescending(x => x.ArchiveYear)
            .ThenByDescending(x => x.AwardedAtUtc)
            .ToListAsync(ct);

        return achievements
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new UserArchivedAchievementVm
                {
                    Kind = x.Kind,
                    Label = x.Label,
                    BadgeText = x.BadgeText,
                    ColorCss = x.ColorCss,
                    ArchiveYear = x.ArchiveYear,
                    IsPermanent = x.IsPermanent
                }).ToList());
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

    private static List<UserAchievementHistory> CollapseDashboardAchievements(List<UserAchievementHistory> achievements)
    {
        return achievements
            .GroupBy(x => new
            {
                x.IsPermanent,
                x.ArchiveYear,
                Family = GetAchievementFamily(x.Kind)
            })
            .Select(g => g
                .OrderByDescending(x => GetAchievementRank(x.Kind))
                .ThenByDescending(x => x.AwardedAtUtc)
                .First())
            .OrderByDescending(x => x.IsPermanent)
            .ThenByDescending(x => x.ArchiveYear)
            .ThenByDescending(x => x.AwardedAtUtc)
            .ToList();
    }

    private static string GetAchievementFamily(string kind)
    {
        return kind switch
        {
            "newcomer" or "eventrunner" or "eventveteran" => "events",
            "hardworker" or "fiftyhours" or "hundredhours" => "hours",
            "podium-year" => "podium-year",
            "podium-permanent" => "podium-permanent",
            _ => kind
        };
    }

    private static int GetAchievementRank(string kind)
    {
        return kind switch
        {
            "newcomer" => 1,
            "eventrunner" => 2,
            "eventveteran" => 3,
            "hardworker" => 1,
            "fiftyhours" => 2,
            "hundredhours" => 3,
            "podium-year" => 1,
            "podium-permanent" => 1,
            _ => 1
        };
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

public class UserDashboardVm
{
    public User User { get; set; } = new();
    public double TotalHours { get; set; }
    public int EventsVisited { get; set; }
    public List<UserArchivedAchievementVm> ArchivedAchievements { get; set; } = new();
    public List<UserDashboardEventVm> Events { get; set; } = new();
}

public class UserArchivedAchievementVm
{
    public string Kind { get; set; } = "";
    public string Label { get; set; } = "";
    public string BadgeText { get; set; } = "";
    public string ColorCss { get; set; } = "text-bg-light";
    public int? ArchiveYear { get; set; }
    public bool IsPermanent { get; set; }
}

public class UserDashboardEventVm
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public double Hours { get; set; }
}
