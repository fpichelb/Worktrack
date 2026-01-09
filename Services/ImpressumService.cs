using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
namespace Worktrack.Services;
public class ImpressumService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ImpressumService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<Impressum> GetAsync()
    {
        await using var Db = await _factory.CreateDbContextAsync();
        var data = await Db.ImpressumData.FirstOrDefaultAsync();
        return data ?? new Impressum();
    }

    public async Task SaveAsync(Impressum imp)
    {
        await using var Db = await _factory.CreateDbContextAsync();
        var existing = await Db.ImpressumData.FirstOrDefaultAsync();
        if (existing == null)
        {
            Db.ImpressumData.Add(imp);
        }
        else
        {
            existing.ResponsibleName = imp.ResponsibleName;
            existing.Address = imp.Address;
            existing.Email = imp.Email;
            existing.Phone = imp.Phone;
            existing.HostingProvider = imp.HostingProvider;
            existing.Notes = imp.Notes;
        }

        await Db.SaveChangesAsync();
    }
}
