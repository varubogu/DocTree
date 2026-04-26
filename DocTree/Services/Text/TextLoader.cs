using DocTree.Models;

namespace DocTree.Services.Text
{
    public sealed class TextLoadFailure
    {
        public required TextLoadFailureReason Reason { get; init; }
        public required string Message { get; init; }
    }

    public enum TextLoadFailureReason
    {
        NotAllowedByExtension,
        BinaryDetected,
        FileNotFound,
        AccessDenied,
        TooLarge,
        UnknownError
    }

    public sealed class TextLoadOutcome
    {
        public DocumentBuffer? Buffer { get; init; }
        public TextLoadFailure? Failure { get; init; }
        public bool Success => Buffer is not null;
    }

    public sealed class TextLoadOptions
    {
        public bool ForceTextEvenIfNotAllowed { get; init; }
        public long MaxBytes { get; init; } = 64 * 1024 * 1024; // 64 MiB
        public required bool IsReadOnly { get; init; }
    }

    public sealed class TextLoader
    {
        private readonly BinaryDetector _binary;
        private readonly EncodingDetector _encoding;

        public TextLoader(BinaryDetector binary, EncodingDetector encoding)
        {
            _binary = binary;
            _encoding = encoding;
        }

        public TextLoadOutcome Load(string fullPath, TextLoadOptions options)
        {
            try
            {
                if (!File.Exists(fullPath))
                {
                    return Fail(TextLoadFailureReason.FileNotFound, "ファイルが見つかりません: " + fullPath);
                }

                var fi = new FileInfo(fullPath);
                if (fi.Length > options.MaxBytes)
                {
                    return Fail(TextLoadFailureReason.TooLarge,
                        $"ファイルサイズが上限 ({options.MaxBytes:N0} バイト) を超えています: {fi.Length:N0} バイト");
                }

                if (!options.ForceTextEvenIfNotAllowed && !_binary.IsExtensionAllowed(fullPath))
                {
                    return Fail(TextLoadFailureReason.NotAllowedByExtension,
                        "テキスト拡張子のホワイトリストに含まれていません。");
                }

                var sniffLen = (int)Math.Min(_binary.SniffBytes, fi.Length);
                byte[] head = new byte[sniffLen];
                using (var fs = File.OpenRead(fullPath))
                {
                    int read = 0;
                    while (read < sniffLen)
                    {
                        int n = fs.Read(head, read, sniffLen - read);
                        if (n <= 0) break;
                        read += n;
                    }
                    if (read < sniffLen) head = head[..read];
                }

                if (!options.ForceTextEvenIfNotAllowed && _binary.LooksBinary(head))
                {
                    return Fail(TextLoadFailureReason.BinaryDetected,
                        "バイナリと判定されたため開きません（ヌルバイトを検出）。");
                }

                var det = _encoding.Detect(head);

                // 全体読み込み
                byte[] all = File.ReadAllBytes(fullPath);
                int start = det.BomLength;
                string text = det.Encoding.GetString(all, start, all.Length - start);

                var eol = DetectLineEnding(text);
                text = NormalizeForDisplay(text);

                return new TextLoadOutcome
                {
                    Buffer = new DocumentBuffer
                    {
                        FullPath = fullPath,
                        Text = text,
                        Encoding = det.Kind,
                        LineEnding = eol,
                        IsReadOnly = options.IsReadOnly,
                        ByteSize = fi.Length
                    }
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                return Fail(TextLoadFailureReason.AccessDenied, ex.Message);
            }
            catch (Exception ex)
            {
                return Fail(TextLoadFailureReason.UnknownError, ex.Message);
            }
        }

        private static TextLoadOutcome Fail(TextLoadFailureReason r, string m) =>
            new() { Failure = new TextLoadFailure { Reason = r, Message = m } };

        private static LineEnding DetectLineEnding(string text)
        {
            bool hasCrLf = false, hasLfOnly = false, hasCrOnly = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n') { hasCrLf = true; i++; }
                    else hasCrOnly = true;
                }
                else if (c == '\n')
                {
                    hasLfOnly = true;
                }
            }
            int kinds = (hasCrLf ? 1 : 0) + (hasLfOnly ? 1 : 0) + (hasCrOnly ? 1 : 0);
            if (kinds == 0) return LineEnding.Unknown;
            if (kinds > 1) return LineEnding.Mixed;
            if (hasCrLf) return LineEnding.Crlf;
            if (hasLfOnly) return LineEnding.Lf;
            return LineEnding.Cr;
        }

        /// <summary>
        /// TextBox(Multiline) は CRLF を前提とするため、LF / CR を CRLF に揃える。
        /// </summary>
        private static string NormalizeForDisplay(string text)
        {
            // 既に CRLF だけなら高速パス
            bool hasLone = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n')
                {
                    if (i == 0 || text[i - 1] != '\r') { hasLone = true; break; }
                }
                else if (c == '\r')
                {
                    if (i + 1 >= text.Length || text[i + 1] != '\n') { hasLone = true; break; }
                }
            }
            if (!hasLone) return text;

            var sb = new System.Text.StringBuilder(text.Length + 16);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\r')
                {
                    sb.Append("\r\n");
                    if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                }
                else if (c == '\n')
                {
                    sb.Append("\r\n");
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
