using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Services;
using Worktrack.Components;
using Worktrack.Components.Provider;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server;
using Worktrack.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "UserLoginCookie";
        options.LoginPath = "/user/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();
// Wichtig: AuthenticationStateProvider registrieren
builder.Services.AddHttpClient();
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    provider => provider.GetRequiredService<AuthStateProvider>()
);

// ------------------------------------------
//  1. Razor Components aktivieren (Blazor Server)
// ------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ------------------------------------------
//  2. MySQL-Datenbank konfigurieren
// ------------------------------------------
// Lies die Connection aus appsettings.json oder nutze Fallback
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "server=localhost;database=worktrack;user=worktrackUser;password=Passwort123;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
// ------------------------------------------
//  3. Eigene Services registrieren
// ------------------------------------------
builder.Services.AddScoped<TimeEntryService>()
                .AddScoped<UserStatsService>()
                .AddScoped<UserService>()
                .AddScoped<SeasonService>()
                .AddScoped<ImpressumService>()
                .AddScoped<PrivacyPolicyService>()
                .AddScoped<ToastService>()
                .AddScoped<EventService>();

builder.Services.AddHostedService<AutoCheckoutHostedService>();
// ------------------------------------------
//  4. App erstellen
// ------------------------------------------
var app = builder.Build();

// ------------------------------------------
//  5. Middleware konfigurieren
// ------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();  
app.UseAuthorization();   
app.UseAntiforgery();
// ------------------------------------------
//  6. Razor-Komponenten aktivieren
// ------------------------------------------
app.MapControllers();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()
   .DisableAntiforgery();;
// ------------------------------------------
//  7. App starten
// ------------------------------------------
app.Run();
