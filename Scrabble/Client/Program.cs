using Blazored.Modal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Scrabble.Client;
using Scrabble.Client.Auth;
using Scrabble.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("Scrabble.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Scrabble.ServerAPI"));


// Non-secured API request
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazoredModal();

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(Policies.IsAdmin, policy =>
        policy.Requirements.Add(new AdminRequirement()));
    options.AddPolicy(Policies.IsPlayer, policy =>
        policy.Requirements.Add(new PlayerRequirement()));
});
builder.Services.AddSingleton<IAuthorizationHandler, AdminHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PlayerHandler>();


builder.Services.AddApiAuthorization();

builder.Logging.SetMinimumLevel(LogLevel.Warning);

var host = builder.Build();

//var logger = host.Services.GetRequiredService<ILoggerFactory>()
//    .CreateLogger<Program>();

var client = host.Services.GetRequiredService<HttpClient>();

AuthCache.AuthHttpClient = client;

await host.RunAsync();


