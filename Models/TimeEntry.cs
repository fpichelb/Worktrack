using System.ComponentModel.DataAnnotations;

namespace Worktrack.Models;

public class TimeEntry
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public string? UserName { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    [MaxLength(200)]
    public string NameEntered { get; set; }

    [MaxLength(500)]
    public string? Task { get; set; }

    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }

    public double? DurationHours { get; set; }

    public bool IsArchived { get; set; } = false;

    public string Status { get; set; } = "pending_assignment";

    // für automatische Zuordnung (wenn Name matcht)
    [MaxLength(100)]
    public string? MatchedUserName { get; set; }
}
