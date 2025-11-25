namespace Worktrack.Data;
public class Impressum
{
    public int Id { get; set; }

    public string ResponsibleName { get; set; } = "";      // Verantwortlicher
    public string Address { get; set; } = "";             // Anschrift
    public string Email { get; set; } = "";               // Kontakt E-Mail
    public string Phone { get; set; } = "";               // optional
    public string HostingProvider { get; set; } = "";     // z.B. Hetzner
    public string Notes { get; set; } = "";               // z.B. Haftungsausschluss
}