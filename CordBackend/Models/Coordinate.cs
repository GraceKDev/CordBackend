using System.ComponentModel.DataAnnotations.Schema;

namespace CordBackend.Models;

[Table("coordinates")]
public class Coordinate
{
    [Column("id")]
    public int Id { get; set; }

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("date")]
    
    public string Date { get; set; } = string.Empty;
    [Column("has_image")]
    public bool HasImage { get; set; } = false;
    [Column("uploaded_successfully")]
    public bool UploadedSuccessfully { get; set; } = false;
    [Column("image_id")]
    public string? ImageId { get; set; } = string.Empty;

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [ForeignKey(nameof(User))]
    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey(nameof(Route))]
    [Column("route_id")]
    public int? RouteId { get; set; }

    public User User { get; set; } = null!;
    public Route? Route { get; set; }
}