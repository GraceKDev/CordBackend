using CordBackend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
builder.Services.AddDbContext<CordDbContext>(options => options.UseNpgsql(connectionString));

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
