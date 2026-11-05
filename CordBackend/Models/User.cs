using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CordBackend.Models;

[Table("users")]
[Index(nameof(GoogleSubject), IsUnique = true, Name = "ix_users_google_subject")]
public class User
{
    [Column("id")]
    public int Id { get; set; }

    [Column("google_subject")]
    public required string GoogleSubject { get; set; }

    [Column("email")]
    public required string Email { get; set; }

    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [Column("last_login_at_utc")]
    public DateTimeOffset LastLoginAtUtc { get; set; }


    public ICollection<Coordinate> Coordinates { get; set; } = new List<Coordinate>();
    public ICollection<Route> Routes {get;set;} = new List<Route>();
}