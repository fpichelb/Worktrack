using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;
using Worktrack.ViewModels;
namespace Worktrack.Services;
public class ActivityService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public ActivityService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<int> CreateAsync(ActivityCreateVm vm, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var a = new Activity
        {
            Name = vm.Name,
            Ort = vm.Ort,
            Info = vm.Info,
            Datum = vm.Datum,
            LastUnregistrationAt = vm.UnregisteredAt,
            MaxTeilnehmer = vm.MaxTeilnehmer,
            EmpfohleneTeilnehmer = vm.EmpfohleneTeilnehmer,
            FileName =  vm.FileName,
            ContentType=   vm.ContentType,
            Data =   vm.Data
        };
        db.Activities.Add(a);
        await db.SaveChangesAsync(ct);
        return a.Id;
    }
    public async Task<(bool ok, string? error)> UpdateExtraAsync(int extraCount, int activityId, int userId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var reg = await db.ActivityRegistrations.FirstOrDefaultAsync(r => r.UserId == userId && r.ActivityId == activityId && r.UnregisteredAt == null,ct);

        if (reg is null) return (false, "Du bist nicht angemeldet!");
        reg.Extra = extraCount;
        await db.SaveChangesAsync(ct);
        return (true,null);
    }

    public async Task<(bool ok, string? error)> UpdateActivityAsync(ActivityCreateVm vm, int activityId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == activityId, ct);
        if (activity is null) return (false, "Aktivität nicht gefunden");
        activity.Name = vm.Name;
        activity.Ort = vm.Ort;
        activity.Datum = vm.Datum;
        activity.LastUnregistrationAt = vm.UnregisteredAt;
        activity.Info = vm.Info;
        activity.MaxTeilnehmer = vm.MaxTeilnehmer;
        activity.EmpfohleneTeilnehmer = vm.EmpfohleneTeilnehmer;
        activity.FileName = vm.FileName;
        activity.ContentType = vm.ContentType;
        activity.Data = vm.Data;

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<Activity?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Activities
            .Include(x => x.Registrations.Where(r => r.UnregisteredAt == null))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<(bool ok, string? error)> RegisterAsync(int activityId, int userId, bool adminOverride=false,CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == activityId, ct);
        if (activity is null) return (false, "Aktivität nicht gefunden.");

        // schon angemeldet?
        var already = await db.ActivityRegistrations
            .AnyAsync(r => r.ActivityId == activityId && r.UserId == userId && r.UnregisteredAt == null, ct);
        if (already) return (false, "Du bist bereits angemeldet.");

        // Kapazität prüfen
        var activeCount = await db.ActivityRegistrations
            .CountAsync(r => r.ActivityId == activityId && r.UnregisteredAt == null, ct);

        if (activeCount >= activity.MaxTeilnehmer && activity.MaxTeilnehmer>0&&!adminOverride)
            return (false, "Maximale Teilnehmerzahl erreicht.");

        db.ActivityRegistrations.Add(new ActivityRegistration
        {
            ActivityId = activityId,
            UserId = userId,
            RegisteredAt = DateTime.UtcNow,
            Extra = 0
        });

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> UnregisterAsync(int activityId, int userId,bool adminOverride=false, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var reg = await db.ActivityRegistrations
            .Where(r => r.ActivityId == activityId && r.UserId == userId && r.UnregisteredAt == null)
            .OrderByDescending(r => r.RegisteredAt)
            .FirstOrDefaultAsync(ct);
        var activity = await db.Activities.FirstAsync(a => a.Id == activityId, ct);

        if (reg is null) return (false, "Du bist nicht angemeldet.");

        var now = DateTime.Today;
        if ((DateTime.Compare(activity.LastUnregistrationAt ?? DateTime.UtcNow, now) < 0)&&!adminOverride) return (false, "Die Zeit zum Abmelden ist bereits abgelaufen.");

        reg.UnregisteredAt = now;

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<ActivityDetailVm> GetDetailAsync(int activityId, int userId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var Participants = await db.ActivityRegistrations
            .Where(r => r.ActivityId == activityId && r.UnregisteredAt == null)
            .OrderByDescending(r => r.RegisteredAt)
            .Select(r => new ParticipantVm
            {
                UserId = r.UserId,
                DisplayName = r.User.Name,
                RegisteredAt = r.RegisteredAt,
                Extra =r.Extra
            })
            .ToListAsync(ct);
        var count = Participants.Count + Participants.Sum(p => p.Extra);
        var a = await db.Activities
            .Where(a => a.Id == activityId)
            .FirstOrDefaultAsync(ct);

        var reg = await db.ActivityRegistrations
            .FirstOrDefaultAsync(r =>
            r.ActivityId == activityId &&
            r.UserId == userId &&
            r.UnregisteredAt == null,
            ct);
        if (a is null) return new ActivityDetailVm();
        
        return new ActivityDetailVm
        {
            Id = a.Id,
            Name = a.Name,
            Ort = a.Ort,
            Info = a.Info,
            Datum = a.Datum,
            MaxTeilnehmer = a.MaxTeilnehmer,
            EmpfohleneTeilnehmer = a.EmpfohleneTeilnehmer,
            FileName = a.FileName,
            ContentType = a.ContentType,
            Data = a.Data,
            ActiveCount = count,
            LastUnregistrationAt = a.LastUnregistrationAt,
            IsRegistered = reg is not null,
            Extra = reg is null? 0:reg.Extra,
            Participants = Participants
        };
    }
    public async Task<List<ActivityListItemVm>> GetListAsync(int userId , CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var list = await db.Activities
            .Select(a => new ActivityListItemVm
            {
                Id = a.Id,
                Name = a.Name,
                Ort = a.Ort,
                Datum = a.Datum,
                MaxTeilnehmer = a.MaxTeilnehmer,
                EmpfohleneTeilnehmer = a.EmpfohleneTeilnehmer,
                ActiveCount = a.Registrations.Where(r => r.UnregisteredAt == null).Sum(r => 1 + r.Extra),
                IsRegistered = a.Registrations.Any(r=> r.UserId==userId&&r.UnregisteredAt==null)
            })
            .Where(a => a.Datum > DateTime.UtcNow)
            .OrderByDescending(a => a.Datum)
            .ToListAsync(ct);
        return list;
    }
}