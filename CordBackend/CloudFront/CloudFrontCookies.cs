namespace CordBackend.CloudFront;

public class CloudFrontCookies
{
    public string Policy { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string KeyPairId { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
}