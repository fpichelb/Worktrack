using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;

namespace Worktrack.Services
{
    public class EventService
    {
        private readonly AppDbContext Db;

        public EventService(AppDbContext db)
        {
            Db = db;
        }

        // ----------------------------------------------------------
        // GET ALL EVENTS
        // ----------------------------------------------------------
        public async Task<List<Event>> GetAllEventsAsync()
        {
            return await Db.Events
                .OrderByDescending(e => e.Id)
                .ToListAsync();
        }

        // ----------------------------------------------------------
        // GET SINGLE EVENT
        // ----------------------------------------------------------
        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await Db.Events.FindAsync(id);
        }

        // ----------------------------------------------------------
        // CREATE EVENT
        // ----------------------------------------------------------
        public async Task<Event> CreateEventAsync(Event ev)
        {
            Db.Events.Add(ev);
            await Db.SaveChangesAsync();
            return ev;
        }

        // ----------------------------------------------------------
        // UPDATE EVENT
        // ----------------------------------------------------------
        public async Task<bool> UpdateEventAsync(Event ev)
        {
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
            var ev = await Db.Events.FindAsync(id);
            if (ev == null)
                return false;

            Db.Events.Remove(ev);
            await Db.SaveChangesAsync();
            return true;
        }

        // ----------------------------------------------------------
        // ACTIVATE / DEACTIVATE
        // ----------------------------------------------------------
        public async Task<bool> SetActiveAsync(int id, bool active)
        {
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
            return await Db.Events
                .Where(e => e.Season == season)
                .ToListAsync();
        }
    }
}
