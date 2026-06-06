using YtLibrary.Models;

namespace YtLibrary.Services;

public interface IYoutubeSearchService
{
    Task<IReadOnlyList<YoutubeVideoItem>> SearchAsync(string query);
    Task<YoutubeVideoItem?> GetVideoByUrlAsync(string url);
    Task DownloadAudiosAsZipAsync(IEnumerable<string> links);
    Task DownloadAudiosAsZipAsync(IEnumerable<YoutubeVideoItem> videoInfo)
    {
        var links = videoInfo.Select(x => x.Url);

        return DownloadAudiosAsZipAsync(links);
    }
    Task DownloadVideoAsync(YoutubeVideoItem video);
    Task DownloadAudioAsync(YoutubeVideoItem video, bool compress);
}
