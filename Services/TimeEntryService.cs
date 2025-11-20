using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;

namespace Worktrack.Services;

public class TimeEntryService
{
    private readonly AppDbContext _db;

    public TimeEntryService(AppDbContext db)
    {
        _db = db;
    }

    // --------------------------------------------------------------------
    // CHECK IN
    // --------------------------------------------------------------------
    public async Task<TimeEntry> CheckInAsync(int eventId, string name, string? task)
    {
        var entry = new TimeEntry
        {
            EventId = eventId,
            NameEntered = name,
            Task = task,
            CheckIn = DateTime.UtcNow,
            Status = "checked_in"
        };

        _db.TimeEntry.Add(entry);
        await _db.SaveChangesAsync();

        return entry;
    }

    // --------------------------------------------------------------------
    // CHECK OUT
    // --------------------------------------------------------------------
    public async Task<TimeEntry?> CheckOutAsync(int eventId, string name)
    {
        var entry = await _db.TimeEntry
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
        var ev = await _db.Events.FindAsync(eventId);
        if (ev != null && ev.AutoMatchUsers)
        {
            var user = await _db.Users
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

        _db.TimeEntry.Update(entry);
        await _db.SaveChangesAsync();

        return entry;
    }

    // --------------------------------------------------------------------
    // LOAD ALL ENTRIES
    // --------------------------------------------------------------------
    public async Task<List<TimeEntry>> GetAllEntriesAsync()
    {
        return await _db.TimeEntry
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
        return await _db.TimeEntry
            .Where(e => e.Status == "pending_assignment")
            .OrderByDescending(e => e.CheckIn)
            .ToListAsync();
    }

    // --------------------------------------------------------------------
    // ASSIGN USER
    // --------------------------------------------------------------------
    public async Task<bool> AssignUserAsync(int entryId, int userId)
    {
        var entry = await _db.TimeEntry.FindAsync(entryId);
        if (entry == null)
            return false;

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return false;

        entry.UserId = user.Id;
        entry.UserName = user.Name;
        entry.Status = entry.CheckOut.HasValue ? "checked_out" : "checked_in";

        _db.TimeEntry.Update(entry);
        await _db.SaveChangesAsync();

        return true;
    }

    // --------------------------------------------------------------------
    // GET USER ENTRIES
    // --------------------------------------------------------------------
    public async Task<List<TimeEntry>> GetEntriesForUserAsync(int userId)
    {
        return await _db.TimeEntry
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CheckIn)
            .ToListAsync();
    }

    // --------------------------------------------------------------------
    // ARCHIVE HOURS (optional)
    // --------------------------------------------------------------------
    public async Task<double> ArchiveUserHoursAsync(int userId)
    {
        var entries = await _db.TimeEntry
            .Where(e => e.UserId == userId && e.IsArchived == false)
            .ToListAsync();

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return 0;

        double total = entries.Sum(e => e.DurationHours ?? 0);

        user.ArchivedHours += total;

        // Optional: mark entries as archived ? Up to you
        foreach (var e in entries) e.IsArchived = true;

        await _db.SaveChangesAsync();
        return total;
    }

    public async Task<int> AutoCheckoutStaleEntriesAsync()
    {
        var now = DateTime.UtcNow;

        // Hole alle offenen Einträge
        var openEntries = await _db.TimeEntry
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
            await _db.SaveChangesAsync();

        return count;
    }


}
