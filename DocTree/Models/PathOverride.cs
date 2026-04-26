namespace DocTree.Models
{
    public sealed class PathOverride
    {
        public string Path { get; set; } = "";
        public ReadOnlyMode ReadOnly { get; set; } = ReadOnlyMode.Inherit;
    }
}
