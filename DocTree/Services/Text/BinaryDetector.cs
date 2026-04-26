using DocTree.Models;

namespace DocTree.Services.Text
{
    public sealed class BinaryDetector
    {
        private readonly AppSettings _settings;
        private readonly HashSet<string> _textExt;
        private readonly HashSet<string> _textNames;

        public BinaryDetector(AppSettings settings)
        {
            _settings = settings;
            _textExt = new HashSet<string>(settings.TextExtensions, StringComparer.OrdinalIgnoreCase);
            _textNames = new HashSet<string>(settings.TextFilenames, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsExtensionAllowed(string fullPath)
        {
            var ext = Path.GetExtension(fullPath);
            if (!string.IsNullOrEmpty(ext) && _textExt.Contains(ext)) return true;

            var name = Path.GetFileName(fullPath);
            return !string.IsNullOrEmpty(name) && _textNames.Contains(name);
        }

        /// <summary>
        /// 先頭バイトを見てバイナリかどうか判定。UTF-16 BOM がある場合は 0x00 検出をスキップ。
        /// </summary>
        public bool LooksBinary(ReadOnlySpan<byte> head)
        {
            if (StartsWithUtf16Bom(head)) return false;
            return head.IndexOf((byte)0) >= 0;
        }

        private static bool StartsWithUtf16Bom(ReadOnlySpan<byte> head)
        {
            if (head.Length < 2) return false;
            if (head[0] == 0xFF && head[1] == 0xFE) return true;
            if (head[0] == 0xFE && head[1] == 0xFF) return true;
            return false;
        }

        public int SniffBytes => _settings.BinarySniffBytes;
    }
}
