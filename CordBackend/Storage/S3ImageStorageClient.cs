using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CordBackend.CloudFront;
using Microsoft.Extensions.Options;

namespace CordBackend.Storage;

/// <summary>Issues presigned upload URLs against an S3-compatible bucket.</summary>
public sealed class S3ImageStorageClient : IImageStorageClient, IDisposable
{
    private readonly AmazonS3Client _s3Client;
    private readonly S3Options _options;
    private readonly CloudFrontOptions _cloudFrontOptions;

    public S3ImageStorageClient(
        IOptions<S3Options> options,
        IOptions<CloudFrontOptions> cloudFrontOptions)
    {
        _options = options.Value;
        _cloudFrontOptions = cloudFrontOptions.Value;
        _s3Client = CreateS3Client(_options);
    }

    public Task<IReadOnlyList<PresignedImageUpload>> CreatePresignedPutUrlsAsync(
        string sub,
        IReadOnlyList<string> imageIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Sign the whole batch with the same expiry so URLs are easy to refresh together.
        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_options.PresignedUrlExpirationMinutes);
        var uploads = new List<PresignedImageUpload>(imageIds.Count);

        foreach (var imageId in imageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var objectKey = BuildImageObjectKey(sub, imageId);
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = expiresAtUtc.UtcDateTime
            };
            uploads.Add(new PresignedImageUpload(
                imageId,
                objectKey,
                _s3Client.GetPreSignedURL(request),
                expiresAtUtc));
        }

        return Task.FromResult<IReadOnlyList<PresignedImageUpload>>(uploads);
    }

    public string GetImageUrl(string sub, string imageId)
    {
        var objectKey = BuildImageObjectKey(sub, imageId);
        var encodedKey = string.Join(
            '/',
            objectKey
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
        var cloudFrontDomain = _cloudFrontOptions.DomainName.TrimEnd('/');
        return $"https://{cloudFrontDomain}/{encodedKey}";
    }

    public void Dispose() => _s3Client.Dispose();

    private string BuildImageObjectKey(string sub, string imageId)
    {
        var keyPrefix = _options.KeyPrefix.Trim('/');
        var userPrefix = (string.IsNullOrWhiteSpace(sub) ? "default" : sub).Trim('/');

        return string.IsNullOrEmpty(keyPrefix)
            ? $"{userPrefix}/{imageId}"
            : $"{userPrefix}/{keyPrefix}/{imageId}";
    }

    private static string GetRegion(S3Options options) => string.IsNullOrWhiteSpace(options.Region)
        ? "ap-southeast-7"
        : options.Region;

    private static AmazonS3Client CreateS3Client(S3Options options)
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
            config.AuthenticationRegion = GetRegion(options);
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(GetRegion(options));
        }

        if (string.IsNullOrWhiteSpace(options.AccessKeyId) || string.IsNullOrWhiteSpace(options.SecretAccessKey))
        {
            return new AmazonS3Client(config);
        }

        return new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
            config);
    }
}
