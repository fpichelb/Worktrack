using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;

namespace Worktrack.Services
{
    public class EventService
    {
        private readonly AppDbContext _db;

        public EventService(AppDbContext db)
        {
            _db = db;
        }

        // ----------------------------------------------------------
        // GET ALL EVENTS
        // ----------------------------------------------------------
        public async Task<List<Event>> GetAllEventsAsync()
        {
            return await _db.Events
                .OrderByDescending(e => e.Id)
                .ToListAsync();
        }

        // ----------------------------------------------------------
        // GET SINGLE EVENT
        // ----------------------------------------------------------
        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _db.Events.FindAsync(id);
        }

        // ----------------------------------------------------------
        // CREATE EVENT
        // ----------------------------------------------------------
        public async Task<Event> CreateEventAsync(Event ev)
        {
            _db.Events.Add(ev);
            await _db.SaveChangesAsync();
            return ev;
        }

        // ----------------------------------------------------------
        // UPDATE EVENT
        // ----------------------------------------------------------
        public async Task<bool> UpdateEventAsync(Event ev)
        {
            var existing = await _db.Events.FindAsync(ev.Id);
            if (existing == null)
                return false;

            _db.Entry(existing).CurrentValues.SetValues(ev);
            await _db.SaveChangesAsync();

            return true;
        }

        // ----------------------------------------------------------
        // DELETE EVENT
        // ----------------------------------------------------------
        public async Task<bool> DeleteEventAsync(int id)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev == null)
                return false;

            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();
            return true;
        }

        // ----------------------------------------------------------
        // ACTIVATE / DEACTIVATE
        // ----------------------------------------------------------
        public async Task<bool> SetActiveAsync(int id, bool active)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev == null)
                return false;

            ev.IsActive = active;
            await _db.SaveChangesAsync();
            return true;
        }

        // ----------------------------------------------------------
        // GET EVENTS BY GROUP / SEASON (wenn du das später einbaust)
        // ----------------------------------------------------------
        public async Task<List<Event>> GetEventsBySeasonAsync(string season)
        {
            return await _db.Events
                .Where(e => e.Season == season)
                .ToListAsync();
        }
    }
}
