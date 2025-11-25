using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
namespace Worktrack.Services;

public class PrivacyPolicyService
{
    private readonly AppDbContext Db;

    public PrivacyPolicyService(AppDbContext db)
    {
        Db = db;
    }

    public async Task<PrivacyPolicy> GetAsync()
    {
        var doc = await Db.PrivacyPolicies.FirstOrDefaultAsync();
        return doc ?? new PrivacyPolicy();
    }

    public async Task SaveAsync(PrivacyPolicy policy)
    {
        var existing = await Db.PrivacyPolicies.FirstOrDefaultAsync();

        if (existing == null)
        {
            Db.PrivacyPolicies.Add(policy);
        }
        else
        {
            existing.Content = policy.Content;
        }

        await Db.SaveChangesAsync();
    }
}
