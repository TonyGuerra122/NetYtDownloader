using FFmpegLibrary.Services;
using System.Diagnostics;
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

    public async Task DownloadAudioAsync(YoutubeVideoItem video)
    {
        var manifest = await _client.Videos.Streams.GetManifestAsync(video.Id);

        string fileName = SanitizeFileName(video.Title) + ".mp4";
        string outputFile = Path.Combine(_videosFolder, fileName);

        var audioStreamInfo = manifest
            .GetAudioOnlyStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestBitrate() ?? throw new Exception("Não foi possível encontrar streams compatíveis para download.");

        await _client.Videos.Streams.DownloadAsync(audioStreamInfo, outputFile);

        OpenVideosFolder();
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
