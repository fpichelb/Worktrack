namespace Worktrack.Interface;
public interface IBlobStorage
{
    Task<string> SaveAsync(Stream content, string extension, string subfolder, CancellationToken ct = default);
    Task<(Stream stream, string contentType, string fileName)?> OpenAsync(string blobKey, string contentType, string fileName, CancellationToken ct = default);
    Task DeleteAsync(string blobKey, CancellationToken ct = default);
}
