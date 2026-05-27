using Blazored.Modal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scrabble.Core.AI;
using Scrabble.Server;
using Scrabble.Server.Components;
using Scrabble.Server.Components.Account;
using Scrabble.Server.Hubs;
using Scrabble.Server.Data;
using Scrabble.Server.Services;
using Scrabble.Server.Utility;
using Microsoft.AspNetCore.Authorization;
using Scrabble.Shared.Auth;
using Scrabble.Shared;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();


builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddBlazoredModal();
builder.Services.AddControllersWithViews();

if (!string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientId"]))
{
    builder.Services.AddAuthentication().AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Connection string 'Authentication:Google:ClientId' not found.");
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Connection string 'Authentication:Google:ClientSecret' not found.");
    });
}

builder.Services.AddAuthorization();
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


builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("ScrabbleDbConnection") ?? throw new InvalidOperationException("Connection string 'ScrabbleDbConnection' not found.");
builder.Services.AddDbContext<ScrabbleDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ------- Begin Signing key storage in DB ------------------
builder.Services.AddDbContext<MyKeysContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<MyKeysContext>();
// ------- End Signing key storage ------------------

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

//builder.Services.AddHttpClient();
#if DEBUG
builder.Services.AddScoped(http => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7040")
});
#else
builder.Services.AddScoped(http => new HttpClient
{
    BaseAddress = new Uri("https://www.scrabble.example.com")
});
#endif

// Enable next 2 lines to use real email sender. Remember to set up the AuthMessageSenderOptions in appsettings.json and provide an implementation of IEmailSender<ApplicationUser>.
//builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration.GetSection("AuthMessageSenderOptions"));
//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, EmailSender>();

// Remove the next line to stop using the no-op email sender (which does nothing and is only for testing / development).
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddTransient<IMyEmailSender, MyEmailSender>();

builder.Services.AddSignalR();

builder.Services.AddSingleton<Scrabble.Core.AI.ComputerPlayerAI>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.MapStaticAssets();

app.UseRouting();
app.MapControllers();
app.MapHub<MoveHub>("/movehub");

app.UseAuthentication();
app.UseAuthorization(); 
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Scrabble.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

string rootpath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
string filePath = System.IO.Path.Combine(rootpath, "TWL06a.txt");
WordLookupSingleton.InitializeWordList(app.Services.GetRequiredService<ComputerPlayerAI>());

var scope = app.Services.CreateScope();
var client = scope.ServiceProvider.GetRequiredService<HttpClient>();
AuthCache.AuthHttpClient = client;

app.Run();



