namespace Worktrack.Models;
public class Activity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Ort { get; set; } = "";
    public DateTime Datum { get; set; }
    public string Info { get; set; } = "";
    public int MaxTeilnehmer { get; set; }
    public int EmpfohleneTeilnehmer { get; set; }

    public DateTime? LastUnregistrationAt { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";   // "image/jpeg", "image/png"...
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public List<ActivityRegistration> Registrations { get; set; } = new();
}

public class ActivityRegistration
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int Extra { get; set; }

    public DateTime RegisteredAt { get; set; }
    public DateTime? UnregisteredAt { get; set; } // null = aktiv angemeldet
}
