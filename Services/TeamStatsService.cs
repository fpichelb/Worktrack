using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;
using Worktrack.ViewModels;

namespace Worktrack.Services;

public class TeamStatsService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly SeasonService _seasonService;
    private readonly UserStatsService _userStatsService;

    public TeamStatsService(
        IDbContextFactory<AppDbContext> factory,
        SeasonService seasonService,
        UserStatsService userStatsService)
    {
        _factory = factory;
        _seasonService = seasonService;
        _userStatsService = userStatsService;
    }

    public async Task<TeamStatsVm> GetTeamStatsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var users = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .ToListAsync(ct);

        var allowedUserIds = users.Where(u => u.ShareStats).Select(x => x.Id).ToHashSet();
        var userNames = users.Where(u => u.ShareStats).ToDictionary(x => x.Id, x => x.Name);

        var entries = await db.TimeEntry
            .AsNoTracking()
            .Where(e => e.CheckIn != null &&
                        e.CheckOut != null &&
                        e.UserId != null &&
                        !e.IsArchived )
            .Include(e => e.Event)
            .Where(e => e.Event != null && !e.Event.IsArchived)
            .ToListAsync(ct);

        var seasonGroups = (await _seasonService.BuildSeasonGroupsAsync())
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Where(e => !e.IsArchived).ToList());
        var achievementHistory = await _userStatsService.GetAchievementHistoryMapAsync(users.Select(x => x.Id), ct);

        var vm = new TeamStatsVm
        {
            GlobalStats = BuildGlobalStats(users, entries, seasonGroups, achievementHistory,allowedUserIds),
            SeasonGroups = BuildSeasonGroups(seasonGroups, entries, userNames)
        };

        return vm;
    }

    private List<TeamGlobalUserStatsVm> BuildGlobalStats(
        List<User> users,
        List<TimeEntry> entries,
        Dictionary<Season, List<Event>> seasonGroups,
        Dictionary<int, List<UserArchivedAchievementVm>> achievementHistory,
        HashSet<int> allowedUserIds)
    {
        var result = new List<TeamGlobalUserStatsVm>();

        foreach (var user in users)
        {
            if (allowedUserIds.Contains(user.Id))
            {
                var userEntries = entries.Where(e => e.UserId == user.Id).ToList();
                var totalHours = userEntries.Sum(e => e.DurationHours ?? 0) + user.BonusHours;
                var eventsVisited = userEntries.Select(e => e.EventId).Distinct().Count();

                result.Add(new TeamGlobalUserStatsVm
                {
                    Id = user.Id,
                    Name = user.Name,
                    TotalHours = totalHours,
                    EventsVisited = eventsVisited,
                    Bonus = user.BonusHours,
                    Achievements = BuildAchievements(user, totalHours, eventsVisited, seasonGroups, entries, achievementHistory)
                });
            }
        }

        return result
            .OrderByDescending(x => x.TotalHours)
            .ThenBy(x => x.Name)
            .Select((x, i) =>
            {
                x.Rank = i + 1;
                return x;
            })
            .ToList();
    }

    private List<TeamSeasonGroupVm> BuildSeasonGroups(
        Dictionary<Season, List<Event>> seasonGroups,
        List<TimeEntry> entries,
        Dictionary<int, string> userNames)
    {
        var result = new List<TeamSeasonGroupVm>();

        foreach (var pair in seasonGroups)
        {
            var stats = _seasonService.BuildSeasonStats(pair.Key, pair.Value, entries);
            foreach (var item in stats.Take(3))
            {
                item.Name = userNames.TryGetValue(item.UserId, out var name) ? name : "----------";
            }

            result.Add(new TeamSeasonGroupVm
            {
                Season = pair.Key,
                Events = pair.Value,
                Podium = stats
            });
        }

        return result;
    }

    private List<TeamAchievementVm> BuildAchievements(
        User user,
        double hours,
        int eventsVisited,
        Dictionary<Season, List<Event>> seasonGroups,
        List<TimeEntry> entries,
        Dictionary<int, List<UserArchivedAchievementVm>> achievementHistory)
    {
        var list = new List<TeamAchievementVm>();

        if (eventsVisited >= 1) list.Add(new() { Shown = "👋", Tooltip = "Newcomer", ColorCss = "text-bg-light" });
        if (eventsVisited >= 3) list.Add(new() { Shown = "🏃", Tooltip = "Eventrunner", ColorCss = "text-bg-primary" });
        if (eventsVisited >= 10) list.Add(new() { Shown = "🎉⚔️", Tooltip = "Eventveteran", ColorCss = "text-bg-dark" });
        if (hours >= 20) list.Add(new() { Shown = "🔥", Tooltip = "Hardworker", ColorCss = "text-bg-success" });
        if (hours >= 50) list.Add(new() { Shown = "🌟", Tooltip = "50 Stunden", ColorCss = "text-bg-warning" });
        if (hours >= 100) list.Add(new() { Shown = "💯", Tooltip = "100 Stunden", ColorCss = "text-bg-danger" });

        var seasonHero = seasonGroups.Keys.Any();
        foreach (var group in seasonGroups.Keys)
        {
            if (!seasonGroups[group].Any())
            {
                seasonHero = false;
                break;
            }

            var seasonEventIds = seasonGroups[group].Select(e => e.Id).ToHashSet();
            if (!entries.Any(e => e.UserId == user.Id && seasonEventIds.Contains(e.EventId)))
            {
                seasonHero = false;
                break;
            }
        }

        if (seasonHero)
            list.Add(new() { Shown = "🌸🍂🚀❄️☀️", Tooltip = "Season Hero", ColorCss = "text-bg-secondary" });

        var isCurrentlyOnAnySeasonPodium = seasonGroups.Any(pair =>
        {
            if (!pair.Value.Any())
            {
                return false;
            }

            var podium = _seasonService.BuildSeasonStats(pair.Key, pair.Value, entries)
                .Take(3)
                .ToList();

            return podium.Any(x => x.UserId == user.Id);
        });

        if (isCurrentlyOnAnySeasonPodium)
            list.Add(new() { Shown = "🥈🏆🥉", Tooltip = "Podium", ColorCss = "text-bg-warning" });
        if (achievementHistory.TryGetValue(user.Id, out var archived))
        {
            foreach (var item in archived.Where(x => x.IsPermanent && x.Kind == "podium-permanent"))
            {
                var shown = GetAchievementBadge(item);
                var tooltip = $"{item.Label} (permanent)";

                list.Add(new TeamAchievementVm
                {
                    Shown = shown,
                    Tooltip = tooltip,
                    ColorCss = item.ColorCss
                });
            }
        }

        return list;
    }

    private static string GetAchievementBadge(UserArchivedAchievementVm item)
    {
        var baseBadge = item.Kind switch
        {
            "newcomer" => "👋",
            "eventrunner" => "🏃",
            "eventveteran" => "🎉⚔️",
            "hardworker" => "🔥",
            "fiftyhours" => "🌟",
            "hundredhours" => "💯",
            "podium-year" => "🥈🏆🥉",
            "podium-permanent" => "🌿🥈🏆🥉🌿",
            _ => string.IsNullOrWhiteSpace(item.BadgeText) ? "🏅" : item.BadgeText
        };

        return item.ArchiveYear.HasValue ? $"{baseBadge} {item.ArchiveYear.Value}" : baseBadge;
    }
}


