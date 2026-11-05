namespace CordBackend.DTOs;

/// <summary>A coordinate included in an export or import.</summary>
public class CoordinateDTO
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Date { get; set; } = string.Empty;
}