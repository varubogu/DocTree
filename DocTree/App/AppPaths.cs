namespace DocTree.App
{
    public static class AppPaths
    {
        public const string AppFolderName = "DocTree";
        public const string SettingsFileName = "settings.jsonc";
        public const string StateFileName = "state.json";

        public static string ExeDirectory =>
            System.AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

        public static string AppDataDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppFolderName);

        public static string PortableSettingsPath =>
            Path.Combine(ExeDirectory, SettingsFileName);

        public static string AppDataSettingsPath =>
            Path.Combine(AppDataDirectory, SettingsFileName);

        public static string AppDataStatePath =>
            Path.Combine(AppDataDirectory, StateFileName);

        public static void EnsureAppDataDirectory()
        {
            Directory.CreateDirectory(AppDataDirectory);
        }
    }
}
