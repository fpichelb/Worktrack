using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;

namespace Worktrack.Services;

public class TimeEntryService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TimeEntryService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
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
        await SnapshotAchievementsAsync(Db, [user.Id]);

        var entries = await Db.TimeEntry
            .Where(e => e.UserId == user.Id && e.IsArchived == false)
            .ToListAsync();

        double total = entries.Sum(e => e.DurationHours ?? 0);
        total += user.BonusHours; 
        user.ArchivedHours += total;

        // Optional: mark entries as archived ? Up to you
        foreach (var e in entries) e.IsArchived = true;

        await Db.SaveChangesAsync();
        return total;
    }
    public async Task<ArchiveResult> ArchiveAllUsersAsync()
    {
        await using var Db = await _factory.CreateDbContextAsync();
        await SnapshotAchievementsAsync(Db);

        var users = await Db.Users.ToListAsync();
        var entries = await Db.TimeEntry
        .Where(e => e.CheckOut != null && !e.IsArchived)
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

    private async Task SnapshotAchievementsAsync(AppDbContext db, List<int>? onlyUserIds = null)
    {
        var archiveYear = DateTime.Today.Year;
        var usersQuery = db.Users.AsQueryable();
        if (onlyUserIds is not null && onlyUserIds.Count > 0)
            usersQuery = usersQuery.Where(x => onlyUserIds.Contains(x.Id));

        var users = await usersQuery.ToListAsync();
        if (users.Count == 0)
            return;

        var userIds = users.Select(x => x.Id).ToList();
        var entries = await db.TimeEntry
            .Where(e => e.CheckOut != null && !e.IsArchived && e.UserId.HasValue && userIds.Contains(e.UserId.Value))
            .ToListAsync();

        var existing = await db.UserAchievementHistories
            .Where(x => userIds.Contains(x.UserId) && (x.ArchiveYear == archiveYear || x.IsPermanent))
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

            if (rank <= 3 && row.Hours >= 15)
            {
                AddAchievementIfMissing(db, existing, row.User.Id, "podium-year", "Podium ", "P3", "text-bg-info", archiveYear);
                AddAchievementIfMissing(db, existing, row.User.Id, "podium-permanent", "Podium", "POD", "text-bg-info", null, isPermanent: true);
            }
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
