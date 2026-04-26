namespace DocTree.Models
{
    public sealed class ProjectSettings
    {
        public List<RootFolder> Roots { get; set; } = new();

        public List<PathOverride> Overrides { get; set; } = new();
    }
}
