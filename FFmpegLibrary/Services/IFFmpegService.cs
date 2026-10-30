namespace FFmpegLibrary.Services;

public interface IFFmpegService
{
    bool IsFFmpegPresent();
    Task EnsureFFmpegInstalledAsync(IProgress<DownloadProgress>? progress = null);
    Task JoinStreamsToFileAsync(
        Stream videoStream,
        Stream audioStream,
        string outputFile,
        CancellationToken cancellationToken = default
    );

}
