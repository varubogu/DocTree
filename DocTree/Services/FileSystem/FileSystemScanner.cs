using DocTree.Models;

namespace DocTree.Services.FileSystem
{
    public sealed class FsEntry
    {
        public required string FullPath { get; init; }
        public required string Name { get; init; }
        public required bool IsDirectory { get; init; }
    }

    /// <summary>
    /// フォルダ列挙。シンボリックリンク・.lnk・除外名は弾く。
    /// 例外（アクセス拒否など）は黙って飛ばし、列挙を継続する。
    /// </summary>
    public sealed class FileSystemScanner
    {
        public IEnumerable<FsEntry> EnumerateChildren(string folderPath, RootFolder owningRoot)
        {
            DirectoryInfo dir;
            try
            {
                dir = new DirectoryInfo(folderPath);
                if (!dir.Exists) yield break;
            }
            catch
            {
                yield break;
            }

            IEnumerable<FileSystemInfo> children;
            try
            {
                children = dir.EnumerateFileSystemInfos();
            }
            catch
            {
                yield break;
            }

            var excludeSet = new HashSet<string>(
                owningRoot.Exclude ?? new(),
                StringComparer.OrdinalIgnoreCase);

            // ディレクトリ → ファイル の順、それぞれ名前順
            var snapshot = new List<FileSystemInfo>();
            using (var e = children.GetEnumerator())
            {
                while (true)
                {
                    try
                    {
                        if (!e.MoveNext()) break;
                    }
                    catch
                    {
                        // 1要素ぶん飛ばすのが難しいので列挙打ち切り
                        break;
                    }
                    snapshot.Add(e.Current);
                }
            }

            foreach (var info in snapshot
                .OrderByDescending(i => i is DirectoryInfo)
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (excludeSet.Contains(info.Name)) continue;
                if (LinkDetector.ShouldSkip(info)) continue;

                yield return new FsEntry
                {
                    FullPath = info.FullName,
                    Name = info.Name,
                    IsDirectory = info is DirectoryInfo
                };
            }
        }
    }
}
