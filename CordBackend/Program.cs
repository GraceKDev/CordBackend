using CordBackend.CloudFront;
using CordBackend.Data;
using CordBackend.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
builder.Services.AddDbContext<CordDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddOptions<CloudFrontOptions>()
    .Bind(builder.Configuration.GetSection("CloudFront"))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.DomainName),
        "CloudFront domain name is not configured.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.PublicKeyId),
        "CloudFront public key ID is not configured.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.PrivateKeyPath),
        "CloudFront private key path is not configured.")
    .ValidateOnStart();
builder.Services.AddScoped<CloudFrontCookieService>();
builder.Services.AddOptions<S3Options>()
    .Bind(builder.Configuration.GetSection(S3Options.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.BucketName),
        "S3 bucket name is not configured.")
    .Validate(
        options => options.PresignedUrlExpirationMinutes is >= 1 and <= 10080,
        "S3 presigned URL expiration must be between 1 and 10080 minutes.")
    .Validate(
        options => string.IsNullOrWhiteSpace(options.AccessKeyId)
            == string.IsNullOrWhiteSpace(options.SecretAccessKey),
        "S3 access key ID and secret access key must be configured together.")
    .ValidateOnStart();
builder.Services.AddSingleton<IImageStorageClient, S3ImageStorageClient>();

var webClientId = builder.Configuration["Authentication:Google:WebClientId"]
    ?? throw new InvalidOperationException("Google Web client ID is not configured.");

builder.Services.AddSingleton(new GoogleAuthSettings(webClientId));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Authority = "https://accounts.google.com";
        options.Audience = webClientId;
    });
builder.Services.AddAuthorization();
builder.WebHost.UseUrls("http://0.0.0.0:5104");
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CordBackend API v1");
    });
}



app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public sealed record GoogleAuthSettings(string WebClientId);
