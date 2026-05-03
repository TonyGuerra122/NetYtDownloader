namespace FFmpegLibrary.Services;

public interface IFFmpegService
{
    bool IsFFmpegPresent();
    Task EnsureFFmpegInstalledAsync();
    Task JoinStreamsToFileAsync(
        Stream videoStream,
        Stream audioStream,
        string outputFile,
        CancellationToken cancellationToken = default
    );

}
