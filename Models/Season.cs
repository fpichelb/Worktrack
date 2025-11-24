namespace Worktrack.Models;

public class Season
{
    public int Id { get; set; }

    // Name der Season – z.B. "Karnevals-Saison"
    public string Name { get; set; } = string.Empty;

    // Text-Vorkommen im Eventnamen oder Eventbeschreibung
    public string? Snippet { get; set; }

    // Kommagetrennte Liste z.B. "1,2,3" für Jan–März
    public string? Months { get; set; }

    // Hilfseigenschaft für Razor
    public List<int> MonthList =>
        string.IsNullOrWhiteSpace(Months)
            ? new List<int>()
            : Months.Split(',').Select(int.Parse).ToList();
}
