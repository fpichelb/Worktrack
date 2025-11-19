using System.ComponentModel.DataAnnotations;

namespace Worktrack.Models;

public class Event
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Wenn true, werden neue Namen automatisch Benutzern zugeordnet
    /// </summary>
    public bool AutoMatchUsers { get; set; } = true;

    /// <summary>
    /// Wenn true, muss beim Check-in eine Aufgabenbeschreibung angegeben werden
    /// </summary>
    public bool RequireTask { get; set; } = false;

    /// <summary>
    /// Standardstunden für Auto-Checkout (z. B. nach 2h, falls kein manueller Checkout erfolgt)
    /// </summary>
    [Range(0.5, 24)]
    public double DefaultHours { get; set; } = 2;

    /// <summary>
    /// Zeitpunkt, wann das Event begonnen hat (optional)
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Zeitpunkt, wann das Event endet (optional)
    /// </summary>
    public DateTime? EndTime { get; set; }

    public string? Season { get; set; }
}
