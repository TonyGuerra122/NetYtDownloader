using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using GUI.Configurations;
using System.Diagnostics;
using System.IO;

namespace GUI.Update;

public static class AutoUpdater
{

    public static async Task<bool> IsUpdateAvailable() => await CheckUpdate() is not null;
    public static async Task CheckForUpdateAsync()
    {
        using var http = new HttpClient();

        var info = await CheckUpdate();

        if (info is null) return;

        string tempMsi = Path.Combine(
            Path.GetTempPath(),
            $"NetYtDownloader-{info.Version}.msi"
        );

        byte[] bytes = await http.GetByteArrayAsync(info.Url);
        await File.WriteAllBytesAsync(tempMsi, bytes);

        Process.Start(new ProcessStartInfo
        {
            FileName = "msiexec.exe",
            Arguments = $"/i \"{tempMsi}\"",
            UseShellExecute = true,
            Verb = "runas"
        });

        Environment.Exit(0);
    }

    private static async Task<UpdateInfo?> CheckUpdate()
    {
        using var http = new HttpClient();

        string json = await http.GetStringAsync(Consts.UPDATE_URL);

        var info = JsonSerializer.Deserialize<UpdateInfo>(json, Consts.JSON_SERIALIZER_OPTIONS);

        if (info is null) return null;

        string version = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.1";

        var currentVersion = new Version(version);
        var latestVersion = new Version(info.Version);

        if (currentVersion <= latestVersion) return info;

        return null;
    }

    private class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
