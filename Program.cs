using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Services;
using Worktrack.Components;
using Blazored.LocalStorage;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------
// ?? 1. Razor Components aktivieren (Blazor Server)
// ------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazoredLocalStorage();

// ------------------------------------------
// ?? 2. MySQL-Datenbank konfigurieren
// ------------------------------------------
// Lies die Connection aus appsettings.json oder nutze Fallback
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "server=localhost;database=worktrack;user=worktrackUser;password=Passwort123;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ------------------------------------------
// ?? 3. Eigene Services registrieren
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

builder.Services.AddSingleton<AppState>();


// ------------------------------------------
// ?? 4. App erstellen
// ------------------------------------------
var app = builder.Build();

// ------------------------------------------
// ?? 5. Middleware konfigurieren
// ------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();



app.UseAntiforgery();
// ------------------------------------------
// ?? 6. Razor-Komponenten aktivieren
// ------------------------------------------
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// ------------------------------------------
// ?? 7. App starten
// ------------------------------------------
app.Run();
