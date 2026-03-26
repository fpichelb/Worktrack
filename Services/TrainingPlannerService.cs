using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;
using Worktrack.ViewModels;

namespace Worktrack.Services;

public class TrainingPlannerService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TrainingPlannerService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<TrainingRoomVm>> GetRoomsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.TrainingRooms
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new TrainingRoomVm
            {
                Id = x.Id,
                Name = x.Name,
                Color = x.Color
            })
            .ToListAsync(ct);
    }

    public async Task<(bool ok, string? error, int? id)> AddRoomAsync(string name, string color, CancellationToken ct = default)
    {
        name = (name ?? "").Trim();
        if (name.Length < 2)
            return (false, "Raumname zu kurz.", null);

        await using var db = await _factory.CreateDbContextAsync(ct);

        var exists = await db.TrainingRooms.AnyAsync(x => x.Name == name && x.IsActive, ct);
        if (exists)
            return (false, "Raum existiert bereits.", null);

        var nextSort = await db.TrainingRooms.Select(x => (int?)x.SortOrder).MaxAsync(ct) ?? 0;
        var room = new TrainingRoom
        {
            Name = name,
            Color = string.IsNullOrWhiteSpace(color) ? "#2563eb" : color,
            SortOrder = nextSort + 10
        };

        db.TrainingRooms.Add(room);
        await db.SaveChangesAsync(ct);
        return (true, null, room.Id);
    }
    public async Task<(bool ok, string? error, int? id)> DeleteRoomAsync(TrainingRoomVm roomVM,CancellationToken ct = default)
    {   
        await using var db = await _factory.CreateDbContextAsync(ct);
        var room = await db.TrainingRooms.FirstOrDefaultAsync(room => room.Id == roomVM.Id);
        if (room is null) return (false, "Raum nicht gefunden", null);
        db.TrainingRooms.Remove(room);
        await db.SaveChangesAsync(ct);
        return (true, null, room.Id);
    }

    public async Task<TrainingWeekVm> GetWeekAsync(DateTime weekStart, int currentUserId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var start = StartOfWeek(weekStart);
        var end = start.AddDays(7);
        var startDate = DateOnly.FromDateTime(start);
        var endDate = DateOnly.FromDateTime(end.AddDays(-1));

        var rooms = await db.TrainingRooms
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new TrainingRoomVm
            {
                Id = x.Id,
                Name = x.Name,
                Color = x.Color
            })
            .ToListAsync(ct);

        var standaloneEvents = await db.TrainingEvents
            .Where(x => x.TrainingSeriesId == null && x.StartAt < end && x.EndAt >= start)
            .OrderBy(x => x.StartAt)
            .Select(x => new TrainingWeekEventVm
            {
                OccurrenceKey = BuildSingleKey(x.Id),
                EventId = x.Id,
                Title = x.Title,
                TrainingRoomId = x.TrainingRoomId,
                RoomName = x.TrainingRoom.Name,
                RoomColor = x.TrainingRoom.Color,
                StartAt = x.StartAt,
                EndAt = x.EndAt,
                ParticipantCount = x.Participants.Sum(p => p.ParticipantCount),
                MaxParticipants = x.MaxParticipants,
                IsCurrentUserRegistered = currentUserId > 0 && x.Participants.Any(p => p.UserId == currentUserId),
                AllowMemberRegistration = x.AllowMemberRegistration,
                AllowGroupRegistration = x.AllowGroupRegistration,
                AllowParticipantEventSubmission = x.AllowParticipantEventSubmission,
                IsRecurring = false,
                HasMaterializedEvent = true
            })
            .ToListAsync(ct);

        var series = await db.TrainingSeries
            .Where(x => x.IsActive &&
                        x.StartDate <= endDate &&
                        (!x.UntilDate.HasValue || x.UntilDate.Value >= startDate))
            .Include(x => x.TrainingRoom)
            .Include(x => x.Exceptions.Where(e => e.OccurrenceDate >= startDate && e.OccurrenceDate <= endDate))
            .Include(x => x.MaterializedEvents.Where(e => e.OccurrenceDate.HasValue &&
                                                          e.OccurrenceDate.Value >= startDate &&
                                                          e.OccurrenceDate.Value <= endDate))
                .ThenInclude(e => e.Participants)
            .ToListAsync(ct);

        var recurringEvents = new List<TrainingWeekEventVm>();

        foreach (var item in series)
        {
            var exceptionDates = item.Exceptions
                .Where(x => x.IsCancelled)
                .Select(x => x.OccurrenceDate)
                .ToHashSet();

            var materializedMap = item.MaterializedEvents
                .Where(x => x.OccurrenceDate.HasValue)
                .ToDictionary(x => x.OccurrenceDate!.Value, x => x);

            foreach (var occurrenceDate in ExpandSeriesOccurrences(item, startDate, endDate))
            {
                if (exceptionDates.Contains(occurrenceDate))
                    continue;

                if (materializedMap.TryGetValue(occurrenceDate, out var materialized))
                {
                    recurringEvents.Add(new TrainingWeekEventVm
                    {
                        OccurrenceKey = BuildSeriesKey(item.Id, occurrenceDate),
                        EventId = materialized.Id,
                        SeriesId = item.Id,
                        Title = materialized.Title,
                        TrainingRoomId = materialized.TrainingRoomId,
                        RoomName = item.TrainingRoom.Name,
                        RoomColor = item.TrainingRoom.Color,
                        StartAt = materialized.StartAt,
                        EndAt = materialized.EndAt,
                        ParticipantCount = materialized.Participants.Sum(p => p.ParticipantCount),
                        MaxParticipants = materialized.MaxParticipants,
                        IsCurrentUserRegistered = currentUserId > 0 && materialized.Participants.Any(p => p.UserId == currentUserId),
                        AllowMemberRegistration = materialized.AllowMemberRegistration,
                        AllowGroupRegistration = materialized.AllowGroupRegistration,
                        AllowParticipantEventSubmission = materialized.AllowParticipantEventSubmission,
                        IsRecurring = true,
                        HasMaterializedEvent = true
                    });
                }
                else
                {
                    recurringEvents.Add(new TrainingWeekEventVm
                    {
                        OccurrenceKey = BuildSeriesKey(item.Id, occurrenceDate),
                        SeriesId = item.Id,
                        Title = item.Title,
                        TrainingRoomId = item.TrainingRoomId,
                        RoomName = item.TrainingRoom.Name,
                        RoomColor = item.TrainingRoom.Color,
                        StartAt = occurrenceDate.ToDateTime(item.StartTime),
                        EndAt = occurrenceDate.ToDateTime(item.EndTime),
                        ParticipantCount = 0,
                        MaxParticipants = item.MaxParticipants,
                        IsCurrentUserRegistered = false,
                        AllowMemberRegistration = item.AllowMemberRegistration,
                        AllowGroupRegistration = item.AllowGroupRegistration,
                        AllowParticipantEventSubmission = item.AllowParticipantEventSubmission,
                        IsRecurring = true,
                        HasMaterializedEvent = false
                    });
                }
            }
        }

        return new TrainingWeekVm
        {
            WeekStart = start,
            Days = Enumerable.Range(0, 7).Select(offset => start.AddDays(offset)).ToList(),
            Rooms = rooms,
            Events = standaloneEvents.Concat(recurringEvents).OrderBy(x => x.StartAt).ToList()
        };
    }

    public async Task<TrainingEventDetailVm?> GetOccurrenceDetailAsync(string occurrenceKey, int currentUserId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var parsed = ParseOccurrenceKey(occurrenceKey);
        if (parsed is null)
            return null;

        if (parsed.Kind == OccurrenceKind.Single && parsed.EventId.HasValue)
            return await BuildSingleDetailAsync(db, parsed.EventId.Value, currentUserId, ct);

        if (parsed.Kind != OccurrenceKind.Series || !parsed.SeriesId.HasValue || !parsed.OccurrenceDate.HasValue)
            return null;

        var series = await db.TrainingSeries
            .Include(x => x.TrainingRoom)
            .Include(x => x.Exceptions)
            .Include(x => x.MaterializedEvents.Where(e => e.OccurrenceDate == parsed.OccurrenceDate))
                .ThenInclude(e => e.Participants)
            .FirstOrDefaultAsync(x => x.Id == parsed.SeriesId.Value, ct);

        if (series is null || !IsOccurrencePartOfSeries(series, parsed.OccurrenceDate.Value))
            return null;
        if (series.Exceptions.Any(x => x.OccurrenceDate == parsed.OccurrenceDate.Value && x.IsCancelled))
            return null;

        var materialized = series.MaterializedEvents.FirstOrDefault(x => x.OccurrenceDate == parsed.OccurrenceDate.Value);
        if (materialized is not null)
            return BuildDetailFromMaterialized(series, materialized, currentUserId);

        var startAt = parsed.OccurrenceDate.Value.ToDateTime(series.StartTime);
        var endAt = parsed.OccurrenceDate.Value.ToDateTime(series.EndTime);

        return new TrainingEventDetailVm
        {
            OccurrenceKey = occurrenceKey,
            SeriesId = series.Id,
            Title = series.Title,
            Description = series.Description,
            TrainingRoomId = series.TrainingRoomId,
            RoomName = series.TrainingRoom.Name,
            RoomColor = series.TrainingRoom.Color,
            StartAt = startAt,
            EndAt = endAt,
            OccurrenceDate = startAt.Date,
            MaxParticipants = series.MaxParticipants,
            RecommendedParticipants = series.RecommendedParticipants,
            ParticipantCount = 0,
            RecurrencePattern = series.RecurrencePattern,
            RecurrenceUntil = series.UntilDate?.ToDateTime(TimeOnly.MinValue),
            AllowMemberRegistration = series.AllowMemberRegistration,
            AllowGroupRegistration = series.AllowGroupRegistration,
            AllowParticipantEventSubmission = series.AllowParticipantEventSubmission,
            IsCurrentUserRegistered = false,
            IsRecurring = true,
            HasMaterializedEvent = false,
            Participants = new()
        };
    }

    public async Task<(bool ok, string? error, string? occurrenceKey)> CreateEventsAsync(TrainingEventEditVm vm, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vm.Title))
            return (false, "Titel fehlt.", null);
        if (vm.TrainingRoomId <= 0)
            return (false, "Bitte einen Raum wählen.", null);
        if (vm.EndTime <= vm.StartTime)
            return (false, "Endzeit muss nach der Startzeit liegen.", null);
        if (vm.MaxParticipants < 0 || vm.RecommendedParticipants < 0)
            return (false, "Teilnehmerzahlen dürfen nicht negativ sein.", null);

        await using var db = await _factory.CreateDbContextAsync(ct);

        var roomExists = await db.TrainingRooms.AnyAsync(x => x.Id == vm.TrainingRoomId && x.IsActive, ct);
        if (!roomExists)
            return (false, "Raum nicht gefunden.", null);

        var roomConflict = await ValidateRoomAvailabilityAsync(db, vm, ct: ct);
        if (roomConflict is not null)
            return (false, roomConflict, null);

        if (vm.IsRecurring)
        {
            var series = new TrainingSeries
            {
                Title = vm.Title.Trim(),
                Description = (vm.Description ?? "").Trim(),
                TrainingRoomId = vm.TrainingRoomId,
                StartDate = DateOnly.FromDateTime(vm.Date),
                StartTime = vm.StartTime,
                EndTime = vm.EndTime,
                RecurrencePattern = vm.RecurrencePattern,
                RecurrenceInterval = Math.Max(1, vm.RecurrenceInterval),
                UntilDate = vm.RecurrenceUntil.HasValue ? DateOnly.FromDateTime(vm.RecurrenceUntil.Value) : null,
                IsActive = true,
                MaxParticipants = vm.MaxParticipants,
                RecommendedParticipants = vm.RecommendedParticipants,
                AllowMemberRegistration = vm.AllowMemberRegistration,
                AllowGroupRegistration = vm.AllowGroupRegistration,
                AllowParticipantEventSubmission = vm.AllowParticipantEventSubmission
            };

            db.TrainingSeries.Add(series);
            await db.SaveChangesAsync(ct);

            return (true, null, BuildSeriesKey(series.Id, series.StartDate));
        }

        var entity = new TrainingEvent
        {
            Title = vm.Title.Trim(),
            Description = (vm.Description ?? "").Trim(),
            TrainingRoomId = vm.TrainingRoomId,
            StartAt = vm.Date.Date.Add(vm.StartTime.ToTimeSpan()),
            EndAt = vm.Date.Date.Add(vm.EndTime.ToTimeSpan()),
            MaxParticipants = vm.MaxParticipants,
            RecommendedParticipants = vm.RecommendedParticipants,
            AllowMemberRegistration = vm.AllowMemberRegistration,
            AllowGroupRegistration = vm.AllowGroupRegistration,
            AllowParticipantEventSubmission = vm.AllowParticipantEventSubmission
        };

        db.TrainingEvents.Add(entity);
        await db.SaveChangesAsync(ct);

        return (true, null, BuildSingleKey(entity.Id));
    }

    public async Task<(bool ok, string? error)> UpdateEventAsync(int eventId, TrainingEventEditVm vm, CancellationToken ct = default)
    {
        var validationError = ValidateEditVm(vm);
        if (validationError is not null)
            return (false, validationError);

        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.TrainingEvents.FirstOrDefaultAsync(x => x.Id == eventId && x.TrainingSeriesId == null, ct);
        if (entity is null)
            return (false, "Nur einzelne Termine können direkt bearbeitet werden.");

        var roomConflict = await ValidateRoomAvailabilityAsync(db, vm, excludeEventId: eventId, ct: ct);
        if (roomConflict is not null)
            return (false, roomConflict);

        entity.Title = vm.Title.Trim();
        entity.Description = (vm.Description ?? "").Trim();
        entity.TrainingRoomId = vm.TrainingRoomId;
        entity.StartAt = vm.Date.Date.Add(vm.StartTime.ToTimeSpan());
        entity.EndAt = vm.Date.Date.Add(vm.EndTime.ToTimeSpan());
        entity.MaxParticipants = vm.MaxParticipants;
        entity.RecommendedParticipants = vm.RecommendedParticipants;
        entity.AllowMemberRegistration = vm.AllowMemberRegistration;
        entity.AllowGroupRegistration = vm.AllowGroupRegistration;
        entity.AllowParticipantEventSubmission = vm.AllowParticipantEventSubmission;

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error, string? occurrenceKey)> UpdateSeriesAsync(int seriesId, TrainingEventEditVm vm, CancellationToken ct = default)
    {
        var validationError = ValidateEditVm(vm);
        if (validationError is not null)
            return (false, validationError, null);
        if (!vm.IsRecurring)
            return (false, "Serien müssen weiterhin als wiederkehrend gespeichert werden.", null);

        await using var db = await _factory.CreateDbContextAsync(ct);

        var series = await db.TrainingSeries.FirstOrDefaultAsync(x => x.Id == seriesId, ct);
        if (series is null)
            return (false, "Serie nicht gefunden.", null);

        var roomExists = await db.TrainingRooms.AnyAsync(x => x.Id == vm.TrainingRoomId && x.IsActive, ct);
        if (!roomExists)
            return (false, "Raum nicht gefunden.", null);

        var roomConflict = await ValidateRoomAvailabilityAsync(db, vm, excludeSeriesId: seriesId, candidateSeriesStartDate: series.StartDate, ct: ct);
        if (roomConflict is not null)
            return (false, roomConflict, null);

        var originalStartDate = series.StartDate;
        ApplySeriesValues(series, vm);
        series.StartDate = originalStartDate;

        var materializedEvents = await db.TrainingEvents
            .Where(x => x.TrainingSeriesId == series.Id)
            .ToListAsync(ct);

        foreach (var item in materializedEvents)
        {
            var occurrenceDate = item.OccurrenceDate ?? DateOnly.FromDateTime(item.StartAt);
            item.Title = series.Title;
            item.Description = series.Description;
            item.TrainingRoomId = series.TrainingRoomId;
            item.StartAt = occurrenceDate.ToDateTime(series.StartTime);
            item.EndAt = occurrenceDate.ToDateTime(series.EndTime);
            item.MaxParticipants = series.MaxParticipants;
            item.RecommendedParticipants = series.RecommendedParticipants;
            item.AllowMemberRegistration = series.AllowMemberRegistration;
            item.AllowGroupRegistration = series.AllowGroupRegistration;
            item.AllowParticipantEventSubmission = series.AllowParticipantEventSubmission;
        }

        await db.SaveChangesAsync(ct);
        return (true, null, BuildSeriesKey(series.Id, series.StartDate));
    }

    public async Task<(bool ok, string? error, string? occurrenceKey)> SplitSeriesFromOccurrenceAsync(int seriesId, DateTime fromOccurrenceDate, TrainingEventEditVm vm, CancellationToken ct = default)
    {
        var validationError = ValidateEditVm(vm);
        if (validationError is not null)
            return (false, validationError, null);
        if (!vm.IsRecurring)
            return (false, "Ab hier ändern bleibt eine Serie und kann nicht in einen Einzeltermin umgewandelt werden.", null);

        await using var db = await _factory.CreateDbContextAsync(ct);

        var series = await db.TrainingSeries
            .Include(x => x.Exceptions)
            .Include(x => x.MaterializedEvents)
            .FirstOrDefaultAsync(x => x.Id == seriesId, ct);
        if (series is null)
            return (false, "Serie nicht gefunden.", null);

        var roomExists = await db.TrainingRooms.AnyAsync(x => x.Id == vm.TrainingRoomId && x.IsActive, ct);
        if (!roomExists)
            return (false, "Raum nicht gefunden.", null);

        var splitDate = DateOnly.FromDateTime(fromOccurrenceDate);
        if (!IsOccurrencePartOfSeries(series, splitDate))
            return (false, "Der gewählte Termin gehört nicht mehr zur Serie.", null);

        var roomConflict = await ValidateRoomAvailabilityAsync(db, vm, excludeSeriesId: seriesId, candidateSeriesStartDate: splitDate, ct: ct);
        if (roomConflict is not null)
            return (false, roomConflict, null);

        var newUntilDate = splitDate.AddDays(-1);
        if (newUntilDate < series.StartDate)
        {
            series.IsActive = false;
            series.UntilDate = series.StartDate;
        }
        else
        {
            series.UntilDate = newUntilDate;
        }

        var newSeries = new TrainingSeries();
        ApplySeriesValues(newSeries, vm);
        newSeries.StartDate = splitDate;
        db.TrainingSeries.Add(newSeries);

        var movedEvents = series.MaterializedEvents
            .Where(x => (x.OccurrenceDate ?? DateOnly.FromDateTime(x.StartAt)) >= splitDate)
            .ToList();

        foreach (var item in movedEvents)
        {
            var occurrenceDate = item.OccurrenceDate ?? DateOnly.FromDateTime(item.StartAt);
            item.TrainingSeries = newSeries;
            item.Title = newSeries.Title;
            item.Description = newSeries.Description;
            item.TrainingRoomId = newSeries.TrainingRoomId;
            item.StartAt = occurrenceDate.ToDateTime(newSeries.StartTime);
            item.EndAt = occurrenceDate.ToDateTime(newSeries.EndTime);
            item.MaxParticipants = newSeries.MaxParticipants;
            item.RecommendedParticipants = newSeries.RecommendedParticipants;
            item.AllowMemberRegistration = newSeries.AllowMemberRegistration;
            item.AllowGroupRegistration = newSeries.AllowGroupRegistration;
            item.AllowParticipantEventSubmission = newSeries.AllowParticipantEventSubmission;
        }

        var movedExceptions = series.Exceptions
            .Where(x => x.OccurrenceDate >= splitDate)
            .ToList();

        foreach (var item in movedExceptions)
        {
            item.TrainingSeries = newSeries;
        }

        await db.SaveChangesAsync(ct);
        return (true, null, BuildSeriesKey(newSeries.Id, newSeries.StartDate));
    }

    public async Task<(bool ok, string? error)> RegisterCurrentUserAsync(string occurrenceKey, int userId, CancellationToken ct = default)
    {
        if (userId <= 0)
            return (false, "Benutzer nicht erkannt.");

        await using var db = await _factory.CreateDbContextAsync(ct);

        var item = await GetOrMaterializeOccurrenceAsync(db, occurrenceKey, ct);
        if (item is null)
            return (false, "Termin nicht gefunden.");
        if (!item.AllowMemberRegistration)
            return (false, "Anmeldung ist für diesen Termin nicht freigeschaltet.");

        var existing = await db.TrainingEventParticipants
            .FirstOrDefaultAsync(x => x.TrainingEventId == item.Id && x.UserId == userId, ct);
        if (existing is not null)
            return (false, "Du bist bereits eingetragen.");

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
            return (false, "Benutzer nicht gefunden.");

        var used = await db.TrainingEventParticipants
            .Where(x => x.TrainingEventId == item.Id)
            .SumAsync(x => x.ParticipantCount, ct);

        if (item.MaxParticipants > 0 && used >= item.MaxParticipants)
            return (false, "Maximale Teilnehmerzahl erreicht.");

        db.TrainingEventParticipants.Add(new TrainingEventParticipant
        {
            TrainingEventId = item.Id,
            UserId = userId,
            DisplayName = user.Name,
            ParticipantType = "member",
            ParticipantCount = 1,
            RegisteredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> UnregisterCurrentUserAsync(string occurrenceKey, int userId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var item = await ResolveMaterializedOccurrenceAsync(db, occurrenceKey, ct);
        if (item is null)
            return (false, "Kein Eintrag gefunden.");

        var entry = await db.TrainingEventParticipants
            .FirstOrDefaultAsync(x => x.TrainingEventId == item.Id && x.UserId == userId, ct);

        if (entry is null)
            return (false, "Kein Eintrag gefunden.");

        db.TrainingEventParticipants.Remove(entry);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> AddParticipantAsync(string occurrenceKey, TrainingParticipantEditVm vm, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var item = await GetOrMaterializeOccurrenceAsync(db, occurrenceKey, ct);
        if (item is null)
            return (false, "Termin nicht gefunden.");
        if (vm.ParticipantCount < 0)
            return (false, "Teilnehmerzahl darf nicht negativ sein.");

        var displayName = (vm.DisplayName ?? "").Trim();
        if (vm.UserId.HasValue)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == vm.UserId.Value, ct);
            if (user is null)
                return (false, "Benutzer nicht gefunden.");

            displayName = user.Name;

            var already = await db.TrainingEventParticipants
                .FirstOrDefaultAsync(x => x.TrainingEventId == item.Id && x.UserId == vm.UserId.Value, ct);
            if (already is not null)
                return (false, "Benutzer ist bereits eingetragen.");
        }
        else if (displayName.Length < 2)
        {
            return (false, "Name zu kurz.");
        }

        var used = await db.TrainingEventParticipants
            .Where(x => x.TrainingEventId == item.Id)
            .SumAsync(x => x.ParticipantCount, ct);

        if (item.MaxParticipants > 0 && used + vm.ParticipantCount > item.MaxParticipants)
            return (false, "Teilnehmerlimit würde überschritten.");

        db.TrainingEventParticipants.Add(new TrainingEventParticipant
        {
            TrainingEventId = item.Id,
            UserId = vm.UserId,
            DisplayName = displayName,
            ParticipantType = string.IsNullOrWhiteSpace(vm.ParticipantType) ? "member" : vm.ParticipantType.Trim(),
            ParticipantCount = vm.ParticipantCount,
            SourceGroupName = (vm.SourceGroupName ?? "").Trim(),
            Notes = (vm.Notes ?? "").Trim(),
            RegisteredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> RemoveParticipantAsync(int participantId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var entry = await db.TrainingEventParticipants.FirstOrDefaultAsync(x => x.Id == participantId, ct);
        if (entry is null)
            return (false, "Eintrag nicht gefunden.");

        db.TrainingEventParticipants.Remove(entry);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> CancelOccurrenceAsync(string occurrenceKey, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var parsed = ParseOccurrenceKey(occurrenceKey);
        if (parsed is null)
            return (false, "Termin nicht gefunden.");

        if (parsed.Kind == OccurrenceKind.Single && parsed.EventId.HasValue)
        {
            var item = await db.TrainingEvents.FirstOrDefaultAsync(x => x.Id == parsed.EventId.Value, ct);
            if (item is null)
                return (false, "Termin nicht gefunden.");

            db.TrainingEvents.Remove(item);
            await db.SaveChangesAsync(ct);
            return (true, null);
        }

        if (parsed.Kind != OccurrenceKind.Series || !parsed.SeriesId.HasValue || !parsed.OccurrenceDate.HasValue)
            return (false, "Termin nicht gefunden.");

        var series = await db.TrainingSeries
            .Include(x => x.MaterializedEvents.Where(e => e.OccurrenceDate == parsed.OccurrenceDate))
            .FirstOrDefaultAsync(x => x.Id == parsed.SeriesId.Value, ct);
        if (series is null)
            return (false, "Serie nicht gefunden.");

        var existingException = await db.TrainingSeriesExceptions
            .FirstOrDefaultAsync(x => x.TrainingSeriesId == series.Id && x.OccurrenceDate == parsed.OccurrenceDate.Value, ct);
        if (existingException is null)
        {
            db.TrainingSeriesExceptions.Add(new TrainingSeriesException
            {
                TrainingSeriesId = series.Id,
                OccurrenceDate = parsed.OccurrenceDate.Value,
                IsCancelled = true,
                Reason = "Einzeltermin entfernt"
            });
        }
        else
        {
            existingException.IsCancelled = true;
            existingException.Reason = "Einzeltermin entfernt";
        }

        if (series.MaterializedEvents.Count > 0)
            db.TrainingEvents.RemoveRange(series.MaterializedEvents);

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> StopSeriesAsync(int seriesId, DateTime fromOccurrenceDate, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var series = await db.TrainingSeries.FirstOrDefaultAsync(x => x.Id == seriesId, ct);
        if (series is null)
            return (false, "Serie nicht gefunden.");

        var stopDate = DateOnly.FromDateTime(fromOccurrenceDate).AddDays(-1);
        if (stopDate < series.StartDate)
        {
            series.IsActive = false;
            series.UntilDate = series.StartDate;
        }
        else
        {
            series.UntilDate = stopDate;
        }

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    private async Task<TrainingEventDetailVm?> BuildSingleDetailAsync(AppDbContext db, int eventId, int currentUserId, CancellationToken ct)
    {
        return await db.TrainingEvents
            .Where(x => x.Id == eventId)
            .Select(x => new TrainingEventDetailVm
            {
                OccurrenceKey = BuildSingleKey(x.Id),
                EventId = x.Id,
                Title = x.Title,
                Description = x.Description,
                TrainingRoomId = x.TrainingRoomId,
                RoomName = x.TrainingRoom.Name,
                RoomColor = x.TrainingRoom.Color,
                StartAt = x.StartAt,
                EndAt = x.EndAt,
                OccurrenceDate = x.StartAt.Date,
                MaxParticipants = x.MaxParticipants,
                RecommendedParticipants = x.RecommendedParticipants,
                ParticipantCount = x.Participants.Sum(p => p.ParticipantCount),
                AllowMemberRegistration = x.AllowMemberRegistration,
                AllowGroupRegistration = x.AllowGroupRegistration,
                AllowParticipantEventSubmission = x.AllowParticipantEventSubmission,
                IsCurrentUserRegistered = currentUserId > 0 && x.Participants.Any(p => p.UserId == currentUserId),
                IsRecurring = false,
                HasMaterializedEvent = true,
                Participants = x.Participants
                    .OrderBy(p => p.DisplayName)
                    .Select(p => new TrainingParticipantVm
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        DisplayName = p.DisplayName,
                        ParticipantType = p.ParticipantType,
                        ParticipantCount = p.ParticipantCount,
                        SourceGroupName = p.SourceGroupName,
                        Notes = p.Notes,
                        RegisteredAt = p.RegisteredAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    private static TrainingEventDetailVm BuildDetailFromMaterialized(TrainingSeries series, TrainingEvent item, int currentUserId)
    {
        return new TrainingEventDetailVm
        {
            OccurrenceKey = BuildSeriesKey(series.Id, item.OccurrenceDate ?? DateOnly.FromDateTime(item.StartAt)),
            EventId = item.Id,
            SeriesId = series.Id,
            Title = item.Title,
            Description = item.Description,
            TrainingRoomId = item.TrainingRoomId,
            RoomName = series.TrainingRoom.Name,
            RoomColor = series.TrainingRoom.Color,
            StartAt = item.StartAt,
            EndAt = item.EndAt,
            OccurrenceDate = item.StartAt.Date,
            MaxParticipants = item.MaxParticipants,
            RecommendedParticipants = item.RecommendedParticipants,
            ParticipantCount = item.Participants.Sum(p => p.ParticipantCount),
            RecurrencePattern = series.RecurrencePattern,
            RecurrenceUntil = series.UntilDate?.ToDateTime(TimeOnly.MinValue),
            AllowMemberRegistration = item.AllowMemberRegistration,
            AllowGroupRegistration = item.AllowGroupRegistration,
            AllowParticipantEventSubmission = item.AllowParticipantEventSubmission,
            IsCurrentUserRegistered = currentUserId > 0 && item.Participants.Any(p => p.UserId == currentUserId),
            IsRecurring = true,
            HasMaterializedEvent = true,
            Participants = item.Participants
                .OrderBy(p => p.DisplayName)
                .Select(p => new TrainingParticipantVm
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    DisplayName = p.DisplayName,
                    ParticipantType = p.ParticipantType,
                    ParticipantCount = p.ParticipantCount,
                    SourceGroupName = p.SourceGroupName,
                    Notes = p.Notes,
                    RegisteredAt = p.RegisteredAt
                })
                .ToList()
        };
    }

    private async Task<TrainingEvent?> ResolveMaterializedOccurrenceAsync(AppDbContext db, string occurrenceKey, CancellationToken ct)
    {
        var parsed = ParseOccurrenceKey(occurrenceKey);
        if (parsed is null)
            return null;

        if (parsed.Kind == OccurrenceKind.Single && parsed.EventId.HasValue)
            return await db.TrainingEvents.FirstOrDefaultAsync(x => x.Id == parsed.EventId.Value, ct);

        if (parsed.Kind == OccurrenceKind.Series && parsed.SeriesId.HasValue && parsed.OccurrenceDate.HasValue)
        {
            return await db.TrainingEvents.FirstOrDefaultAsync(x =>
                x.TrainingSeriesId == parsed.SeriesId.Value &&
                x.OccurrenceDate == parsed.OccurrenceDate.Value, ct);
        }

        return null;
    }

    private async Task<TrainingEvent?> GetOrMaterializeOccurrenceAsync(AppDbContext db, string occurrenceKey, CancellationToken ct)
    {
        var parsed = ParseOccurrenceKey(occurrenceKey);
        if (parsed is null)
            return null;

        if (parsed.Kind == OccurrenceKind.Single && parsed.EventId.HasValue)
            return await db.TrainingEvents.FirstOrDefaultAsync(x => x.Id == parsed.EventId.Value, ct);

        if (parsed.Kind != OccurrenceKind.Series || !parsed.SeriesId.HasValue || !parsed.OccurrenceDate.HasValue)
            return null;

        var existing = await db.TrainingEvents.FirstOrDefaultAsync(x =>
            x.TrainingSeriesId == parsed.SeriesId.Value &&
            x.OccurrenceDate == parsed.OccurrenceDate.Value, ct);
        if (existing is not null)
            return existing;

        var series = await db.TrainingSeries
            .Include(x => x.Exceptions)
            .FirstOrDefaultAsync(x => x.Id == parsed.SeriesId.Value, ct);
        if (series is null || !IsOccurrencePartOfSeries(series, parsed.OccurrenceDate.Value))
            return null;
        if (series.Exceptions.Any(x => x.OccurrenceDate == parsed.OccurrenceDate.Value && x.IsCancelled))
            return null;

        var created = new TrainingEvent
        {
            Title = series.Title,
            Description = series.Description,
            TrainingRoomId = series.TrainingRoomId,
            TrainingSeriesId = series.Id,
            OccurrenceDate = parsed.OccurrenceDate.Value,
            StartAt = parsed.OccurrenceDate.Value.ToDateTime(series.StartTime),
            EndAt = parsed.OccurrenceDate.Value.ToDateTime(series.EndTime),
            MaxParticipants = series.MaxParticipants,
            RecommendedParticipants = series.RecommendedParticipants,
            AllowMemberRegistration = series.AllowMemberRegistration,
            AllowGroupRegistration = series.AllowGroupRegistration,
            AllowParticipantEventSubmission = series.AllowParticipantEventSubmission
        };

        db.TrainingEvents.Add(created);
        await db.SaveChangesAsync(ct);
        return created;
    }

    private static IEnumerable<DateOnly> ExpandSeriesOccurrences(TrainingSeries series, DateOnly from, DateOnly to)
    {
        if (!series.IsActive)
            yield break;

        var current = series.StartDate;
        var until = series.UntilDate ?? to;

        while (current <= to && current <= until)
        {
            if (current >= from)
                yield return current;

            if (!string.Equals(series.RecurrencePattern, "weekly", StringComparison.OrdinalIgnoreCase))
                yield break;

            current = current.AddDays(Math.Max(1, series.RecurrenceInterval) * 7);
        }
    }

    private static bool IsOccurrencePartOfSeries(TrainingSeries series, DateOnly occurrenceDate)
    {
        if (!series.IsActive || occurrenceDate < series.StartDate)
            return false;
        if (series.UntilDate.HasValue && occurrenceDate > series.UntilDate.Value)
            return false;
        if (!string.Equals(series.RecurrencePattern, "weekly", StringComparison.OrdinalIgnoreCase))
            return false;

        var diffDays = occurrenceDate.DayNumber - series.StartDate.DayNumber;
        var intervalDays = Math.Max(1, series.RecurrenceInterval) * 7;
        return diffDays % intervalDays == 0;
    }

    private async Task<string?> ValidateRoomAvailabilityAsync(
        AppDbContext db,
        TrainingEventEditVm vm,
        int? excludeEventId = null,
        int? excludeSeriesId = null,
        DateOnly? candidateSeriesStartDate = null,
        CancellationToken ct = default)
    {
        if (!vm.IsRecurring)
        {
            var startAt = vm.Date.Date.Add(vm.StartTime.ToTimeSpan());
            var endAt = vm.Date.Date.Add(vm.EndTime.ToTimeSpan());

            var eventConflict = await db.TrainingEvents.AnyAsync(x =>
                x.TrainingRoomId == vm.TrainingRoomId &&
                (!excludeEventId.HasValue || x.Id != excludeEventId.Value) &&
                (!excludeSeriesId.HasValue || x.TrainingSeriesId != excludeSeriesId.Value) &&
                x.StartAt < endAt &&
                x.EndAt > startAt, ct);

            if (eventConflict)
                return "Der Raum ist in diesem Zeitraum bereits belegt.";

            var occurrenceDate = DateOnly.FromDateTime(vm.Date);
            var seriesConflict = await db.TrainingSeries
                .Where(x => x.IsActive &&
                            x.TrainingRoomId == vm.TrainingRoomId &&
                            (!excludeSeriesId.HasValue || x.Id != excludeSeriesId.Value))
                .ToListAsync(ct);

            if (seriesConflict.Any(x => SeriesContainsDate(x, occurrenceDate) && TimesOverlap(vm.StartTime, vm.EndTime, x.StartTime, x.EndTime)))
                return "Der Raum ist in diesem Zeitraum bereits durch eine Serie belegt.";

            return null;
        }

        var candidate = new TrainingSeries
        {
            TrainingRoomId = vm.TrainingRoomId,
            StartDate = candidateSeriesStartDate ?? DateOnly.FromDateTime(vm.Date),
            StartTime = vm.StartTime,
            EndTime = vm.EndTime,
            RecurrencePattern = vm.RecurrencePattern,
            RecurrenceInterval = Math.Max(1, vm.RecurrenceInterval),
            UntilDate = vm.RecurrenceUntil.HasValue ? DateOnly.FromDateTime(vm.RecurrenceUntil.Value) : null,
            IsActive = true
        };

        var candidateFrom = candidate.StartDate;
        var candidateUntil = candidate.UntilDate;
        var candidateFromStart = candidateFrom.ToDateTime(TimeOnly.MinValue);
        var candidateUntilEnd = candidateUntil?.ToDateTime(new TimeOnly(23, 59, 59));

        var conflictingEvents = await db.TrainingEvents
            .Where(x => x.TrainingRoomId == vm.TrainingRoomId &&
                        (!excludeEventId.HasValue || x.Id != excludeEventId.Value) &&
                        (!excludeSeriesId.HasValue || x.TrainingSeriesId != excludeSeriesId.Value) &&
                        x.StartAt >= candidateFromStart &&
                        (!candidateUntilEnd.HasValue || x.StartAt <= candidateUntilEnd.Value))
            .ToListAsync(ct);

        foreach (var item in conflictingEvents)
        {
            var occurrenceDate = item.OccurrenceDate ?? DateOnly.FromDateTime(item.StartAt);
            if (SeriesContainsDate(candidate, occurrenceDate) &&
                TimesOverlap(candidate.StartTime, candidate.EndTime, TimeOnly.FromDateTime(item.StartAt), TimeOnly.FromDateTime(item.EndAt)))
            {
                return "Der Raum ist durch einen bestehenden Termin bereits in der Serie belegt.";
            }
        }

        var conflictingSeries = await db.TrainingSeries
            .Where(x => x.IsActive &&
                        x.TrainingRoomId == vm.TrainingRoomId &&
                        (!excludeSeriesId.HasValue || x.Id != excludeSeriesId.Value))
            .ToListAsync(ct);

        if (conflictingSeries.Any(x => SeriesOverlaps(candidate, x)))
            return "Der Raum ist durch eine andere Serie bereits belegt.";

        return null;
    }

    private static string? ValidateEditVm(TrainingEventEditVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Title))
            return "Titel fehlt.";
        if (vm.TrainingRoomId <= 0)
            return "Bitte einen Raum wählen.";
        if (vm.EndTime <= vm.StartTime)
            return "Endzeit muss nach der Startzeit liegen.";
        if (vm.MaxParticipants < 0 || vm.RecommendedParticipants < 0)
            return "Teilnehmerzahlen dürfen nicht negativ sein.";

        return null;
    }

    private static void ApplySeriesValues(TrainingSeries series, TrainingEventEditVm vm)
    {
        series.Title = vm.Title.Trim();
        series.Description = (vm.Description ?? "").Trim();
        series.TrainingRoomId = vm.TrainingRoomId;
        series.StartDate = DateOnly.FromDateTime(vm.Date);
        series.StartTime = vm.StartTime;
        series.EndTime = vm.EndTime;
        series.RecurrencePattern = vm.RecurrencePattern;
        series.RecurrenceInterval = Math.Max(1, vm.RecurrenceInterval);
        series.UntilDate = vm.RecurrenceUntil.HasValue ? DateOnly.FromDateTime(vm.RecurrenceUntil.Value) : null;
        series.IsActive = true;
        series.MaxParticipants = vm.MaxParticipants;
        series.RecommendedParticipants = vm.RecommendedParticipants;
        series.AllowMemberRegistration = vm.AllowMemberRegistration;
        series.AllowGroupRegistration = vm.AllowGroupRegistration;
        series.AllowParticipantEventSubmission = vm.AllowParticipantEventSubmission;
    }

    private static bool SeriesContainsDate(TrainingSeries series, DateOnly occurrenceDate)
    {
        return IsOccurrencePartOfSeries(series, occurrenceDate);
    }

    private static bool TimesOverlap(TimeOnly startA, TimeOnly endA, TimeOnly startB, TimeOnly endB)
    {
        return startA < endB && endA > startB;
    }

    private static bool SeriesOverlaps(TrainingSeries a, TrainingSeries b)
    {
        if (!string.Equals(a.RecurrencePattern, "weekly", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(b.RecurrencePattern, "weekly", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (a.StartDate.DayOfWeek != b.StartDate.DayOfWeek)
            return false;
        if (!TimesOverlap(a.StartTime, a.EndTime, b.StartTime, b.EndTime))
            return false;

        var overlapStart = a.StartDate > b.StartDate ? a.StartDate : b.StartDate;
        var overlapEnd = MinDate(a.UntilDate, b.UntilDate);
        if (overlapEnd.HasValue && overlapEnd.Value < overlapStart)
            return false;

        var intervalA = Math.Max(1, a.RecurrenceInterval);
        var intervalB = Math.Max(1, b.RecurrenceInterval);
        var gcd = GreatestCommonDivisor(intervalA, intervalB);
        var weekOffset = (b.StartDate.DayNumber - a.StartDate.DayNumber) / 7;
        if (Math.Abs(weekOffset) % gcd != 0)
            return false;

        var lcm = LeastCommonMultiple(intervalA, intervalB);
        DateOnly? firstSharedDate = null;

        for (var weeksFromA = 0; weeksFromA < lcm; weeksFromA += intervalA)
        {
            if (Modulo(weeksFromA - weekOffset, intervalB) == 0)
            {
                firstSharedDate = a.StartDate.AddDays(weeksFromA * 7);
                break;
            }
        }

        if (!firstSharedDate.HasValue)
            return false;

        var sharedDate = firstSharedDate.Value;
        var jumpDays = lcm * 7;
        while (sharedDate < overlapStart)
            sharedDate = sharedDate.AddDays(jumpDays);

        return !overlapEnd.HasValue || sharedDate <= overlapEnd.Value;
    }

    private static DateOnly? MinDate(DateOnly? a, DateOnly? b)
    {
        if (!a.HasValue)
            return b;
        if (!b.HasValue)
            return a;
        return a.Value < b.Value ? a : b;
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return Math.Abs(a);
    }

    private static int LeastCommonMultiple(int a, int b)
    {
        return Math.Abs(a / GreatestCommonDivisor(a, b) * b);
    }

    private static int Modulo(int value, int divisor)
    {
        var result = value % divisor;
        return result < 0 ? result + divisor : result;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var current = date.Date;
        var diff = (7 + (current.DayOfWeek - DayOfWeek.Monday)) % 7;
        return current.AddDays(-diff);
    }

    private static string BuildSingleKey(int eventId) => $"single:{eventId}";

    private static string BuildSeriesKey(int seriesId, DateOnly occurrenceDate) => $"series:{seriesId}:{occurrenceDate:yyyyMMdd}";

    private static ParsedOccurrenceKey? ParseOccurrenceKey(string occurrenceKey)
    {
        if (string.IsNullOrWhiteSpace(occurrenceKey))
            return null;

        var parts = occurrenceKey.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && string.Equals(parts[0], "single", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(parts[1], out var eventId))
        {
            return new ParsedOccurrenceKey { Kind = OccurrenceKind.Single, EventId = eventId };
        }

        if (parts.Length == 3 &&
            string.Equals(parts[0], "series", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(parts[1], out var seriesId) &&
            DateOnly.TryParseExact(parts[2], "yyyyMMdd", out var occurrenceDate))
        {
            return new ParsedOccurrenceKey { Kind = OccurrenceKind.Series, SeriesId = seriesId, OccurrenceDate = occurrenceDate };
        }

        return null;
    }

    private sealed class ParsedOccurrenceKey
    {
        public OccurrenceKind Kind { get; init; }
        public int? EventId { get; init; }
        public int? SeriesId { get; init; }
        public DateOnly? OccurrenceDate { get; init; }
    }

    private enum OccurrenceKind
    {
        Single,
        Series
    }
}
