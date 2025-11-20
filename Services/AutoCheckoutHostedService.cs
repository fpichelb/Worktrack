using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
namespace Worktrack.Services;

public class AutoCheckoutHostedService : BackgroundService
{
    private readonly IServiceProvider _provider;

    public AutoCheckoutHostedService(IServiceProvider provider)
    {
        _provider = provider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _provider.CreateScope();
            var timeService = scope.ServiceProvider.GetRequiredService<TimeEntryService>();

            int updated = await timeService.AutoCheckoutStaleEntriesAsync();

            Console.WriteLine($"[AutoCheckout] Updated {updated} stale entries.");

            // jede Stunde prüfen
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
