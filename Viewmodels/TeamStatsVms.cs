using Worktrack.Models;
using Worktrack.Services;

namespace Worktrack.ViewModels;

public class TeamStatsVm
{
    public List<TeamGlobalUserStatsVm> GlobalStats { get; set; } = new();
    public List<TeamSeasonGroupVm> SeasonGroups { get; set; } = new();
}

public class TeamGlobalUserStatsVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Rank { get; set; }
    public int EventsVisited { get; set; }
    public double TotalHours { get; set; }
    public int Bonus { get; set; }
    public List<TeamAchievementVm> Achievements { get; set; } = new();
}

public class TeamSeasonGroupVm
{
    public Season Season { get; set; } = new();
    public List<Event> Events { get; set; } = new();
    public List<EventUserStats> Podium { get; set; } = new();
}

public class TeamAchievementVm
{
    public string Shown { get; set; } = "";
    public string Tooltip { get; set; } = "";
    public string ColorCss { get; set; } = "text-bg-light";
}
