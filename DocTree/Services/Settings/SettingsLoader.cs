using System.Text.Json;
using System.Text.Json.Serialization;
using DocTree.Models;

namespace DocTree.Services.Settings
{
    public sealed class SettingsLoadResult
    {
        public required AppSettings Settings { get; init; }
        public required string SourcePath { get; init; }
        public required string ProjectSourcePath { get; init; }
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
            return Load(path, GetDefaultProjectSettingsPath(path));
        }

        public static SettingsLoadResult Load(string path, string projectPath)
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Options)
                ?? throw new InvalidDataException("settings.jsonc のパースに失敗しました（null）。");
            var warnings = new List<string>();

            // null 防御
            settings.TextExtensions ??= new();
            settings.TextFilenames ??= new();
            settings.Exclude ??= new();
            settings.Roots ??= new();
            settings.Overrides ??= new();
            settings.ExternalEditors ??= new();
            settings.Font ??= new FontSetting();

            if (File.Exists(projectPath))
            {
                var projectJson = File.ReadAllText(projectPath);
                var projectSettings = JsonSerializer.Deserialize<ProjectSettings>(projectJson, Options)
                    ?? throw new InvalidDataException("project-settings.jsonc のパースに失敗しました（null）。");

                projectSettings.Roots ??= new();
                projectSettings.Overrides ??= new();
                settings.Roots = projectSettings.Roots;
                settings.Overrides = projectSettings.Overrides;
            }
            else if (settings.Roots.Count > 0 || settings.Overrides.Count > 0)
            {
                warnings.Add("settings.jsonc の roots / overrides を互換読み込みしました。project-settings.jsonc への移動を推奨します。");
            }
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
                SourcePath = path,
                ProjectSourcePath = projectPath,
                Warnings = warnings
            };
        }

        private static string GetDefaultProjectSettingsPath(string settingsPath)
        {
            var directory = Path.GetDirectoryName(settingsPath);
            return Path.Combine(directory ?? "", DocTree.App.AppPaths.ProjectSettingsFileName);
        }
    }
}
