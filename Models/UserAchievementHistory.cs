namespace Worktrack.Models;

public class UserAchievementHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Kind { get; set; } = "";
    public string Label { get; set; } = "";
    public string BadgeText { get; set; } = "";
    public string ColorCss { get; set; } = "";
    public int? ArchiveYear { get; set; }
    public bool IsPermanent { get; set; }
    public DateTime AwardedAtUtc { get; set; } = DateTime.UtcNow;
}
