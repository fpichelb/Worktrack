public class NewsItem
{
    public int Id { get; set; }

    public string Title { get; set; } = "";
    public DateTime PublishFrom { get; set; } = DateTime.UtcNow;
    public DateTime? PublishTo { get; set; }
    public bool IsPinned { get; set; }

    public string ContentType { get; set; } = "";
    public string FileName { get; set; } = "";

    public string BlobKey { get; set; } = "";     // z.B. "news/2026/01/<guid>.pdf"
    public string? ThumbBlobKey { get; set; }     // optional

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; } 
}
