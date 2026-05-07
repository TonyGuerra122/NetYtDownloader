using FFmpegLibrary.Services;
using System.IO;
using System.Windows;
using System.Windows.Input;
using YtLibrary.Services;

namespace GUI;

public partial class MainWindow : Window
{
    private readonly FFmpegService _ffmpegService;
    private readonly MainViewModel _mainViewModel;

    public MainWindow()
    {
        InitializeComponent();

        string videosFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "NetYtDownloader");
        string binariesFolder = Path.Combine(videosFolder, "bin");
        
        _ffmpegService = new FFmpegService(binariesFolder);
        var youtubeService = new YoutubeSearchService(_ffmpegService, videosFolder);

        _mainViewModel = new MainViewModel(youtubeService);

        DataContext = _mainViewModel;

        Loaded += MainWindowLoaded;
    }

    private async void MainWindowLoaded(object sender, RoutedEventArgs e)
    {
        await _mainViewModel.CheckAndInstallUpdate();

        if (!_ffmpegService.IsFFmpegPresent())
        {
            var result = MessageBox.Show(
                "O FFmpeg é necessário para baixar vídeos.\nDeseja baixar automaticamente agora?",
                "Dependência necessária",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    await _ffmpegService.EnsureFFmpegInstalledAsync();

                    MessageBox.Show(
                        "FFmpeg instalado com sucesso!",
                        "Sucesso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erro ao instalar FFmpeg:\n{ex.Message}",
                        "Erro",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
            else
            {
                MessageBox.Show(
                    "Sem o FFmpeg, o download de vídeos não funcionará.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }
    }
}