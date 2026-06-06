using FFmpegLibrary.Services;
using System.Diagnostics;
using System.IO.Compression;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using YtLibrary.Models;

namespace YtLibrary.Services;

public class YoutubeSearchService(FFmpegService ffmpegService, string videosFolder) : IYoutubeSearchService
{
    private readonly YoutubeClient _client = new();
    private readonly FFmpegService _ffmpegService = ffmpegService;
    private readonly string _videosFolder = videosFolder;

    public async Task<IReadOnlyList<YoutubeVideoItem>> SearchAsync(string query)
    {
        var results = new List<YoutubeVideoItem>();

        await foreach (var video in _client.Search.GetVideosAsync(query))
        {
            results.Add(new YoutubeVideoItem
            {
                Id = video.Id,
                Title = video.Title,
                Author = video.Author.ChannelTitle,
                Duration = video.Duration?.ToString(@"hh\:mm\:ss") ?? "Unknown",
                Url = $"https://www.youtube.com/watch?v={video.Id}",
                ThumbnailUrl = video.Thumbnails.GetWithHighestResolution().Url
            });

            if (results.Count >= 20) break;
        }

        return [.. results];
    }

    public async Task<YoutubeVideoItem?> GetVideoByUrlAsync(string url)
    {
        var video = await _client.Videos.GetAsync(url);

        return new YoutubeVideoItem
        {
            Id = video.Id,
            Title = video.Title,
            Author = video.Author.ChannelTitle,
            Duration = video.Duration?.ToString(@"hh\:mm\:ss") ?? "Unknown",
            Url = $"https://www.youtube.com/watch?v={video.Id}",
            ThumbnailUrl = video.Thumbnails.GetWithHighestResolution().Url
        };
    }

    public async Task DownloadVideoAsync(YoutubeVideoItem video)
    {
        var manifest = await _client.Videos.Streams.GetManifestAsync(video.Id);

        var videoStreamInfo = manifest
            .GetVideoOnlyStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestVideoQuality();

        var audioStreamInfo = manifest
            .GetAudioOnlyStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestBitrate();

        if (videoStreamInfo is null || audioStreamInfo is null) throw new Exception("Não foi possível encontrar streams compatíveis para download.");

        string fileName = SanitizeFileName(video.Title) + ".mp4";
        string outputFile = Path.Combine(_videosFolder, fileName);

        await using var videoStream =
            await _client.Videos.Streams.GetAsync(videoStreamInfo);

        await using var audioStream =
            await _client.Videos.Streams.GetAsync(audioStreamInfo);

        await _ffmpegService.JoinStreamsToFileAsync(
            videoStream,
            audioStream,
            outputFile
        );

        OpenVideosFolder();
    }

    public async Task DownloadAudioAsync(YoutubeVideoItem video, bool compress = false)
    {
        var manifest = await _client.Videos.Streams.GetManifestAsync(video.Id);

        string fileName = SanitizeFileName(video.Title) + ".mp4";
        string outputFile = Path.Combine(_videosFolder, fileName);

        var audioStreamInfo = manifest
            .GetAudioOnlyStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestBitrate() ?? throw new Exception("Não foi possível encontrar streams compatíveis para download.");

        await _client.Videos.Streams.DownloadAsync(audioStreamInfo, outputFile);

        if (compress)
        {
            string zipFile = Path.Combine(
                _videosFolder,
                SanitizeFileName(video.Title) + ".zip"
            );

            if (File.Exists(zipFile)) File.Delete(zipFile);

            using var zip = ZipFile.Open(zipFile, ZipArchiveMode.Create);
            zip.CreateEntryFromFile(outputFile, fileName);

            File.Delete(outputFile);
        }

        OpenVideosFolder();
    }

    public async Task DownloadAudiosAsZipAsync(IEnumerable<string> links)
    {
        if (!Directory.Exists(_videosFolder)) Directory.CreateDirectory(_videosFolder);

        string tempFolder = Path.Combine(
            Path.GetTempPath(),
            "NetYtDownloader_" + Guid.NewGuid()
        );

        Directory.CreateDirectory(tempFolder);

        List<string> downloadedFiles = [];
        var locker = new object();

        try
        {
            await Parallel.ForEachAsync(links, async (link, _) =>
            {
                try
                {
                    var video = await _client.Videos.GetAsync(link, _);

                    var manifest = await _client.Videos.Streams.GetManifestAsync(video.Id, _);

                    var audioStreamInfo = manifest
                        .GetAudioOnlyStreams()
                        .Where(s => s.Container == Container.Mp4)
                        .GetWithHighestBitrate();

                    if (audioStreamInfo is null)
                        return;

                    string fileName = SanitizeFileName(video.Title) + ".mp4";
                    string outputFile = Path.Combine(tempFolder, fileName);

                    await _client.Videos.Streams.DownloadAsync(audioStreamInfo, outputFile, cancellationToken: _);

                    lock (locker)
                    {
                        downloadedFiles.Add(outputFile);
                    }
                }
                catch
                {
                    // Ignora links inválidos ou que falharam
                }
            });

            string zipFile = Path.Combine(
                _videosFolder,
                $"musicas_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
            );

            if (File.Exists(zipFile)) File.Delete(zipFile);

            using var zip = ZipFile.Open(zipFile, ZipArchiveMode.Create);

            foreach (string file in downloadedFiles)
            {
                zip.CreateEntryFromFile(file, Path.GetFileName(file));
            }

            OpenVideosFolder();
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    private void OpenVideosFolder()
    {
        if (IsFolderOpen()) return;

        if (!Directory.Exists(_videosFolder)) Directory.CreateDirectory(_videosFolder);

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = _videosFolder,
            UseShellExecute = true
        });
    }

    private static bool IsFolderOpen()
    {
        var processes = Process.GetProcessesByName("explorer");

        foreach (var item in processes)
        {
            try
            {
                if (item.MainWindowTitle.Contains("NetYtDownloader")) return true;
            }
            catch { }
        }

        return false;
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
    }
}
