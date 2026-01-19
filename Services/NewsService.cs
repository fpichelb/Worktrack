using Microsoft.EntityFrameworkCore;
using Worktrack.Interface;
using Worktrack.Data;
using Microsoft.AspNetCore.Components.Forms;
namespace Worktrack.Services;
public class NewsService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IBlobStorage _blob;

    private static readonly HashSet<string> AllowedTypes = new()
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public NewsService(IDbContextFactory<AppDbContext> factory, IBlobStorage blob)
    {
        _factory = factory;
        _blob = blob;
    }

    public async Task<int> CreateAsync(
        string title,
        DateTime publishFromLocal,
        DateTime? publishToLocal,
        bool isPinned,
        IBrowserFile file,
        int createdByUserId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Titel ist erforderlich.");

        if (!AllowedTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Nur PDF, JPG, PNG oder WebP erlaubt.");

        const long maxBytes = 10 * 1024 * 1024; // 10MB
        if (file.Size <= 0 || file.Size > maxBytes)
            throw new InvalidOperationException("Datei ist leer oder zu groß (max 10MB).");

        var ext = Path.GetExtension(file.Name);
        if (string.IsNullOrWhiteSpace(ext))
            ext = file.ContentType == "application/pdf" ? ".pdf" : ".bin";

        // Blob speichern
        await using var stream = file.OpenReadStream(maxBytes);
        var blobKey = await _blob.SaveAsync(stream, ext, subfolder: $"news/{DateTime.UtcNow:yyyy/MM}", ct);

        await using var db = await _factory.CreateDbContextAsync(ct);

        var news = new NewsItem
        {
            Title = title.Trim(),
            PublishFrom = publishFromLocal.ToUniversalTime(),
            PublishTo = publishToLocal?.ToUniversalTime(),
            IsPinned = isPinned,
            ContentType = file.ContentType,
            FileName = file.Name,
            BlobKey = blobKey,
            CreatedByUserId = createdByUserId
        };

        db.NewsItems.Add(news);
        await db.SaveChangesAsync(ct);
        return news.Id;
    }

    public async Task<List<NewsItem>> ListForAdminAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.NewsItems
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.PublishFrom)
            .ToListAsync(ct);
    }

    public async Task<List<NewsItem>> ListPinnedForDashboardAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;

        return await db.NewsItems
            .Where(n => n.PublishFrom <= now && (n.PublishTo == null || n.PublishTo >= now)&&n.IsPinned)
            .OrderByDescending(n => n.PublishFrom)
            .Take(3)
            .ToListAsync(ct);
    }

    public async Task<NewsItem?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.NewsItems.FirstOrDefaultAsync(n => n.Id == id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var item = await db.NewsItems.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (item is null) return;

        db.NewsItems.Remove(item);
        await db.SaveChangesAsync(ct);

        await _blob.DeleteAsync(item.BlobKey, ct);
        if (!string.IsNullOrWhiteSpace(item.ThumbBlobKey))
            await _blob.DeleteAsync(item.ThumbBlobKey!, ct);
    }
}
