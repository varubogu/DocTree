using DocTree.App;

namespace DocTree.Services.Settings
{
    public sealed class SettingsLocateResult
    {
        public required string Path { get; init; }
        public required string ProjectPath { get; init; }
        public required bool IsPortable { get; init; }
        public required bool Existed { get; init; }
        public required bool ProjectExisted { get; init; }
    }

    public static class SettingsLocator
    {
        /// <summary>
        /// 設定ファイルの探索: exe同梱優先 → %AppData% フォールバック。
        /// 両方なければ %AppData% パスを返す（呼び出し側で初期テンプレ生成）。
        /// </summary>
        public static SettingsLocateResult Locate()
        {
            var portable = AppPaths.PortableSettingsPath;
            if (File.Exists(portable))
            {
                return new SettingsLocateResult
                {
                    Path = portable,
                    ProjectPath = AppPaths.PortableProjectSettingsPath,
                    IsPortable = true,
                    Existed = true,
                    ProjectExisted = File.Exists(AppPaths.PortableProjectSettingsPath)
                };
            }

            var appData = AppPaths.AppDataSettingsPath;
            if (File.Exists(appData))
            {
                return new SettingsLocateResult
                {
                    Path = appData,
                    ProjectPath = AppPaths.AppDataProjectSettingsPath,
                    IsPortable = false,
                    Existed = true,
                    ProjectExisted = File.Exists(AppPaths.AppDataProjectSettingsPath)
                };
            }

            return new SettingsLocateResult
            {
                Path = appData,
                ProjectPath = AppPaths.AppDataProjectSettingsPath,
                IsPortable = false,
                Existed = false,
                ProjectExisted = false
            };
        }
    }
}
