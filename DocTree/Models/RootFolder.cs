namespace DocTree.Models
{
    public sealed class RootFolder
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public ReadOnlyMode ReadOnly { get; set; } = ReadOnlyMode.Inherit;
        public List<string> Exclude { get; set; } = new();
    }
}
