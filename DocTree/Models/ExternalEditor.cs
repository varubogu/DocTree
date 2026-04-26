namespace DocTree.Models
{
    public sealed class ExternalEditor
    {
        public string Name { get; set; } = "";
        public string Exe { get; set; } = "";
        public string Args { get; set; } = "\"{path}\"";
        public bool Default { get; set; }
    }
}
