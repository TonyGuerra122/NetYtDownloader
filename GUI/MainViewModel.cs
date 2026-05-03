using GUI.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YtLibrary.Models;
using YtLibrary.Services;

namespace GUI;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly YoutubeSearchService _youtubeSearchService;

    private string _searchText = string.Empty;
    private bool _isLoading = false;
    private string _loadingText = string.Empty;
    private YoutubeVideoItem? _selectedVideo = null;

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

    public ICommand SearchCommand { get; }
    public ICommand OpenVideoCommand { get; }
    public ICommand DownloadCommand { get; }

    public MainViewModel(YoutubeSearchService youtubeSearchService)
    {
        _youtubeSearchService = youtubeSearchService;

        SearchCommand = new RelayCommand<YoutubeVideoItem>(async _ => await SearchAsync());

        OpenVideoCommand = new RelayCommand<YoutubeVideoItem>(OpenVideo);

        DownloadCommand = new RelayCommand<YoutubeVideoItem>(
            async video => await DownloadVideo(video)
        );
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
        if (video is null)
            return;

        IsLoading = true;
        LoadingText = "Baixando vídeo...";

        try
        {
            await _youtubeSearchService.DownloadAsync(video);
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
