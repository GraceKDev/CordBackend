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
}