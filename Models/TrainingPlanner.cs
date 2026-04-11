namespace Worktrack.Models;

public class TrainingRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#2563eb";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<TrainingEvent> Events { get; set; } = new();
    public List<TrainingSeries> Series { get; set; } = new();
}

public class TrainingEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int TrainingRoomId { get; set; }
    public TrainingRoom TrainingRoom { get; set; } = null!;
    public bool AppliesToAllRooms { get; set; }
    public int? TrainingSeriesId { get; set; }
    public TrainingSeries? TrainingSeries { get; set; }
    public DateOnly? OccurrenceDate { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsAllDay { get; set; }
    public int MaxParticipants { get; set; } =  0;
    public int RecommendedParticipants { get; set; } = 0;
    public bool AllowMemberRegistration { get; set; } = false;
    public bool AllowGroupRegistration { get; set; } = false;
    public bool AllowParticipantEventSubmission { get; set; } = false;

    public List<TrainingEventParticipant> Participants { get; set; } = new();
}

public class TrainingSeries
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int TrainingRoomId { get; set; }
    public TrainingRoom TrainingRoom { get; set; } = null!;
    public bool AppliesToAllRooms { get; set; }
    public DateOnly StartDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAllDay { get; set; }
    public string RecurrencePattern { get; set; } = "weekly";
    public int RecurrenceInterval { get; set; } = 1;
    public DateOnly? UntilDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxParticipants { get; set; } = 0;
    public int RecommendedParticipants { get; set; } = 0;
    public bool AllowMemberRegistration { get; set; } = false;
    public bool AllowGroupRegistration { get; set; } = false;
    public bool AllowParticipantEventSubmission { get; set; } = false;

    public List<TrainingEvent> MaterializedEvents { get; set; } = new();
    public List<TrainingSeriesException> Exceptions { get; set; } = new();
}

public class TrainingSeriesException
{
    public int Id { get; set; }
    public int TrainingSeriesId { get; set; }
    public TrainingSeries TrainingSeries { get; set; } = null!;
    public DateOnly OccurrenceDate { get; set; }
    public bool IsCancelled { get; set; } = true;
    public string Reason { get; set; } = "";
}

public class TrainingEventParticipant
{
    public int Id { get; set; }
    public int TrainingEventId { get; set; }
    public TrainingEvent TrainingEvent { get; set; } = null!;
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string DisplayName { get; set; } = "";
    public string ParticipantType { get; set; } = "member";
    public int ParticipantCount { get; set; } = 1;
    public string SourceGroupName { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime RegisteredAt { get; set; }
}
