namespace CordBackend.DTOs;

/// <summary>A route and its ordered coordinates in an export or import.</summary>
public class RouteDTO
{
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public List<CoordinateDTO> Coordinates { get; set; } = new List<CoordinateDTO>();
}