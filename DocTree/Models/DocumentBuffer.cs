namespace DocTree.Models
{
    public sealed class DocumentBuffer
    {
        public required string FullPath { get; init; }
        public required string Text { get; init; }
        public required EncodingKind Encoding { get; init; }
        public required LineEnding LineEnding { get; init; }
        public required bool IsReadOnly { get; init; }
        public required long ByteSize { get; init; }
    }
}
