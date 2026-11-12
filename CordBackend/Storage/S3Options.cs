namespace CordBackend.Storage;

/// <summary>Configuration for the S3-compatible image bucket.</summary>
public sealed class S3Options
{
    public const string SectionName = "S3";

    /// <summary>Name of the bucket that stores uploaded images.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>AWS region, or the region used by an S3-compatible service.</summary>
    public string? Region { get; set; }

    /// <summary>
    /// Optional custom endpoint for S3-compatible services such as MinIO or R2.
    /// Leave empty to use the AWS S3 endpoint for <see cref="Region"/>.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Optional explicit access key. Omit to use the default AWS credential chain.</summary>
    public string? AccessKeyId { get; set; }

    /// <summary>Optional explicit secret key. Omit to use the default AWS credential chain.</summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>Use path-style URLs, which S3-compatible services typically require.</summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>Prefix prepended to every image object key.</summary>
    public string KeyPrefix { get; set; } = "images";



    /// <summary>Lifetime of a presigned upload URL, in minutes.</summary>
    public int PresignedUrlExpirationMinutes { get; set; } = 15;
}
