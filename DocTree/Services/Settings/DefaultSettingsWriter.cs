using System.Reflection;
using DocTree.App;

namespace DocTree.Services.Settings
{
    /// <summary>
    /// 初回起動時に %AppData%\DocTree\settings.jsonc を埋め込みリソースから生成する。
    /// </summary>
    public static class DefaultSettingsWriter
    {
        private const string ResourceName = "DocTree.Resources.default-settings.jsonc";

        public static void WriteDefaultIfMissing(string targetPath)
        {
            if (File.Exists(targetPath)) return;

            AppPaths.EnsureAppDataDirectory();

            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException(
                    $"埋め込みリソース '{ResourceName}' が見つかりません。");

            using var fs = File.Create(targetPath);
            stream.CopyTo(fs);
        }
    }
}
