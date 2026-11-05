namespace CordBackend.DTOs;

/// <summary>Contains a user's standalone coordinates and routes for export or import.</summary>
public sealed class CoordinateExportDTO
{
    public List<RouteDTO> Routes { get; set; } = [];
    public List<CoordinateDTO> Coordinates { get; set; } = [];
}