using System.Diagnostics;
using System.IO.Compression;

namespace FFmpegLibrary.Services;

public class FFmpegService : IFFmpegService
{
    private readonly string _binariesFolder;
    private readonly string _ffmpegFolder;
    private readonly string _ffmpegPath;

    public FFmpegService(string binariesFolder)
    {
        _binariesFolder = binariesFolder;
        _ffmpegFolder = Path.Combine(_binariesFolder, "ffmpeg");
        _ffmpegPath = Path.Combine(_ffmpegFolder, "ffmpeg.exe");
    }

    public bool IsFFmpegPresent() => File.Exists(_ffmpegPath);

    public async Task EnsureFFmpegInstalledAsync(IProgress<DownloadProgress>? progress = null)
    {
        if (IsFFmpegPresent())
            return;

        Directory.CreateDirectory(_binariesFolder);
        Directory.CreateDirectory(_ffmpegFolder);

        string zipUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
        string zipPath = Path.Combine(_binariesFolder, "ffmpeg.zip");
        string extractPath = Path.Combine(_binariesFolder, "ffmpeg_extract");

        if (Directory.Exists(extractPath))
            Directory.Delete(extractPath, true);

        using var httpClient = new HttpClient();

        progress?.Report(new DownloadProgress { Message = "Iniciando download do FFmpeg..." });

        var response = await httpClient.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var canReportProgress = totalBytes != -1;

        await using (var contentStream = await response.Content.ReadAsStreamAsync())
        await using (var fileStream = File.Create(zipPath))
        {
            var totalRead = 0L;
            var buffer = new byte[8192];
            int read;

            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                totalRead += read;

                if (canReportProgress)
                {
                    progress?.Report(new DownloadProgress
                    {
                        BytesDownloaded = totalRead,
                        TotalBytes = totalBytes,
                        Message = $"Baixando FFmpeg... {(totalRead * 100) / totalBytes}%"
                    });
                }
            }
        }

        progress?.Report(new DownloadProgress { Message = "Extraindo arquivos..." });
        ZipFile.ExtractToDirectory(zipPath, extractPath, true);

        progress?.Report(new DownloadProgress { Message = "Finalizando instalação..." });

        string? ffmpegExe = Directory
            .GetFiles(extractPath, "ffmpeg.exe", SearchOption.AllDirectories)
            .FirstOrDefault() ?? throw new FileNotFoundException("ffmpeg.exe não foi encontrado no ZIP baixado.");
        File.Copy(ffmpegExe, _ffmpegPath, true);

        File.Delete(zipPath);
        Directory.Delete(extractPath, true);

        progress?.Report(new DownloadProgress { BytesDownloaded = 100, TotalBytes = 100, Message = "FFmpeg instalado com sucesso!" });
    }

    public async Task JoinStreamsToFileAsync(
        Stream videoStream,
        Stream audioStream,
        string outputFile,
        CancellationToken cancellationToken = default)
    {
        await EnsureFFmpegInstalledAsync();

        Directory.CreateDirectory(_ffmpegFolder);
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

        string id = Guid.NewGuid().ToString("N");

        string tempVideoPath = Path.Combine(_ffmpegFolder, $"{id}_video.mp4");
        string tempAudioPath = Path.Combine(_ffmpegFolder, $"{id}_audio.m4a");

        try
        {
            await using (var fs = File.Create(tempVideoPath))
                await videoStream.CopyToAsync(fs, cancellationToken);

            await using (var fs = File.Create(tempAudioPath))
                await audioStream.CopyToAsync(fs, cancellationToken);

            string ffmpegArgs =
                $"-y -i \"{tempVideoPath}\" -i \"{tempAudioPath}\" -c:v copy -c:a aac \"{outputFile}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = ffmpegArgs,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(processInfo)
                ?? throw new InvalidOperationException("Falha ao iniciar o FFmpeg.");

            string errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
                throw new Exception($"FFmpeg falhou: {errorOutput}");
        }
        finally
        {
            if (File.Exists(tempVideoPath))
                File.Delete(tempVideoPath);

            if (File.Exists(tempAudioPath))
                File.Delete(tempAudioPath);
        }
    }
}