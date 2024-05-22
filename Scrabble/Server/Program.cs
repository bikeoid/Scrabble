using Blazored.Modal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
//using Scrabble.Client.Pages;
using Scrabble.Server.Components;
using Scrabble.Server.Components.Account;
using Scrabble.Server.Hubs;
using Scrabble.Server.Data;
using Scrabble.Server.Services;
using Scrabble.Server.Utility;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Scrabble.Shared.Auth;
using Scrabble.Shared;
using Microsoft.AspNetCore.DataProtection;


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

//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration.GetSection("AuthMessageSenderOptions"));
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, EmailSender>();
builder.Services.AddTransient<IMyEmailSender, MyEmailSender>();

builder.Services.AddSignalR();

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

app.UseHttpsRedirection();

app.UseStaticFiles();

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
WordLookupSingleton.InitializeWordList(filePath);

var scope = app.Services.CreateScope();
var client = scope.ServiceProvider.GetRequiredService<HttpClient>();
AuthCache.AuthHttpClient = client;

app.Run();



