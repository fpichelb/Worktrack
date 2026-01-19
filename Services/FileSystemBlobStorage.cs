using Worktrack.Interface;
public class FileSystemBlobStorage : IBlobStorage
{
    private readonly string _rootPath;

    public FileSystemBlobStorage(IConfiguration cfg)
    {
        _rootPath = cfg["BlobStorage:RootPath"] ?? "App_Data/blob";
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string extension, string subfolder, CancellationToken ct = default)
    {
        extension = (extension ?? "").Trim().ToLowerInvariant();
        if (!extension.StartsWith(".")) extension = "." + extension;

        var folder = Path.Combine(_rootPath, subfolder);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);

        await using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(fs, ct);

        // blobKey ist relativer Pfad
        var blobKey = Path.Combine(subfolder, fileName).Replace("\\", "/");
        return blobKey;
    }

    public async Task<(Stream stream, string contentType, string fileName)?> OpenAsync(
        string blobKey, string contentType, string fileName, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, blobKey.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!File.Exists(fullPath)) return null;

        var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return (fs, contentType, fileName);
    }

    public Task DeleteAsync(string blobKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, blobKey.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
