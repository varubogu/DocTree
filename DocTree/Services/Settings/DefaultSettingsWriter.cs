using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocTree.App;
using DocTree.Models;

namespace DocTree.Services.Settings
{
    /// <summary>
    /// 初回起動時に %AppData%\DocTree\settings.jsonc を埋め込みリソースから生成する。
    /// </summary>
    public static class DefaultSettingsWriter
    {
        private const string SettingsResourceName = "DocTree.Resources.default-settings.jsonc";
        private const string ProjectSettingsResourceName = "DocTree.Resources.default-project-settings.jsonc";
        private static readonly JsonSerializerOptions ProjectOptions = CreateProjectOptions();

        public static void WriteDefaultIfMissing(string targetPath)
        {
            WriteResourceIfMissing(targetPath, SettingsResourceName);
        }

        public static void WriteDefaultProjectIfMissing(string targetPath)
        {
            WriteResourceIfMissing(targetPath, ProjectSettingsResourceName);
        }

        public static void WriteProjectSettingsIfMissing(string targetPath, ProjectSettings projectSettings)
        {
            if (File.Exists(targetPath)) return;

            if (projectSettings.Roots.Count == 0 && projectSettings.Overrides.Count == 0)
            {
                WriteDefaultProjectIfMissing(targetPath);
                return;
            }

            AppPaths.EnsureAppDataDirectory();
            var json = JsonSerializer.Serialize(projectSettings, ProjectOptions);
            File.WriteAllText(targetPath, json + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static void WriteResourceIfMissing(string targetPath, string resourceName)
        {
            if (File.Exists(targetPath)) return;

            AppPaths.EnsureAppDataDirectory();

            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"埋め込みリソース '{resourceName}' が見つかりません。");

            using var fs = File.Create(targetPath);
            stream.CopyTo(fs);
        }

        private static JsonSerializerOptions CreateProjectOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
            return options;
        }
    }
}
