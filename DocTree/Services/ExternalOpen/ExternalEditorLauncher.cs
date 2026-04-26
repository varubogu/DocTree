using System.Diagnostics;
using DocTree.Models;

namespace DocTree.Services.ExternalOpen
{
    public sealed class ExternalEditorLauncher
    {
        public void Open(ExternalEditor editor, string path, int line = 1, int column = 1)
        {
            if (string.IsNullOrWhiteSpace(editor.Exe))
                throw new InvalidOperationException("外部エディタの 'exe' が空です。");

            var args = (editor.Args ?? "")
                .Replace("{path}", path)
                .Replace("{line}", line.ToString())
                .Replace("{column}", column.ToString());

            var psi = new ProcessStartInfo
            {
                FileName = editor.Exe,
                Arguments = args,
                UseShellExecute = true, // PATH 解決と .bat 等を吸収
                WorkingDirectory = SafeDirOf(path)
            };
            Process.Start(psi);
        }

        public void RevealInExplorer(string fullPath)
        {
            // /select で対象を選択した状態でエクスプローラを開く
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "/select,\"" + fullPath + "\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private static string SafeDirOf(string path)
        {
            try { return Path.GetDirectoryName(path) ?? ""; }
            catch { return ""; }
        }
    }
}
