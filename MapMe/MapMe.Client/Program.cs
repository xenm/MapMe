using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MapMe.Client.Services;
using System;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register HttpClient for DI so components (e.g., Map.razor) can inject it
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<ChatService>();

await builder.Build().RunAsync();