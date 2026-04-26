using System.Text;
using DocTree.Models;

namespace DocTree.Services.Text
{
    public sealed class EncodingDetectionResult
    {
        public required EncodingKind Kind { get; init; }
        public required Encoding Encoding { get; init; }
        public required int BomLength { get; init; }
    }

    /// <summary>
    /// UTF-8 / UTF-16 のみ対応のエンコーディング判定。
    /// 1) BOM を見る
    /// 2) なければ UTF-8 厳格デコードで成立すれば UTF-8
    /// 3) どちらでもなければ defaultEncoding（utf-8 / utf-16le / utf-16be）にフォールバック
    /// </summary>
    public sealed class EncodingDetector
    {
        private readonly AppSettings _settings;

        public EncodingDetector(AppSettings settings)
        {
            _settings = settings;
        }

        public EncodingDetectionResult Detect(ReadOnlySpan<byte> head)
        {
            // 1) BOM
            if (head.Length >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF)
            {
                return Make(EncodingKind.Utf8Bom, new UTF8Encoding(true), 3);
            }
            if (head.Length >= 2 && head[0] == 0xFF && head[1] == 0xFE)
            {
                return Make(EncodingKind.Utf16Le, new UnicodeEncoding(false, true), 2);
            }
            if (head.Length >= 2 && head[0] == 0xFE && head[1] == 0xFF)
            {
                return Make(EncodingKind.Utf16Be, new UnicodeEncoding(true, true), 2);
            }

            // 2) UTF-8 厳格デコード
            if (TryStrictUtf8(head))
            {
                return Make(EncodingKind.Utf8NoBom, new UTF8Encoding(false, false), 0);
            }

            // 3) フォールバック
            return _settings.DefaultEncoding.Trim().ToLowerInvariant() switch
            {
                "utf-16le" or "utf16le" => Make(EncodingKind.Utf16Le, new UnicodeEncoding(false, false), 0),
                "utf-16be" or "utf16be" => Make(EncodingKind.Utf16Be, new UnicodeEncoding(true, false), 0),
                _                       => Make(EncodingKind.Utf8NoBom, new UTF8Encoding(false, false), 0),
            };
        }

        private static EncodingDetectionResult Make(EncodingKind kind, Encoding enc, int bomLen) =>
            new() { Kind = kind, Encoding = enc, BomLength = bomLen };

        private static bool TryStrictUtf8(ReadOnlySpan<byte> bytes)
        {
            // 末尾でマルチバイトが切れている可能性に備え、不完全な末尾シーケンスは許容する。
            var safeLen = TrimToCompleteUtf8(bytes);
            if (safeLen <= 0) return true; // 何も判断できないなら許容

            try
            {
                var enc = new UTF8Encoding(false, throwOnInvalidBytes: true);
                _ = enc.GetCharCount(bytes[..safeLen]);
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// バッファ末尾で UTF-8 マルチバイトが途切れている分を切り落とした長さを返す。
        /// </summary>
        private static int TrimToCompleteUtf8(ReadOnlySpan<byte> bytes)
        {
            for (int i = bytes.Length - 1; i >= 0 && i >= bytes.Length - 4; i--)
            {
                var b = bytes[i];
                if ((b & 0x80) == 0) return i + 1;       // ASCII = 完全
                if ((b & 0xC0) == 0x80) continue;        // 継続バイト
                // リーディングバイト
                int needed = (b & 0xE0) == 0xC0 ? 2
                           : (b & 0xF0) == 0xE0 ? 3
                           : (b & 0xF8) == 0xF0 ? 4
                           : 0;
                if (needed == 0) return i + 1;
                int have = bytes.Length - i;
                return have >= needed ? bytes.Length : i;
            }
            return bytes.Length;
        }
    }
}
