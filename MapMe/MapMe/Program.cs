// Using fully qualified name for Blazor.Bootstrap to avoid namespace conflicts
using MapMe.Client.Pages;
using MapMe.Components;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using MapMe.Repositories;
using MapMe.DTOs;
using MapMe.Models;
using MapMe.Utils;
using Microsoft.Azure.Cosmos;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Add services to the container.
builder.Services.AddBlazorBootstrap();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    })
    .AddInteractiveWebAssemblyComponents();

// Add HttpContextAccessor for building request-based HttpClient base address
builder.Services.AddHttpContextAccessor();

// Register HttpClient for SSR/prerender so components that inject HttpClient work server-side
builder.Services.AddScoped(sp =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var req = accessor.HttpContext?.Request;
    // Fallback to configured URLs if no current request (e.g., background ops)
    var baseUri = req is not null
        ? $"{req.Scheme}://{req.Host}"
        : (Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Split(';', StringSplitOptions.RemoveEmptyEntries)[0]
            ?? "https://localhost:8008");
    return new HttpClient { BaseAddress = new Uri(baseUri) };
});

// Data access: prefer Cosmos if configured; else, in-memory fallback
var cosmosEndpoint = builder.Configuration["Cosmos:Endpoint"];
var cosmosKey = builder.Configuration["Cosmos:Key"];
var cosmosDatabase = builder.Configuration["Cosmos:Database"] ?? "mapme";
var useCosmos = !string.IsNullOrWhiteSpace(cosmosEndpoint) && !string.IsNullOrWhiteSpace(cosmosKey);

if (useCosmos)
{
    builder.Services.AddSingleton(sp =>
    {
        // Allow self-signed cert for local emulator endpoints
        var isLocal = cosmosEndpoint!.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                    || cosmosEndpoint.Contains("127.0.0.1");
        var options = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway
        };
        if (isLocal)
        {
            options.HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        }
        return new CosmosClient(cosmosEndpoint!, cosmosKey!, options);
    });
    builder.Services.AddSingleton(new CosmosContextOptions(cosmosDatabase));
    builder.Services.AddSingleton<IUserProfileRepository, CosmosUserProfileRepository>();
    builder.Services.AddSingleton<IDateMarkByUserRepository, CosmosDateMarkByUserRepository>();
}
else
{
    // Temporary in-memory repositories
    builder.Services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
    builder.Services.AddSingleton<IDateMarkByUserRepository, InMemoryDateMarkByUserRepository>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForErrors: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MapMe.Client._Imports).Assembly);

// Minimal API to provide client with Google Maps API key (prefer config/user-secrets over env var)
app.MapGet("/config/maps", (HttpContext http) =>
{
    // Prefer configuration first (includes appsettings and User Secrets in Development)
    var apiKey = app.Configuration["GoogleMaps:ApiKey"];
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        // Fallback to environment variable
        apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
    }

    return Results.Ok(new { ApiKey = apiKey });
});

// Profiles API
app.MapPost("/api/profiles", async (CreateProfileRequest req, IUserProfileRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(req.Id) || string.IsNullOrWhiteSpace(req.UserId) || string.IsNullOrWhiteSpace(req.DisplayName))
        return Results.BadRequest("Id, UserId and DisplayName are required");
    var now = DateTimeOffset.UtcNow;
    var profile = req.ToProfile(now);
    await repo.UpsertAsync(profile);
    return Results.Created($"/api/profiles/{profile.Id}", profile);
});

app.MapGet("/api/profiles/{id}", async (string id, IUserProfileRepository repo) =>
{
    var profile = await repo.GetByIdAsync(id);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

// DateMarks API
app.MapPost("/api/datemarks", async (UpsertDateMarkRequest req, IDateMarkByUserRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(req.Id) || string.IsNullOrWhiteSpace(req.UserId))
        return Results.BadRequest("Id and UserId are required");
    var now = DateTimeOffset.UtcNow;
    var mark = req.ToDateMark(now);
    await repo.UpsertAsync(mark);
    return Results.Created($"/api/datemarks/{mark.Id}", mark);
});

app.MapGet("/api/users/{userId}/datemarks", async (
    string userId,
    DateOnly? from,
    DateOnly? to,
    string[]? categories,
    string[]? tags,
    string[]? qualities,
    IDateMarkByUserRepository repo,
    CancellationToken ct) =>
{
    var cats = categories is { Length: > 0 } ? Normalization.ToNorm(categories!) : Array.Empty<string>();
    var tgs = tags is { Length: > 0 } ? Normalization.ToNorm(tags!) : Array.Empty<string>();
    var qls = qualities is { Length: > 0 } ? Normalization.ToNorm(qualities!) : Array.Empty<string>();
    var list = new List<DateMark>();
    await foreach (var dm in repo.GetByUserAsync(userId, from, to, cats, tgs, qls, ct))
    {
        list.Add(dm);
    }
    return Results.Ok(list);
});

// Map viewport query (prototype: radius around lat/lng); later switch to bbox & geohash prefixes
app.MapGet("/api/map/datemarks", async (
    double lat,
    double lng,
    double radiusMeters,
    string[]? categories,
    string[]? tags,
    string[]? qualities,
    IDateMarkByUserRepository repo,
    CancellationToken ct) =>
{
    // For prototype, scan all in-memory marks; will be replaced by DateMarksGeo + prefixes
    var cats = categories is { Length: > 0 } ? Normalization.ToNorm(categories!) : Array.Empty<string>();
    var tgs = tags is { Length: > 0 } ? Normalization.ToNorm(tags!) : Array.Empty<string>();
    var qls = qualities is { Length: > 0 } ? Normalization.ToNorm(qualities!) : Array.Empty<string>();

    // In-memory store is per user; aggregate across users
    var results = new List<DateMark>();
    // Access internal repo data is not exposed; for prototype we iterate by known users via reflection is overkill.
    // Instead, require a userId for now or expand repository later. Here we just return empty to keep API shape stable.
    return Results.Ok(results);
});

app.Run()
;

// Expose Program for WebApplicationFactory in tests
public partial class Program { }