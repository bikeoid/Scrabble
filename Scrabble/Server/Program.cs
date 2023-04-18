using Blazored.Modal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.Identity.Web;
using Scrabble.Server.Hubs;
//using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Scrabble.Server.Data;
using Scrabble.Server.Utility;
using Scrabble.Server.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Runtime.ConstrainedExecution;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = false; // true would be a security risk
});

var connectionString = builder.Configuration.GetConnectionString("ScrabbleDbConnection") ?? throw new InvalidOperationException("Connection string 'ScrabbleDbConnection' not found.");
builder.Services.AddDbContext<ScrabbleDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

var programExecutable = System.Reflection.Assembly.GetExecutingAssembly().Location;
var programPath = System.IO.Path.GetDirectoryName(programExecutable);
var keyDbPath = Path.Combine(programPath, @"..\db\k");

builder.Services.AddIdentityServer()
    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

if (!string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientId"]))
{
    builder.Services.AddAuthentication().AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Connection string 'Authentication:Google:ClientId' not found.");
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Connection string 'Authentication:Google:ClientSecret' not found.");
    });
}

builder.Services.AddAuthentication();
  //  .AddIdentityServerJwt(); // https://stackoverflow.com/questions/63661946/how-do-i-authenticate-a-user-in-serverside-controller-in-a-blazor-webassembly-pr
//.AddJwtBearer("Bearer", options =>
// {
//     options.Audience = "api1";
//     options.Authority = "https://localhost:5000";
// });


builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddSignalR();


builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

builder.Services.AddBlazoredModal();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseResponseCompression();
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();





app.UseRouting();

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<MoveHub>("/movehub");
app.MapFallbackToFile("index.html");


string rootpath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
string filePath = System.IO.Path.Combine(rootpath, "TWL06a.txt");
WordLookupSingleton.InitializeWordList(filePath);


app.Run();




