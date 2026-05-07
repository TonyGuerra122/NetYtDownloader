using YtLibrary.Models;

namespace YtLibrary.Services;

public interface IYoutubeSearchService
{
    Task<IReadOnlyList<YoutubeVideoItem>> SearchAsync(string query);
    Task DownloadVideoAsync(YoutubeVideoItem video);
    Task DownloadAudioAsync(YoutubeVideoItem video);
}
