namespace Worktrack.Models;

public class ImportBatch
{
    public int Id { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = "";
}
