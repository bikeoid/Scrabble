using Blazored.Modal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Scrabble.Client;
using Scrabble.Client.Data;
using Scrabble.Core.AI;
using Scrabble.Shared;
using Scrabble.Shared.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);


// Non-secured API request
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazoredModal();

//builder.Services.AddAuthorizationCore();
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

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
builder.Services.AddSingleton<Scrabble.Core.AI.ComputerPlayerAI>();


//await builder.Build().RunAsync();
var host = builder.Build();

var httpClient = host.Services.GetRequiredService<HttpClient>();

await WordLookupSingleton.InitializeWordListInstance(httpClient, host.Services.GetRequiredService<ComputerPlayerAI>(), "TWL06a.txt?v=4");

AuthCache.AuthHttpClient = httpClient;

await host.RunAsync();

