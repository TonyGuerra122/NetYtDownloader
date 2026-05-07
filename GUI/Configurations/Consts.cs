using System.Text.Json;

namespace GUI.Configurations;

public static class Consts
{
    public static readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public const string UPDATE_URL =
        "https://github.com/TonyGuerra122/NetYtDownloader/releases/latest/download/update.json";
}
