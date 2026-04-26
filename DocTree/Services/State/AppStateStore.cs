using System.Text.Json;
using DocTree.App;

namespace DocTree.Services.State
{
    public sealed class AppState
    {
        public int? WindowX { get; set; }
        public int? WindowY { get; set; }
        public int? WindowWidth { get; set; }
        public int? WindowHeight { get; set; }
        public bool? Maximized { get; set; }
        public int? SplitterDistance { get; set; }
        public bool? WordWrap { get; set; }
        public List<string> OpenFiles { get; set; } = new();
        public string? ActiveFile { get; set; }
    }

    /// <summary>
    /// %AppData%\DocTree\state.json の永続化。失敗しても致命傷にならないよう全例外を握りつぶす。
    /// </summary>
    public static class AppStateStore
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static AppState Load()
        {
            try
            {
                var path = AppPaths.AppDataStatePath;
                if (!File.Exists(path)) return new AppState();
                var text = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppState>(text, Options) ?? new AppState();
            }
            catch
            {
                return new AppState();
            }
        }

        public static void Save(AppState state)
        {
            try
            {
                AppPaths.EnsureAppDataDirectory();
                var json = JsonSerializer.Serialize(state, Options);
                File.WriteAllText(AppPaths.AppDataStatePath, json);
            }
            catch
            {
                // 状態保存の失敗は無視
            }
        }
    }
}
