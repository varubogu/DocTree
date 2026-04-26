using System.Text.Json;
using System.Text.Json.Serialization;
using DocTree.Models;

namespace DocTree.Services.Settings
{
    public sealed class SettingsLoadResult
    {
        public required AppSettings Settings { get; init; }
        public required string SourcePath { get; init; }
        public List<string> Warnings { get; init; } = new();
    }

    public static class SettingsLoader
    {
        private static readonly JsonSerializerOptions Options = CreateOptions();

        private static JsonSerializerOptions CreateOptions()
        {
            var o = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
            return o;
        }

        public static SettingsLoadResult Load(string path)
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Options)
                ?? throw new InvalidDataException("settings.jsonc のパースに失敗しました（null）。");

            // null 防御
            settings.TextExtensions ??= new();
            settings.TextFilenames ??= new();
            settings.Exclude ??= new();
            settings.Roots ??= new();
            settings.Overrides ??= new();
            settings.ExternalEditors ??= new();
            settings.Font ??= new FontSetting();
            if (string.IsNullOrWhiteSpace(settings.DefaultEncoding))
                settings.DefaultEncoding = "utf-8";
            if (settings.BinarySniffBytes <= 0)
                settings.BinarySniffBytes = 8192;

            // 拡張子は小文字に正規化
            for (int i = 0; i < settings.TextExtensions.Count; i++)
            {
                var e = settings.TextExtensions[i].Trim().ToLowerInvariant();
                if (!e.StartsWith('.')) e = "." + e;
                settings.TextExtensions[i] = e;
            }

            foreach (var root in settings.Roots)
            {
                root.Exclude ??= new();
            }

            return new SettingsLoadResult
            {
                Settings = settings,
                SourcePath = path
            };
        }
    }
}
