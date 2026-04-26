namespace DocTree.Services.FileSystem
{
    /// <summary>
    /// シンボリックリンク・ジャンクション・マウントポイント・Windowsショートカット(.lnk)
    /// を検出する。これらは DocTree のツリーから除外される。
    /// </summary>
    public static class LinkDetector
    {
        public static bool ShouldSkip(FileSystemInfo info)
        {
            try
            {
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0) return true;
                if (info.LinkTarget is not null) return true;
            }
            catch
            {
                // 属性取得で例外 → アクセスできないものはスキップ
                return true;
            }

            if (info is FileInfo fi &&
                string.Equals(fi.Extension, ".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// クラウドオンリー (OneDrive など) のファイルを検出。
        /// 開くとダウンロードを発生させる可能性があるため、必要に応じてスキップする。
        /// </summary>
        public static bool IsCloudOnly(FileSystemInfo info)
        {
            try
            {
                const FileAttributes RecallOnDataAccess = (FileAttributes)0x400000;
                var a = info.Attributes;
                return (a & FileAttributes.Offline) != 0
                    || (a & RecallOnDataAccess) != 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
