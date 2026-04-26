namespace DocTree.Models
{
    public sealed class AppSettings
    {
        public bool ReadOnly { get; set; } = true;

        public List<string> TextExtensions { get; set; } = new()
        {
            ".txt", ".md", ".markdown", ".log", ".csv", ".tsv",
            ".json", ".jsonc", ".xml", ".yaml", ".yml", ".ini", ".toml",
            ".cs", ".ts", ".tsx", ".js", ".jsx", ".py", ".rb", ".go", ".rs",
            ".sql", ".html", ".htm", ".css", ".scss", ".sass",
            ".sh", ".ps1", ".bat", ".cmd"
        };

        public List<string> TextFilenames { get; set; } = new()
        {
            "README", "LICENSE", "Makefile", "Dockerfile", "CHANGELOG"
        };

        public int BinarySniffBytes { get; set; } = 8192;

        public string DefaultEncoding { get; set; } = "utf-8";

        public FontSetting Font { get; set; } = new();

        public List<string> Exclude { get; set; } = new()
        {
            ".git"
        };

        public List<RootFolder> Roots { get; set; } = new();

        public List<PathOverride> Overrides { get; set; } = new();

        public List<ExternalEditor> ExternalEditors { get; set; } = new();
    }

    public sealed class FontSetting
    {
        public string Family { get; set; } = "Consolas";
        public float Size { get; set; } = 11.0f;
    }
}
