// Using fully qualified name for Blazor.Bootstrap to avoid namespace conflicts
using MapMe.Client.Pages;
using MapMe.Components;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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

app.Run();