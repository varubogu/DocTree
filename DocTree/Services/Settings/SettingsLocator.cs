using DocTree.App;

namespace DocTree.Services.Settings
{
    public sealed class SettingsLocateResult
    {
        public required string Path { get; init; }
        public required bool IsPortable { get; init; }
        public required bool Existed { get; init; }
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
                return new SettingsLocateResult { Path = portable, IsPortable = true, Existed = true };
            }

            var appData = AppPaths.AppDataSettingsPath;
            if (File.Exists(appData))
            {
                return new SettingsLocateResult { Path = appData, IsPortable = false, Existed = true };
            }

            return new SettingsLocateResult { Path = appData, IsPortable = false, Existed = false };
        }
    }
}
