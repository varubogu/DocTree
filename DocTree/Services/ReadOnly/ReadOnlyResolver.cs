using DocTree.Models;

namespace DocTree.Services.ReadOnly
{
    /// <summary>
    /// 読み取り専用モードの3階層解決:
    ///   優先度（高い順）
    ///     1. overrides[]（path 前方一致最長マッチ, Inherit以外）
    ///     2. roots[i].readOnly（Inherit以外）
    ///     3. アプリ全体 readOnly
    ///   さらに NTFS の FileAttributes.ReadOnly が立っていれば常に ReadOnly に倒す。
    /// </summary>
    public sealed class ReadOnlyResolver
    {
        private readonly AppSettings _settings;

        public ReadOnlyResolver(AppSettings settings)
        {
            _settings = settings;
        }

        public bool IsReadOnly(string fullPath)
        {
            // 1) overrides 最長一致
            PathOverride? best = null;
            int bestLen = -1;
            foreach (var ov in _settings.Overrides)
            {
                if (string.IsNullOrEmpty(ov.Path)) continue;
                if (StartsWithPath(fullPath, ov.Path) && ov.Path.Length > bestLen)
                {
                    best = ov;
                    bestLen = ov.Path.Length;
                }
            }
            if (best is { ReadOnly: not ReadOnlyMode.Inherit })
            {
                if (best.ReadOnly == ReadOnlyMode.ReadOnly) return true;
                if (best.ReadOnly == ReadOnlyMode.Writable) return AttrIsReadOnly(fullPath); // file attr は無視できない
            }

            // 2) ルート
            var root = FindOwningRoot(fullPath);
            if (root is { ReadOnly: not ReadOnlyMode.Inherit })
            {
                if (root.ReadOnly == ReadOnlyMode.ReadOnly) return true;
                if (root.ReadOnly == ReadOnlyMode.Writable) return AttrIsReadOnly(fullPath);
            }

            // 3) アプリ全体
            if (_settings.ReadOnly) return true;

            // 4) NTFS 属性
            return AttrIsReadOnly(fullPath);
        }

        public RootFolder? FindOwningRoot(string fullPath)
        {
            RootFolder? best = null;
            int bestLen = -1;
            foreach (var r in _settings.Roots)
            {
                if (string.IsNullOrEmpty(r.Path)) continue;
                if (StartsWithPath(fullPath, r.Path) && r.Path.Length > bestLen)
                {
                    best = r;
                    bestLen = r.Path.Length;
                }
            }
            return best;
        }

        private static bool StartsWithPath(string fullPath, string prefix)
        {
            if (fullPath.Length < prefix.Length) return false;
            if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            // 完全一致 or 直後がセパレータ
            if (fullPath.Length == prefix.Length) return true;
            char next = fullPath[prefix.Length];
            return next == Path.DirectorySeparatorChar || next == Path.AltDirectorySeparatorChar
                   || prefix.EndsWith(Path.DirectorySeparatorChar) || prefix.EndsWith(Path.AltDirectorySeparatorChar);
        }

        private static bool AttrIsReadOnly(string fullPath)
        {
            try
            {
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath)) return false;
                return (File.GetAttributes(fullPath) & FileAttributes.ReadOnly) != 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
