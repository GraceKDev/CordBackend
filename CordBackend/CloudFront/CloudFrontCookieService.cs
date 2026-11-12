using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CordBackend.CloudFront;

public sealed class CloudFrontCookieService : IDisposable
{
    // CloudFront only accepts RSA-SHA1 signatures for signed URLs and signed cookies.
    private static readonly HashAlgorithmName SignatureHashAlgorithm = HashAlgorithmName.SHA1;

    private readonly CloudFrontOptions _options;
    private readonly RSA _privateKey;

    public CloudFrontCookieService(
        IOptions<CloudFrontOptions> options,
        IHostEnvironment environment)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.PublicKeyId))
        {
            throw new InvalidOperationException("CloudFront public key ID is not configured.");
        }

        var privateKeyPath = ResolvePrivateKeyPath(_options.PrivateKeyPath, environment.ContentRootPath);
        _privateKey = RSA.Create();
        _privateKey.ImportFromPem(File.ReadAllText(privateKeyPath));
    }

    public CloudFrontCookies GenerateCookies(string resource, TimeSpan duration)
    {
        var expiration = DateTimeOffset.UtcNow.Add(duration);
        var policy = new Dictionary<string, object>
        {
            ["Statement"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["Resource"] = resource,
                    ["Condition"] = new Dictionary<string, object>
                    {
                        ["DateLessThan"] = new Dictionary<string, object>
                        {
                            ["AWS:EpochTime"] = expiration.ToUnixTimeSeconds()
                        }
                    }
                }
            }
        };

        var policyJson = JsonSerializer.Serialize(policy);
        var policyBytes = Encoding.UTF8.GetBytes(policyJson);

        var signature = _privateKey.SignData(
            policyBytes,
            SignatureHashAlgorithm,
            RSASignaturePadding.Pkcs1);

        return new CloudFrontCookies
        {
            Policy = ToCloudFrontBase64(policyBytes),
            Signature = ToCloudFrontBase64(signature),
            KeyPairId = _options.PublicKeyId,
            ExpiresAt = expiration.ToUnixTimeSeconds()
        };
    }

    public void Dispose() => _privateKey.Dispose();

    private static string ResolvePrivateKeyPath(string configuredPath, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException("CloudFront private key path is not configured.");
        }

        var path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(contentRootPath, configuredPath);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"CloudFront private key was not found at '{path}'.", path);
        }

        return path;
    }

    private static string ToCloudFrontBase64(byte[] data)
    {
        var base64 = Convert.ToBase64String(data);
        return base64.Replace('+', '-').Replace('=', '_').Replace('/', '~');
    }
}
