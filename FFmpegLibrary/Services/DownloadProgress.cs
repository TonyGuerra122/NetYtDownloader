namespace FFmpegLibrary.Services;

public class DownloadProgress
{
    public long BytesDownloaded { get; set; }
    public long? TotalBytes { get; set; }
    public int PercentComplete => TotalBytes.HasValue && TotalBytes > 0
        ? (int)((BytesDownloaded * 100) / TotalBytes.Value)
        : 0;
    public string Message { get; set; } = string.Empty;
}
