namespace DocTree.Models
{
    public enum EncodingKind
    {
        Unknown,
        Utf8NoBom,
        Utf8Bom,
        Utf16Le,
        Utf16Be
    }

    public enum LineEnding
    {
        Unknown,
        Crlf,
        Lf,
        Cr,
        Mixed
    }

    public static class EncodingKindExtensions
    {
        public static string Display(this EncodingKind k) => k switch
        {
            EncodingKind.Utf8NoBom => "UTF-8",
            EncodingKind.Utf8Bom   => "UTF-8 (BOM)",
            EncodingKind.Utf16Le   => "UTF-16 LE",
            EncodingKind.Utf16Be   => "UTF-16 BE",
            _ => "不明"
        };

        public static string Display(this LineEnding e) => e switch
        {
            LineEnding.Crlf  => "CRLF",
            LineEnding.Lf    => "LF",
            LineEnding.Cr    => "CR",
            LineEnding.Mixed => "Mixed",
            _ => ""
        };
    }
}
