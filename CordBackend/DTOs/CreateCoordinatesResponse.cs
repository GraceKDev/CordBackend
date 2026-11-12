namespace CordBackend.DTOs;

/// <summary>Contains the saved coordinates, routes, and upload URLs for any newly added images.</summary>
public sealed class CreateCoordinatesResponse
{
    public List<RouteDTO> Routes { get; set; } = [];
    public List<CoordinateDTO> Coordinates { get; set; } = [];
    public List<ImageUploadDTO> ImageUploads { get; set; } = [];
}

/// <summary>A presigned URL the client can use to upload an image for a coordinate.</summary>
public sealed class ImageUploadDTO
{
    /// <summary>Identifier persisted on the coordinate that owns this image.</summary>
    public string ImageId { get; set; } = string.Empty;

    /// <summary>Presigned URL to PUT the image bytes to.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>When <see cref="Url"/> stops being valid.</summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
