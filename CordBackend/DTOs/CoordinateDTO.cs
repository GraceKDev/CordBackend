using System.ComponentModel.DataAnnotations;

namespace CordBackend.DTOs;

/// <summary>A coordinate included in an export or import.</summary>
public class CoordinateDTO
{
    [Range(-90d, 90d)]
    public double Latitude { get; set; }
    [Range(-180d, 180d)]
    public double Longitude { get; set; }
    public string Date { get; set; } = string.Empty;
    public bool HasImage { get; set; } = false;
    public string? ImageId { get; set; }
    public string? ImageUrl { get; set; }
    public bool UploadedSuccessfully { get; set; } = false;
}