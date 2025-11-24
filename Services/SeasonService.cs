using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;
namespace Worktrack.Services;


 public class EventUserStats
    {
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public double Hours { get; set; }

    }
public class SeasonService
{
    private readonly AppDbContext DB;

    public SeasonService(AppDbContext db)
    {
        DB = db;
    }


    // ---------------------------------------------------------
    //  SEASON CRUD
    // ---------------------------------------------------------

    public async Task<List<Season>> GetAllAsync()
    {
        return await DB.Seasons.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<Season?> GetByIdAsync(int id)
    {
        return await DB.Seasons.FindAsync(id);
    }

    public async Task<Season> CreateAsync(Season season)
    {
        DB.Seasons.Add(season);
        await DB.SaveChangesAsync();
        return season;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var s = await DB.Seasons.FindAsync(id);
        if (s == null) return false;

        DB.Seasons.Remove(s);
        await DB.SaveChangesAsync();
        return true;
    }

    public async Task<Season> UpdateAsync(Season season)
    {
        DB.Seasons.Update(season);
        await DB.SaveChangesAsync();
        return season;
    }

    // ---------------------------------------------------------
    //  MONTH HANDLING
    // ---------------------------------------------------------

    // Converts comma string "1,2,3" → List<int>
    public List<int> ToMonthList(string? months)
    {
        if (string.IsNullOrWhiteSpace(months)) return new();

        return months
            .Split(',')
            .Select(m => int.TryParse(m, out var i) ? i : 0)
            .Where(i => i >= 1 && i <= 12)
            .ToList();
    }

    // Returns a nice month name string: "Jan, Feb, Mär..."
    public string MonthListToDisplay(string? months)
    {
        var list = ToMonthList(months);

        if (!list.Any()) return "-";

        return string.Join(", ",
            list.Select(i =>
                System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
            )
        );
    }

    // ---------------------------------------------------------
    //  SEASON → EVENT MAPPING
    // ---------------------------------------------------------
    // Regeln:
    //  - Falls Snippet gesetzt: Treffer wenn Name/Description enthält
    //  - Falls Monate gesetzt: Treffer wenn Event-Startmonat passt
    //  - ODER Logik (Snippet OR Months)

    public async Task<Dictionary<Season, List<Event>>> BuildSeasonGroupsAsync()
    {
        var seasons = await DB.Seasons.ToListAsync();
        var events = await DB.Events.ToListAsync();

        var result = new Dictionary<Season, List<Event>>();

        foreach (var season in seasons)
        {
            var seasonMonths = ToMonthList(season.Months);
            var snippet = season.Snippet?.Trim().ToLower();

            var matchedEvents = events.Where(ev =>
            {
                bool matchSnippet = false;
                bool matchMonth = false;

                // Snippet
                if (!string.IsNullOrWhiteSpace(snippet))
                {
                    matchSnippet =
                        (ev.Name?.ToLower().Contains(snippet) ?? false)
                        || (ev.Description?.ToLower().Contains(snippet) ?? false);
                }

                // Monate
                if (seasonMonths.Any())
                {
                    matchMonth = seasonMonths.Contains(ev.StartTime.Month);
                }

                // Wenn Season nichts gesetzt hat → passt nichts
                if (string.IsNullOrWhiteSpace(snippet) && !seasonMonths.Any())
                    return false;

                return matchSnippet || matchMonth;
            })
            .ToList();

            if (matchedEvents.Any())
                result[season] = matchedEvents;
        }

        return result;
    }

    // ---------------------------------------------------------
    //  SEASON → USERSTATS
    //  Wird von TeamStats genutzt
    // ---------------------------------------------------------

    public List<EventUserStats> BuildSeasonStats(
        Season season,
        List<Event> events,
        List<TimeEntry> entries)
    {
        var eventIds = events.Select(e => e.Id).ToList();

        var groupEntries = entries
            .Where(e => e.EventId != null && eventIds.Contains(e.EventId))
            .ToList();

        return groupEntries
            .GroupBy(e => e.UserId)
            .Select(g => new EventUserStats
            {
                UserId = g.Key ?? 0,
                Name = g.First().UserName ?? "Unbekannt",
                Hours = g.Sum(x => x.DurationHours ?? 0)
            })
            .OrderByDescending(x => x.Hours)
            .ToList();
    }


}
