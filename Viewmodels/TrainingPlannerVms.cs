namespace Worktrack.ViewModels;

public class TrainingRoomVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#2563eb";
}

public class TrainingWeekVm
{
    public DateTime WeekStart { get; set; }
    public List<DateTime> Days { get; set; } = new();
    public List<TrainingRoomVm> Rooms { get; set; } = new();
    public List<TrainingWeekEventVm> Events { get; set; } = new();
}

public class TrainingMonthVm
{
    public DateTime MonthStart { get; set; }
    public DateTime GridStart { get; set; }
    public List<TrainingMonthDayVm> Days { get; set; } = new();
}

public class TrainingMonthDayVm
{
    public DateTime Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public List<TrainingWeekEventVm> Events { get; set; } = new();
}

public class TrainingWeekEventVm
{
    public string OccurrenceKey { get; set; } = "";
    public int? EventId { get; set; }
    public int? SeriesId { get; set; }
    public string Title { get; set; } = "";
    public int TrainingRoomId { get; set; }
    public string RoomName { get; set; } = "";
    public string RoomColor { get; set; } = "#2563eb";
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool AppliesToAllRooms { get; set; }
    public bool IsAllDay { get; set; }
    public int ParticipantCount { get; set; }
    public int MaxParticipants { get; set; }
    public bool IsCurrentUserRegistered { get; set; }
    public bool AllowMemberRegistration { get; set; }
    public bool AllowGroupRegistration { get; set; }
    public bool AllowParticipantEventSubmission { get; set; }
    public bool IsRecurring { get; set; }
    public bool HasMaterializedEvent { get; set; }
}

public class TrainingEventDetailVm
{
    public string OccurrenceKey { get; set; } = "";
    public int? EventId { get; set; }
    public int? SeriesId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int TrainingRoomId { get; set; }
    public string RoomName { get; set; } = "";
    public string RoomColor { get; set; } = "#2563eb";
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool AppliesToAllRooms { get; set; }
    public bool IsAllDay { get; set; }
    public int MaxParticipants { get; set; }
    public int RecommendedParticipants { get; set; }
    public int ParticipantCount { get; set; }
    public string RecurrencePattern { get; set; } = "none";
    public DateTime? RecurrenceUntil { get; set; }
    public DateTime OccurrenceDate { get; set; }
    public bool AllowMemberRegistration { get; set; }
    public bool AllowGroupRegistration { get; set; }
    public bool AllowParticipantEventSubmission { get; set; }
    public bool IsCurrentUserRegistered { get; set; }
    public bool IsRecurring { get; set; }
    public bool HasMaterializedEvent { get; set; }
    public List<TrainingParticipantVm> Participants { get; set; } = new();
}

public class TrainingParticipantVm
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string ParticipantType { get; set; } = "";
    public int ParticipantCount { get; set; }
    public string SourceGroupName { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime RegisteredAt { get; set; }
}

public class TrainingEventEditVm
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int TrainingRoomId { get; set; }
    public bool AppliesToAllRooms { get; set; }
    public bool IsAllDay { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    public TimeOnly StartTime { get; set; } = new(18, 0);
    public TimeOnly EndTime { get; set; } = new(19, 30);
    public int MaxParticipants { get; set; }
    public int RecommendedParticipants { get; set; }
    public bool AllowMemberRegistration { get; set; } = true;
    public bool AllowGroupRegistration { get; set; }
    public bool AllowParticipantEventSubmission { get; set; }
    public bool IsRecurring { get; set; }
    public string RecurrencePattern { get; set; } = "weekly";
    public int RecurrenceInterval { get; set; } = 1;
    public DateTime? RecurrenceUntil { get; set; }
    public int RepeatCount { get; set; } = 8;
}

public class TrainingParticipantEditVm
{
    public int? UserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string ParticipantType { get; set; } = "member";
    public int ParticipantCount { get; set; } = 1;
    public string SourceGroupName { get; set; } = "";
    public string Notes { get; set; } = "";
}
