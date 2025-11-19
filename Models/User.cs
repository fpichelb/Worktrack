using System.ComponentModel.DataAnnotations;

namespace Worktrack.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string SecretCode { get; set; } = string.Empty;

    public Double ArchivedHours { get; set; } = 0;

    public bool ShareStats { get; set; } = true;
    public string Role { get; set; } = "user"; // admin / user

}
