using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
namespace Worktrack.Services;
public class ImpressumService
{
    private readonly AppDbContext Db;

    public ImpressumService(AppDbContext db)
    {
        Db = db;
    }

    public async Task<Impressum> GetAsync()
    {
        var data = await Db.ImpressumData.FirstOrDefaultAsync();
        return data ?? new Impressum();
    }

    public async Task SaveAsync(Impressum imp)
    {
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
