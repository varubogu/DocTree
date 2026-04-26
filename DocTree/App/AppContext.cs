using DocTree.Models;
using DocTree.Services.FileSystem;
using DocTree.Services.ReadOnly;
using DocTree.Services.Settings;

namespace DocTree.App
{
    /// <summary>
    /// 設定と各サービスを束ねる軽量コンテナ（手動DIの寄せ場）。
    /// </summary>
    public sealed class AppContext
    {
        public AppSettings Settings { get; private set; }
        public string SettingsPath { get; private set; }
        public string ProjectSettingsPath { get; private set; }

        public FileSystemScanner Scanner { get; }
        public ReadOnlyResolver ReadOnly { get; private set; }

        private AppContext(AppSettings settings, string settingsPath, string projectSettingsPath)
        {
            Settings = settings;
            SettingsPath = settingsPath;
            ProjectSettingsPath = projectSettingsPath;
            Scanner = new FileSystemScanner();
            ReadOnly = new ReadOnlyResolver(settings);
        }

        public static AppContext Bootstrap()
        {
            var locate = SettingsLocator.Locate();
            if (!locate.Existed)
            {
                DefaultSettingsWriter.WriteDefaultIfMissing(locate.Path);
            }
            if (!locate.ProjectExisted)
            {
                var legacyLoaded = SettingsLoader.Load(locate.Path, locate.ProjectPath);
                DefaultSettingsWriter.WriteProjectSettingsIfMissing(locate.ProjectPath, new ProjectSettings
                {
                    Roots = legacyLoaded.Settings.Roots,
                    Overrides = legacyLoaded.Settings.Overrides
                });
            }

            var loaded = SettingsLoader.Load(locate.Path, locate.ProjectPath);
            return new AppContext(loaded.Settings, loaded.SourcePath, loaded.ProjectSourcePath);
        }

        /// <summary>
        /// 設定読込に失敗した時のフォールバック起動用。空設定 + パスは %AppData%。
        /// </summary>
        public static AppContext BootstrapEmpty()
        {
            return new AppContext(new AppSettings(), AppPaths.AppDataSettingsPath, AppPaths.AppDataProjectSettingsPath);
        }

        /// <summary>
        /// 設定ファイルを再読み込みする（パスは現状維持）。
        /// </summary>
        public void ReloadSettings()
        {
            var loaded = SettingsLoader.Load(SettingsPath, ProjectSettingsPath);
            Settings = loaded.Settings;
            ReadOnly = new ReadOnlyResolver(loaded.Settings);
        }
    }
}
