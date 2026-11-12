namespace CordBackend.Storage;

/// <summary>Issues presigned URLs that let clients upload images directly to object storage.</summary>
public interface IImageStorageClient
{
    /// <summary>Creates a presigned PUT URL for each requested image.</summary>
    /// <param name="imageIds">Identifiers used to build the object keys.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>One result per image ID, in the same order as <paramref name="imageIds"/>.</returns>
    Task<IReadOnlyList<PresignedImageUpload>> CreatePresignedPutUrlsAsync(
        string sub,
        IReadOnlyList<string> imageIds,
        CancellationToken cancellationToken = default);

    /// <summary>Builds the URL for an image stored in the bucket.</summary>
    /// <param name="sub">The user's Google subject, which prefixes the object key.</param>
    /// <param name="imageId">Identifier of the image.</param>
    /// <returns>The public URL of the image object.</returns>
    string GetImageUrl(string sub, string imageId);
}

/// <summary>A presigned PUT URL the client can use to upload an image to the bucket.</summary>
/// <param name="ImageId">Identifier of the image.</param>
/// <param name="ObjectKey">Key of the image object in the bucket.</param>
/// <param name="Url">Presigned URL to PUT the image bytes to.</param>
/// <param name="ExpiresAtUtc">When <paramref name="Url"/> stops being valid.</param>
public sealed record PresignedImageUpload(
    string ImageId,
    string ObjectKey,
    string Url,
    DateTimeOffset ExpiresAtUtc);
