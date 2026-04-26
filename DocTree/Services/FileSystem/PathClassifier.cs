namespace DocTree.Services.FileSystem
{
    public enum PathKind
    {
        Local,
        Unc,
        MappedNetwork,
        Unknown
    }

    public static class PathClassifier
    {
        public static PathKind Classify(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return PathKind.Unknown;

            // UNC: \\server\share\...
            if (path.StartsWith(@"\\", StringComparison.Ordinal)) return PathKind.Unc;

            // ドライブレター
            if (path.Length >= 2 && path[1] == ':')
            {
                try
                {
                    var driveInfo = new DriveInfo(path[..1]);
                    return driveInfo.DriveType switch
                    {
                        DriveType.Network => PathKind.MappedNetwork,
                        DriveType.Fixed or DriveType.Removable
                            or DriveType.CDRom or DriveType.Ram => PathKind.Local,
                        _ => PathKind.Unknown,
                    };
                }
                catch
                {
                    return PathKind.Unknown;
                }
            }

            return PathKind.Unknown;
        }

        public static bool IsRemote(string path)
        {
            var kind = Classify(path);
            return kind == PathKind.Unc || kind == PathKind.MappedNetwork;
        }
    }
}
