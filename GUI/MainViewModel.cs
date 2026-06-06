using GUI.Commands;
using GUI.Update;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using YtLibrary.Models;
using YtLibrary.Services;

namespace GUI;

public class MainViewModel : INotifyPropertyChanged
{
    private const int MAX_PARALLEL_LINKS = 4;

    private readonly YoutubeSearchService _youtubeSearchService;

    private string _searchText = string.Empty;
    private bool _isLoading = false;
    private string _loadingText = string.Empty;
    private YoutubeVideoItem? _selectedVideo = null;
    private bool zipAudio = false;
    private string linksText = string.Empty;

    public ObservableCollection<YoutubeVideoItem> Videos { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string LoadingText
    {
        get => _loadingText;
        set
        {
            _loadingText = value;
            OnPropertyChanged();
        }
    }

    public YoutubeVideoItem? SelectedVideo
    {
        get => _selectedVideo;
        set
        {
            _selectedVideo = value;
            OnPropertyChanged();
        }
    }

    public bool ZipAudio
    {
        get => zipAudio;
        set
        {
            zipAudio = value;
            OnPropertyChanged();
        }
    }

    public string LinksText
    {
        get => linksText;
        set
        {
            linksText = value;
            OnPropertyChanged();
        }
    }

    public ICommand SearchCommand { get; }
    public ICommand OpenVideoCommand { get; }
    public ICommand DownloadVideoCommand { get; }
    public ICommand DownloadAudioCommand { get; }
    public ICommand AddLinksCommand { get; }
    public ICommand DownloadAllLinksCommand { get; }

    public MainViewModel(YoutubeSearchService youtubeSearchService)
    {
        _youtubeSearchService = youtubeSearchService;

        SearchCommand = new RelayCommand<YoutubeVideoItem>(async _ => await SearchAsync());

        OpenVideoCommand = new RelayCommand<YoutubeVideoItem>(OpenVideo);

        DownloadVideoCommand = new RelayCommand<YoutubeVideoItem>(
            async video => await DownloadVideo(video)
        );

        DownloadAudioCommand = new RelayCommand<YoutubeVideoItem>(
            async audio => await DownloadAudio(audio)
        );

        AddLinksCommand = new RelayCommand<YoutubeVideoItem>(async _ => await AddLinks());
        DownloadAllLinksCommand = new RelayCommand<YoutubeVideoItem>(async _ => await DownloadLinksAsZip());
    }

    public async Task CheckAndInstallUpdate()
    {
        if (await AutoUpdater.IsUpdateAvailable())
        {
            IsLoading = true;
            LoadingText = "Atualização disponível... (atualizando)";

            try
            {
                await AutoUpdater.CheckForUpdateAsync();
            }
            finally
            {
                IsLoading = false;
                LoadingText = string.Empty;
            }
        }
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        IsLoading = true;
        Videos.Clear();
        LoadingText = "Buscando vídeos...";

        try
        {
            var videos = await _youtubeSearchService.SearchAsync(SearchText);
            foreach (var video in videos)
            {
                Videos.Add(video);
            }
        }
        finally
        {
            IsLoading = false;
            LoadingText = string.Empty;
        }
    }

    private void OpenVideo(YoutubeVideoItem? video)
    {
        if (video is null)
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = $"https://www.youtube.com/watch?v={video.Id}",
            UseShellExecute = true
        });
    }

    private async Task DownloadVideo(YoutubeVideoItem? video)
    {
        if (video is null) return;

        IsLoading = true;
        LoadingText = "Baixando vídeo...";

        try
        {
            await _youtubeSearchService.DownloadVideoAsync(video);
        }
        finally
        {
            IsLoading = false;
            LoadingText = string.Empty;
        }
    }

    private async Task DownloadAudio(YoutubeVideoItem? video)
    {
        if (video is null) return;

        IsLoading = true;
        LoadingText = "Baixando audio...";

        try
        {
            await _youtubeSearchService.DownloadAudioAsync(video, ZipAudio);
        }
        finally
        {
            IsLoading = false;
            LoadingText = string.Empty;
        }
    }

    private async Task AddLinks()
    {
        if (string.IsNullOrWhiteSpace(LinksText)) return;

        IsLoading = true;
        LoadingText = "Carregando links...";

        try
        {
            var links = GetLinksFromText();

            await Parallel.ForEachAsync(
                links,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = MAX_PARALLEL_LINKS
                },
                async (link, cancellationToken) =>
                {
                    try
                    {
                        var video = await _youtubeSearchService.GetVideoByUrlAsync(link);

                        if (video is null) return;

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Videos.Add(video);
                        });
                    }
                    catch
                    {
                        // ignora link inválido
                    }
                });

            LinksText = string.Empty;
        }
        finally
        {
            IsLoading = false;
            LoadingText = string.Empty;
        }
    }

    private List<string> GetLinksFromText()
    {
        return [.. LinksText
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()];
    }

    private async Task DownloadLinksAsZip()
    {
        if (string.IsNullOrWhiteSpace(LinksText)) return;

        IsLoading = true;
        LoadingText = "Preparando downloads...";

        try
        {
            var links = GetLinksFromText();

            await _youtubeSearchService.DownloadAudiosAsZipAsync(links);

            LinksText = string.Empty;
        }
        finally
        {
            IsLoading = false;
            LoadingText = string.Empty;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
