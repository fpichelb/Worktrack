using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;

namespace Worktrack.Services;

public class TimeEntryService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly SeasonService _seasonService;

    public TimeEntryService(IDbContextFactory<AppDbContext> factory, SeasonService seasonService)
    {
        _factory = factory;
        _seasonService = seasonService;
    }

    // --------------------------------------------------------------------
    // CHECK IN
    // --------------------------------------------------------------------
    public async Task<TimeEntry> CheckInAsync(int eventId, string name, string? task)
    {
        await using var Db = await _factory.CreateDbContextAsync();
        var entry = new TimeEntry
        {
            EventId = eventId,
            NameEntered = name,
            Task = task,
            CheckIn = DateTime.UtcNow,
            Status = "checked_in"
        };

        Db.TimeEntry.Add(entry);
        await Db.SaveChangesAsync();

        return entry;
    }

    // --------------------------------------------------------------------
    // CHECK OUT
    // --------------------------------------------------------------------
    public async Task<TimeEntry?> CheckOutAsync(int eventId, string name)
    {
        await using var Db = await _factory.CreateDbContextAsync();
        var entry = await Db.TimeEntry
            .Where(e => e.EventId == eventId &&
                        e.NameEntered == name &&
                        e.CheckOut == null)
            .OrderByDescending(e => e.CheckIn)
            .FirstOrDefaultAsync();

        if (entry == null)
            return null;
        var now = DateTime.UtcNow;

        entry.CheckOut = now;


        // Automatisches Matching erlaubt?
        var ev = await Db.Events.FindAsync(eventId);
        if (ev != null && ev.AutoMatchUsers)
        {
            var user = await Db.Users
                .FirstOrDefaultAsync(u => u.Name == name);

            if (user != null)
            {
                entry.UserId = user.Id;
                entry.UserName = user.Name;
                entry.Status = "checked_out";
            }
            else
            {
                entry.Status = "pending_assignment";
            }
        }
        else
        {
            entry.Status = "pending_assignment";
        }

        Db.TimeEntry.Update(entry);
        await Db.SaveChangesAsync();

        return entry;
    }

    // --------------------------------------------------------------------
    // LOAD ALL ENTRIES
    // --------------------------------------------------------------------
    public async Task<List<TimeEntry>> GetAllEntriesAsync()
    {
        await using var Db = await _factory.CreateDbContextAsync();
        return await Db.TimeEntry
            .Include(e => e.Event)
            .Include(e => e.UserName)
            .OrderByDescending(e => e.CheckIn)
            .ToListAsync();
    }

    // --------------------------------------------------------------------
    // LOAD PENDING ASSIGNMENT ENTRIES (Admin)
    // --------------------------------------------------------------------
    public async Task<List<TimeEntry>> GetPendingAssignmentsAsync()
    {
        await using var Db = await _factory.CreateDbContextAsync();
        return await Db.TimeEntry
            .Where(e => e.Status == "pending_assignment")
            .OrderByDescending(e => e.CheckIn)
            .ToListAsync();
    }

    // --------------------------------------------------------------------
    // ASSIGN USER
    // --------------------------------------------------------------------
    public async Task<bool> AssignUserAsync(int entryId, int userId)
    {
        await using var Db = await _factory.CreateDbContextAsync();
        var entry = await Db.TimeEntry.FindAsync(entryId);
        if (entry == null)
            return false;

        var user = await Db.Users.FindAsync(userId);
        if (user == null)
            return false;

        entry.UserId = user.Id;
        entry.UserName = user.Name;
        entry.Status = entry.CheckOut.HasValue ? "checked_out" : "checked_in";

        Db.TimeEntry.Update(entry);
        await Db.SaveChangesAsync();

        return true;
    }
    public async Task<String> UndoLastImport()
{
    await using var Db = await _factory.CreateDbContextAsync();
    var batch = await Db.Imports
        .OrderByDescending(b => b.Id)
        .FirstOrDefaultAsync();

    if (batch == null)
    {
        return "Kein Import gefunden";
    }

    var entries = Db.TimeEntry
        .Where(e => e.ImportBatchId == batch.Id);

    Db.TimeEntry.RemoveRange(entries);
    Db.Imports.Remove(batch);

    await Db.SaveChangesAsync();

    return  "Der letzte Import wurde rückgängig gemacht.";
}

    // --------------------------------------------------------------------
    // GET USER ENTRIES
    // --------------------------------------------------------------------
    public async Task<List<TimeEntry>> GetEntriesForUserAsync(int userId)
    {
        await using var Db = await _factory.CreateDbContextAsync();
        return await Db.TimeEntry
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CheckIn)
            .ToListAsync();
    }
    public async Task<List<TimeEntry>> GetActiveEntriesForUserAsync(int userId,CancellationToken ct = default)
    {
        await using var Db = await _factory.CreateDbContextAsync();
        return await Db.TimeEntry
            .Where(e => e.UserId == userId && e.CheckOut != null && e.IsArchived == false)
            .Include(e => e.Event)
            .ToListAsync(ct);
    }

    public async Task<List<TimeEntry>> GetEntriesForEventAsync(int EventId)
    {
        await using var Db = await _factory.CreateDbContextAsync();
        return await Db.TimeEntry
        .Where(t => t.EventId == EventId)
        .OrderByDescending(t => t.CheckIn)
        .ToListAsync();
    }

    public async Task<bool> UnarchiveEntryAsync(int entryId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entry = await db.TimeEntry.FindAsync(entryId);
        if (entry is null || !entry.IsArchived)
            return false;

        entry.IsArchived = false;

        var ev = await db.Events.FindAsync(entry.EventId);
        if (ev is not null && ev.IsArchived)
        {
            ev.IsArchived = false;
            ev.ArchivedAtUtc = null;
        }

        await db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> IsDuplicate(TimeEntry entry)
    {
        await using var Db = await _factory.CreateDbContextAsync();
        bool ret = Db.TimeEntry.Any(e =>
            e.EventId == entry.EventId &&
            e.NameEntered.ToLower() == entry.NameEntered.ToLower() &&
            e.CheckIn == entry.CheckIn &&
            Math.Abs((e.DurationHours ?? 0) - (entry.DurationHours ?? 0)) < 0.001
            );
        return ret;
    }


    public async Task<List<TimeEntry>> GetAllNotArchivedEntriesAsync()
    {
        await using var Db = await _factory.CreateDbContextAsync();
        return await Db.TimeEntry
        .Where(e => e.CheckIn != null && e.CheckOut != null && !e.IsArchived)
        .ToListAsync();
    }
    // --------------------------------------------------------------------
    // ARCHIVE HOURS (optional)
    // --------------------------------------------------------------------
    public async Task<double> ArchiveUserHoursAsync(User user)
    {
        await using var Db = await _factory.CreateDbContextAsync();
        await SnapshotAchievementsAsync(Db, [user.Id], includePodium: false);

        var entries = await Db.TimeEntry
            .Where(e => e.UserId == user.Id && e.IsArchived == false)
            .ToListAsync();

        double total = entries.Sum(e => e.DurationHours ?? 0);
        total += user.BonusHours; 
        user.ArchivedHours += total;

        // Optional: mark entries as archived ? Up to you
        foreach (var e in entries) e.IsArchived = true;

        await MarkCompletedEventsAsArchivedAsync(Db);
        await Db.SaveChangesAsync();
        return total;
    }
    public async Task<ArchiveResult> ArchiveAllUsersAsync()
    {
        await using var Db = await _factory.CreateDbContextAsync();
        await SnapshotAchievementsAsync(Db, includePodium: true);

        var users = await Db.Users.ToListAsync();
        var entries = await Db.TimeEntry
        .Where(e => e.CheckOut != null &&
                    !e.IsArchived &&
                    e.Status != "pending_assignment")
        .ToListAsync();

        int archivedEntries = 0;
        double totalArchivedHours = 0;

        foreach (var user in users)
        {
            var userEntries = entries.Where(e => e.UserId == user.Id);

            double userHours = userEntries
            .Sum(e => e.DurationHours ?? 0);
            userHours += user.BonusHours;
            if (userHours > 0)
            {
                user.ArchivedHours += userHours;
                archivedEntries += userEntries.Count();
                totalArchivedHours += userHours;
            }
        }

        // Mark entries as archived
        foreach (var entry in entries)
        {
            entry.IsArchived = true;
        }

        await MarkCompletedEventsAsArchivedAsync(Db);
        await Db.SaveChangesAsync();

        return new ArchiveResult
        {
            TotalEntries = archivedEntries,
            TotalHours = totalArchivedHours
        };
    }
    public class ArchiveResult
    {
        public int TotalEntries { get; set; }
        public double TotalHours { get; set; }
    }

    public class PodiumBackfillResult
    {
        public int ArchiveYear { get; set; }
        public int RankedUsers { get; set; }
        public int AwardedYearPodiums { get; set; }
        public int AwardedPermanentPodiums { get; set; }
    }

    public async Task<PodiumBackfillResult> BackfillPodiumsAsync(DateTime from, DateTime to)
    {
        if (to.Date < from.Date)
            throw new ArgumentException("Das Bis-Datum darf nicht vor dem Von-Datum liegen.");

        await using var db = await _factory.CreateDbContextAsync();
        var fromDate = from.Date;
        var toDate = to.Date;
        var archiveYear = to.Date.Year;
        var seasonGroups = await _seasonService.BuildSeasonGroupsAsync();
        var filteredSeasonGroups = seasonGroups
            .Select(pair => new
            {
                Season = pair.Key,
                Events = pair.Value
                    .Where(e => e.StartTime.Date >= fromDate && e.StartTime.Date <= toDate)
                    .ToList()
            })
            .Where(x => x.Events.Any())
            .ToList();
        var relevantEventIds = filteredSeasonGroups
            .SelectMany(x => x.Events)
            .Select(x => x.Id)
            .Distinct()
            .ToList();

        var entries = await db.TimeEntry
            .Where(e => e.CheckOut != null &&
                        e.UserId.HasValue &&
                        relevantEventIds.Contains(e.EventId))
            .ToListAsync();

        var targetUserIds = new HashSet<int>();
        var rankedUserIds = new HashSet<int>();

        foreach (var group in filteredSeasonGroups)
        {
            var stats = _seasonService.BuildSeasonStats(group.Season, group.Events, entries);
            var podium = stats.Take(3).Where(x => x.UserId > 0 && x.Hours >= 9).ToList();

            foreach (var stat in stats.Where(x => x.UserId > 0))
            {
                rankedUserIds.Add(stat.UserId);
            }

            foreach (var item in podium)
            {
                targetUserIds.Add(item.UserId);
            }
        }

        var existing = await db.UserAchievementHistories
            .Where(x => targetUserIds.Contains(x.UserId) &&
                        ((x.Kind == "podium-year" && x.ArchiveYear == archiveYear) ||
                         (x.Kind == "podium-permanent" && x.IsPermanent)))
            .ToListAsync();

        var beforeYear = existing.Count(x => x.Kind == "podium-year" && x.ArchiveYear == archiveYear);
        var beforePermanent = existing.Count(x => x.Kind == "podium-permanent" && x.IsPermanent);

        foreach (var userId in targetUserIds)
        {
            AddAchievementIfMissing(db, existing, userId, "podium-year", "Podium ", "P3", "text-bg-info", archiveYear);
            AddAchievementIfMissing(db, existing, userId, "podium-permanent", "Podium", "POD", "text-bg-info", null, isPermanent: true);
        }

        await db.SaveChangesAsync();

        return new PodiumBackfillResult
        {
            ArchiveYear = archiveYear,
            RankedUsers = rankedUserIds.Count,
            AwardedYearPodiums = existing.Count(x => x.Kind == "podium-year" && x.ArchiveYear == archiveYear) - beforeYear,
            AwardedPermanentPodiums = existing.Count(x => x.Kind == "podium-permanent" && x.IsPermanent) - beforePermanent
        };
    }

    private static async Task MarkCompletedEventsAsArchivedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var eventIdsWithEntries = await db.TimeEntry
            .Select(x => x.EventId)
            .Distinct()
            .ToListAsync();

        if (eventIdsWithEntries.Count == 0)
            return;

        var events = await db.Events
            .Where(e => eventIdsWithEntries.Contains(e.Id) && !e.IsArchived)
            .ToListAsync();

        foreach (var ev in events)
        {
            var allEntriesArchived = await db.TimeEntry
                .Where(x => x.EventId == ev.Id)
                .AllAsync(x => x.IsArchived);

            var isPastEvent = (ev.EndTime ?? ev.StartTime) <= now;
            if (!allEntriesArchived || !isPastEvent)
                continue;

            ev.IsArchived = true;
            ev.IsActive = false;
            ev.ArchivedAtUtc ??= now;
        }
    }

    private async Task SnapshotAchievementsAsync(AppDbContext db, List<int>? onlyUserIds = null, bool includePodium = true)
    {
        var archiveYear = DateTime.Today.Year;
        var targetUserIds = onlyUserIds is { Count: > 0 }
            ? onlyUserIds.Distinct().ToList()
            : null;

        var users = await db.Users.ToListAsync();
        if (users.Count == 0)
            return;

        var allUserIds = users.Select(x => x.Id).ToList();
        var affectedUserIds = targetUserIds ?? allUserIds;
        var entries = await db.TimeEntry
            .Where(e => e.CheckOut != null && !e.IsArchived && e.UserId.HasValue && allUserIds.Contains(e.UserId.Value))
            .ToListAsync();

        var existing = await db.UserAchievementHistories
            .Where(x => affectedUserIds.Contains(x.UserId) &&
                        (x.ArchiveYear == archiveYear || x.IsPermanent || x.Kind == "podium-year"))
            .ToListAsync();

        var activeRows = users
            .Select(u =>
            {
                var userEntries = entries.Where(e => e.UserId == u.Id).ToList();
                return new
                {
                    User = u,
                    Hours = userEntries.Sum(e => e.DurationHours ?? 0) + u.BonusHours,
                    EventsVisited = userEntries.Select(e => e.EventId).Distinct().Count()
                };
            })
            .Where(x => x.Hours > 0)
            .OrderByDescending(x => x.Hours)
            .ThenBy(x => x.User.Name)
            .ToList();

        for (var i = 0; i < activeRows.Count; i++)
        {
            var row = activeRows[i];
            var rank = i + 1;

            if (!affectedUserIds.Contains(row.User.Id))
                continue;

            if (row.EventsVisited < 1)
                continue;

            AddAchievementIfMissing(db, existing, row.User.Id, "newcomer", "Newcomer", "NEW", "text-bg-light", archiveYear);
            if (row.EventsVisited >= 3)
                AddAchievementIfMissing(db, existing, row.User.Id, "eventrunner", "Eventrunner", "RUN", "text-bg-primary", archiveYear);
            if (row.EventsVisited >= 10)
                AddAchievementIfMissing(db, existing, row.User.Id, "eventveteran", "Eventveteran", "VET", "text-bg-dark", archiveYear);
            if (row.Hours >= 20)
                AddAchievementIfMissing(db, existing, row.User.Id, "hardworker", "Hardworker", "15h+", "text-bg-success", archiveYear);
            if (row.Hours >= 50)
                AddAchievementIfMissing(db, existing, row.User.Id, "fiftyhours", "50 Stunden", "50h", "text-bg-warning", archiveYear);
            if (row.Hours >= 100)
                AddAchievementIfMissing(db, existing, row.User.Id, "hundredhours", "100 Stunden", "100h", "text-bg-danger", archiveYear);

            if (includePodium && rank <= 3 && row.Hours >= 9)
            {
                AddAchievementIfMissing(db, existing, row.User.Id, "podium-year", "Podium ", "P3", "text-bg-info", archiveYear);
                AddAchievementIfMissing(db, existing, row.User.Id, "podium-permanent", "Podium", "POD", "text-bg-info", null, isPermanent: true);
            }
        }

        if (includePodium)
        {
            BackfillPermanentPodiums(db, existing, affectedUserIds);
        }
    }

    private static void AddAchievementIfMissing(
        AppDbContext db,
        List<UserAchievementHistory> existing,
        int userId,
        string kind,
        string label,
        string badgeText,
        string colorCss,
        int? archiveYear,
        bool isPermanent = false)
    {
        if (existing.Any(x => x.UserId == userId &&
                              x.Kind == kind &&
                              x.ArchiveYear == archiveYear &&
                              x.IsPermanent == isPermanent))
        {
            return;
        }

        var achievement = new UserAchievementHistory
        {
            UserId = userId,
            Kind = kind,
            Label = label,
            BadgeText = badgeText,
            ColorCss = colorCss,
            ArchiveYear = archiveYear,
            IsPermanent = isPermanent,
            AwardedAtUtc = DateTime.UtcNow
        };

        db.UserAchievementHistories.Add(achievement);
        existing.Add(achievement);
    }

    private static void BackfillPermanentPodiums(
        AppDbContext db,
        List<UserAchievementHistory> existing,
        List<int> affectedUserIds)
    {
        var podiumUsers = existing
            .Where(x => affectedUserIds.Contains(x.UserId) && x.Kind == "podium-year")
            .Select(x => x.UserId)
            .Distinct()
            .ToList();

        foreach (var userId in podiumUsers)
        {
            AddAchievementIfMissing(
                db,
                existing,
                userId,
                "podium-permanent",
                "Podium",
                "POD",
                "text-bg-info",
                null,
                isPermanent: true);
        }
    }

    public async Task<int> AutoCheckoutStaleEntriesAsync()
    {
        await using var Db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;

        // Hole alle offenen Eintr�ge
        var openEntries = await Db.TimeEntry
            .Include(e => e.Event)
            .Where(e => e.CheckOut == null)
            .ToListAsync();

        int count = 0;

        foreach (var entry in openEntries)
        {
            if (entry.Event == null)
                continue;

            var defaultHours = entry.Event.DefaultHours;

            if (defaultHours <= 0)
                continue;

            if ((now - entry.CheckIn)?.TotalHours < 24)
                continue;

            entry.CheckOut = DateTime.Now;
            entry.DurationHours = defaultHours;

            // Status korrekt setzen
            entry.Status = entry.UserId != null ? "checked_out" : "pending_assignment";

            count++;
        }

        if (count > 0)
            await Db.SaveChangesAsync();

        return count;
    }


}
