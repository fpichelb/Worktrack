using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
namespace Worktrack.Services;

public class PrivacyPolicyService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public PrivacyPolicyService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<PrivacyPolicy> GetAsync()
    {
        await using var Db = await _factory.CreateDbContextAsync();
        var doc = await Db.PrivacyPolicies.FirstOrDefaultAsync();
        return doc ?? new PrivacyPolicy();
    }

    public async Task SaveAsync(PrivacyPolicy policy)
    {
        await using var Db = await _factory.CreateDbContextAsync();
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
