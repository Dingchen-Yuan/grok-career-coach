using System.Text;
using Azure.Identity;
using GrokCareerCoach.Api.Data;
using GrokCareerCoach.Api.Options;
using GrokCareerCoach.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["KeyVault:Uri"]
    ?? Environment.GetEnvironmentVariable("KEY_VAULT_URI");
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

var postgresConnection = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Postgres is required.");
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";
var jwtOptions = builder.Configuration
    .GetRequiredSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is required.");
var grokOptions = builder.Configuration
    .GetSection(GrokOptions.SectionName)
    .Get<GrokOptions>()
    ?? new GrokOptions();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnection, name: "postgres")
    .AddRedis(redisConnection, name: "redis");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "grok-career-coach:";
});
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<GoogleOptions>()
    .BindConfiguration(GoogleOptions.SectionName);
builder.Services.AddOptions<GrokOptions>()
    .BindConfiguration(GrokOptions.SectionName);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub"
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
if (grokOptions.HasApiKey)
{
    builder.Services.AddHttpClient<IGrokClient, GrokClient>((_, client) =>
    {
        client.BaseAddress = new Uri(
            grokOptions.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(60);
    });
}
else
{
    builder.Services.AddScoped<IGrokClient, MockGrokClient>();
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var csv = builder.Configuration["Cors:AllowedOriginsCsv"];
        string[] origins;
        if (!string.IsNullOrWhiteSpace(csv))
        {
            origins = csv.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries
                    | StringSplitOptions.TrimEntries);
        }
        else
        {
            origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? ["http://localhost:5173"];
        }

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
