using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;

namespace Worktrack.Services
{
    public class EventService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

    public EventService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

        // ----------------------------------------------------------
        // GET ALL EVENTS
        // ----------------------------------------------------------
        public async Task<List<Event>> GetAllEventsAsync(bool includeArchived = true)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            var query = Db.Events.AsQueryable();
            if (!includeArchived)
                query = query.Where(e => !e.IsArchived);

            return await query
                .OrderBy(e => e.IsArchived)
                .ThenByDescending(e => e.StartTime)
                .ThenByDescending(e => e.Id)
                .ToListAsync();
        }

        // ----------------------------------------------------------
        // GET SINGLE EVENT
        // ----------------------------------------------------------
        public async Task<Event?> GetEventByIdAsync(int id)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            return await Db.Events.FindAsync(id);
        }

        public async Task<List<Event>> GetSelectableEventsAsync()
        {
            await using var Db = await _factory.CreateDbContextAsync();
            return await Db.Events
                .Where(e => !e.IsArchived)
                .OrderByDescending(e => e.IsActive)
                .ThenBy(e => e.Name)
                .ToListAsync();
        }

        // ----------------------------------------------------------
        // CREATE EVENT
        // ----------------------------------------------------------
        public async Task<Event> CreateEventAsync(Event ev)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            ev.IsArchived = false;
            ev.ArchivedAtUtc = null;
            Db.Events.Add(ev);
            await Db.SaveChangesAsync();
            return ev;
        }

        // ----------------------------------------------------------
        // UPDATE EVENT
        // ----------------------------------------------------------
        public async Task<bool> UpdateEventAsync(Event ev)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            var existing = await Db.Events.FindAsync(ev.Id);
            if (existing == null)
                return false;

            Db.Entry(existing).CurrentValues.SetValues(ev);
            await Db.SaveChangesAsync();

            return true;
        }

        // ----------------------------------------------------------
        // DELETE EVENT
        // ----------------------------------------------------------
        public async Task<bool> DeleteEventAsync(int id)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            var ev = await Db.Events.FindAsync(id);
            if (ev == null)
                return false;

            Db.Events.Remove(ev);
            await Db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ArchiveEventAsync(int id)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            var ev = await Db.Events.FindAsync(id);
            if (ev == null)
                return false;

            ev.IsArchived = true;
            ev.IsActive = false;
            ev.ArchivedAtUtc ??= DateTime.UtcNow;
            await Db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreEventAsync(int id)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            var ev = await Db.Events.FindAsync(id);
            if (ev == null)
                return false;

            ev.IsArchived = false;
            ev.ArchivedAtUtc = null;
            await Db.SaveChangesAsync();
            return true;
        }

        public async Task<Event?> ReplanEventAsync(int id, DateTime newStartDate)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            var source = await Db.Events.FindAsync(id);
            if (source == null)
                return null;

            var clone = new Event
            {
                Name = source.Name,
                Location = source.Location,
                Description = source.Description,
                AutoMatchUsers = source.AutoMatchUsers,
                RequireTask = source.RequireTask,
                DefaultHours = source.DefaultHours,
                StartTime = newStartDate,
                EndTime = source.EndTime.HasValue
                    ? newStartDate.Add(source.EndTime.Value - source.StartTime)
                    : null,
                Season = source.Season,
                IsActive = true,
                IsArchived = false,
                ArchivedAtUtc = null
            };

            Db.Events.Add(clone);
            await Db.SaveChangesAsync();
            return clone;
        }

        // ----------------------------------------------------------
        // ACTIVATE / DEACTIVATE
        // ----------------------------------------------------------
        public async Task<bool> SetActiveAsync(int id, bool active)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            var ev = await Db.Events.FindAsync(id);
            if (ev == null)
                return false;

            ev.IsActive = active;
            await Db.SaveChangesAsync();
            return true;
        }

        // ----------------------------------------------------------
        // GET EVENTS BY GROUP / SEASON (wenn du das sp�ter einbaust)
        // ----------------------------------------------------------
        public async Task<List<Event>> GetEventsBySeasonAsync(string season)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            return await Db.Events
                .Where(e => e.Season == season && !e.IsArchived)
                .ToListAsync();
        }
    }
}
