using System.Diagnostics;
using DocTree.App;
using DocTree.Models;
using DocTree.Services.ExternalOpen;
using DocTree.Services.FileSystem;
using DocTree.Services.Settings;
using DocTree.Services.State;
using DocTree.Services.Text;

namespace DocTree.Forms
{
    public partial class MainForm : Form
    {
        private const string DummyNodeKey = "__dummy__";

        private readonly App.AppContext _appContext;
        private TextLoader _textLoader = null!;
        private BinaryDetector _binaryDetector = null!;
        private EncodingDetector _encodingDetector = null!;
        private readonly ExternalEditorLauncher _launcher = new();

        public MainForm() : this(App.AppContext.Bootstrap()) { }

        public MainForm(App.AppContext appContext)
        {
            _appContext = appContext;
            InitializeComponent();
            RebuildTextServices();

            WireMenuEvents();
            WireExplorerEvents();

            LoadRootsIntoTree();
            UpdateStatus(path: "", encoding: "", eol: "", readOnly: _appContext.Settings.ReadOnly);
            Text = $"DocTree — {_appContext.SettingsPath} / {_appContext.ProjectSettingsPath}";

            Load += OnFormLoadRestoreState;
            FormClosing += OnFormClosingPersistState;
        }

        private void RebuildTextServices()
        {
            _binaryDetector = new BinaryDetector(_appContext.Settings);
            _encodingDetector = new EncodingDetector(_appContext.Settings);
            _textLoader = new TextLoader(_binaryDetector, _encodingDetector);
        }

        private void WireMenuEvents()
        {
            fileAddRootMenu.Click += OnAddRoot;
            fileOpenSettingsMenu.Click += OnOpenSettings;
            fileReloadSettingsMenu.Click += OnReloadSettings;
            fileExitMenu.Click += (_, _) => Close();

            viewToggleExplorerMenu.CheckedChanged += (_, _) =>
                splitContainer.Panel1Collapsed = !viewToggleExplorerMenu.Checked;
            viewWordWrapMenu.CheckedChanged += (_, _) => ApplyWordWrapToAllTabs();

            helpOpenDocsMenu.Click += OnOpenDocs;
            helpAboutMenu.Click += OnAbout;
        }

        private void WireExplorerEvents()
        {
            explorerTree.BeforeExpand += OnTreeBeforeExpand;
            explorerTree.AfterSelect += OnTreeAfterSelect;
            explorerTree.NodeMouseDoubleClick += (_, e) => OpenNode(e.Node, toggleDirectory: false);
            explorerTree.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    OpenNode(explorerTree.SelectedNode, toggleDirectory: true);
                    e.Handled = true;
                }
            };

            documentTabs.DrawItem += OnTabDrawItem;
            documentTabs.MouseDown += OnTabMouseDown;
            KeyPreview = true;
            KeyDown += OnFormKeyDown;

            // Context menu
            explorerTree.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = explorerTree.GetNodeAt(e.Location);
                    if (hit is not null) explorerTree.SelectedNode = hit;
                }
            };
            explorerContextMenu.Opening += OnContextMenuOpening;
            ctxOpen.Click += (_, _) => OpenNode(explorerTree.SelectedNode, toggleDirectory: true);
            ctxOpenAsText.Click += (_, _) => ForceOpenAsText(explorerTree.SelectedNode);
            ctxRevealInExplorer.Click += (_, _) => RevealSelected();
            ctxCopyPath.Click += (_, _) => CopyPathOfSelected();
            ctxRefresh.Click += (_, _) => RefreshSelectedNode();
            ctxRemoveRoot.Click += (_, _) => RemoveSelectedRoot();
        }

        // ----- Tree -----

        private void LoadRootsIntoTree()
        {
            explorerTree.BeginUpdate();
            try
            {
                explorerTree.Nodes.Clear();
                foreach (var root in _appContext.Settings.Roots)
                {
                    var node = new TreeNode(string.IsNullOrWhiteSpace(root.Name) ? root.Path : root.Name)
                    {
                        Tag = new NodeTag(root.Path, true, root)
                    };
                    if (Directory.Exists(root.Path))
                    {
                        node.Nodes.Add(new TreeNode("...") { Name = DummyNodeKey });
                    }
                    else
                    {
                        node.ForeColor = SystemColors.GrayText;
                        node.Text += "  (パスが見つかりません)";
                    }
                    explorerTree.Nodes.Add(node);
                }

                if (explorerTree.Nodes.Count == 0)
                {
                    var hint = new TreeNode("ルートフォルダが未設定です。[ファイル] > [ルートフォルダを追加...] から追加してください。")
                    {
                        ForeColor = SystemColors.GrayText
                    };
                    explorerTree.Nodes.Add(hint);
                }
            }
            finally
            {
                explorerTree.EndUpdate();
            }
        }

        private void OnTreeBeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node is null) return;
            if (e.Node.Tag is not NodeTag tag || !tag.IsDirectory) return;

            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Name == DummyNodeKey)
            {
                if (PathClassifier.IsRemote(tag.Path))
                {
                    // リモートはバックグラウンドで列挙し、UIに戻して反映
                    var node = e.Node;
                    var loadingNode = node.Nodes[0];
                    loadingNode.Text = "読み込み中...";

                    _ = System.Threading.Tasks.Task.Run(() =>
                    {
                        var entries = new List<FsEntry>();
                        try
                        {
                            foreach (var entry in _appContext.Scanner.EnumerateChildren(tag.Path, _appContext.Settings, tag.OwningRoot))
                                entries.Add(entry);
                        }
                        catch { /* ignore */ }
                        BeginInvoke(new Action(() => ApplyChildren(node, tag, entries)));
                    });
                }
                else
                {
                    e.Node.Nodes.Clear();
                    PopulateChildren(e.Node, tag);
                }
            }
        }

        private void ApplyChildren(TreeNode node, NodeTag tag, List<FsEntry> entries)
        {
            node.Nodes.Clear();
            foreach (var entry in entries)
            {
                var child = new TreeNode(entry.Name)
                {
                    Tag = new NodeTag(entry.FullPath, entry.IsDirectory, tag.OwningRoot)
                };
                if (entry.IsDirectory)
                    child.Nodes.Add(new TreeNode("...") { Name = DummyNodeKey });
                node.Nodes.Add(child);
            }
        }

        private void OnTreeAfterSelect(object? sender, TreeViewEventArgs e)
        {
            var path = (e.Node?.Tag as NodeTag)?.Path ?? "";
            statusPath.Text = path;
            // 開いているタブがなければ、選択中ノードの readOnly を表示
            if (documentTabs.TabCount == 0 && !string.IsNullOrEmpty(path))
            {
                var ro = _appContext.ReadOnly.IsReadOnly(path);
                statusReadOnly.Text = ro ? "読取専用" : "";
                statusReadOnly.ForeColor = ro ? Color.OrangeRed : SystemColors.ControlText;
            }
        }

        private void PopulateChildren(TreeNode parentNode, NodeTag parentTag)
        {
            foreach (var entry in _appContext.Scanner.EnumerateChildren(parentTag.Path, _appContext.Settings, parentTag.OwningRoot))
            {
                var child = new TreeNode(entry.Name)
                {
                    Tag = new NodeTag(entry.FullPath, entry.IsDirectory, parentTag.OwningRoot)
                };
                if (entry.IsDirectory)
                {
                    child.Nodes.Add(new TreeNode("...") { Name = DummyNodeKey });
                }
                parentNode.Nodes.Add(child);
            }
        }

        // ----- Open file -----

        private void OpenNode(TreeNode? node, bool toggleDirectory)
        {
            if (node?.Tag is not NodeTag tag) return;
            if (tag.IsDirectory)
            {
                if (toggleDirectory)
                {
                    node.Toggle();
                }
                return;
            }
            OpenFile(tag.Path);
        }

        private void OpenFile(string path)
        {
            // 既に開いていればそのタブをアクティブに
            foreach (TabPage existing in documentTabs.TabPages)
            {
                if (existing.Tag is DocumentBuffer buf &&
                    string.Equals(buf.FullPath, path, StringComparison.OrdinalIgnoreCase))
                {
                    documentTabs.SelectedTab = existing;
                    UpdateStatusFromBuffer(buf);
                    return;
                }
            }

            var outcome = _textLoader.Load(path, new TextLoadOptions
            {
                IsReadOnly = _appContext.ReadOnly.IsReadOnly(path)
            });

            if (!outcome.Success)
            {
                ShowOpenFailure(path, outcome.Failure!);
                return;
            }

            CreateTabForBuffer(outcome.Buffer!);
        }

        private void ShowOpenFailure(string path, TextLoadFailure failure)
        {
            var prefix = failure.Reason switch
            {
                TextLoadFailureReason.NotAllowedByExtension => "テキスト拡張子のホワイトリストに含まれていません。",
                TextLoadFailureReason.BinaryDetected         => "バイナリファイルのため開けません。",
                TextLoadFailureReason.FileNotFound           => "ファイルが見つかりません。",
                TextLoadFailureReason.AccessDenied           => "アクセスが拒否されました。",
                TextLoadFailureReason.TooLarge               => "ファイルサイズが大きすぎます。",
                _ => "開けませんでした。"
            };
            MessageBox.Show(this,
                prefix + "\n\n" + path + "\n\n" + failure.Message,
                "DocTree", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void CreateTabForBuffer(DocumentBuffer buf)
        {
            var page = new TabPage(Path.GetFileName(buf.FullPath))
            {
                ToolTipText = buf.FullPath,
                Tag = buf
            };

            var viewer = new TextViewerControl
            {
                WordWrap = viewWordWrapMenu.Checked,
                Dock = DockStyle.Fill,
                Font = new Font(FontFamily.GenericMonospace, _appContext.Settings.Font.Size),
                TextContent = buf.Text,
                ReadOnly = buf.IsReadOnly
            };
            viewer.SelectStart();

            page.Controls.Add(viewer);
            documentTabs.TabPages.Add(page);
            documentTabs.SelectedTab = page;
            UpdateStatusFromBuffer(buf);
        }

        private void ApplyWordWrapToAllTabs()
        {
            foreach (TabPage page in documentTabs.TabPages)
            {
                foreach (Control c in page.Controls)
                {
                    if (c is TextViewerControl viewer)
                    {
                        viewer.WordWrap = viewWordWrapMenu.Checked;
                    }
                }
            }
        }

        // ----- Tab close (X / middle-click / Ctrl+W) -----

        private void OnTabDrawItem(object? sender, DrawItemEventArgs e)
        {
            var tabRect = documentTabs.GetTabRect(e.Index);
            var page = documentTabs.TabPages[e.Index];
            var closeRect = GetCloseRect(tabRect);

            using var bg = new SolidBrush(e.State.HasFlag(DrawItemState.Selected)
                ? SystemColors.Window : SystemColors.Control);
            e.Graphics.FillRectangle(bg, tabRect);

            using var fg = new SolidBrush(SystemColors.ControlText);
            var titleRect = new Rectangle(tabRect.Left + 6, tabRect.Top + 4,
                tabRect.Width - 24, tabRect.Height - 4);
            TextRenderer.DrawText(e.Graphics, page.Text, documentTabs.Font, titleRect,
                SystemColors.ControlText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // ×
            using var pen = new Pen(SystemColors.ControlDarkDark, 1.5f);
            int pad = 3;
            e.Graphics.DrawLine(pen,
                closeRect.Left + pad, closeRect.Top + pad,
                closeRect.Right - pad, closeRect.Bottom - pad);
            e.Graphics.DrawLine(pen,
                closeRect.Right - pad, closeRect.Top + pad,
                closeRect.Left + pad, closeRect.Bottom - pad);
        }

        private static Rectangle GetCloseRect(Rectangle tabRect)
        {
            const int s = 14;
            return new Rectangle(tabRect.Right - s - 4, tabRect.Top + (tabRect.Height - s) / 2, s, s);
        }

        private void OnTabMouseDown(object? sender, MouseEventArgs e)
        {
            for (int i = 0; i < documentTabs.TabCount; i++)
            {
                var tabRect = documentTabs.GetTabRect(i);
                if (!tabRect.Contains(e.Location)) continue;

                if (e.Button == MouseButtons.Middle)
                {
                    CloseTab(i);
                    return;
                }
                if (e.Button == MouseButtons.Left && GetCloseRect(tabRect).Contains(e.Location))
                {
                    CloseTab(i);
                    return;
                }
            }
        }

        private void CloseTab(int index)
        {
            if (index < 0 || index >= documentTabs.TabCount) return;
            var page = documentTabs.TabPages[index];
            documentTabs.TabPages.RemoveAt(index);
            page.Dispose();
            if (documentTabs.TabCount == 0)
                UpdateStatus("", "", "", _appContext.Settings.ReadOnly);
            else
            {
                var sel = documentTabs.SelectedTab;
                if (sel?.Tag is DocumentBuffer b) UpdateStatusFromBuffer(b);
            }
        }

        private void OnFormKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.W)
            {
                CloseTab(documentTabs.SelectedIndex);
                e.Handled = true;
            }
        }

        // ----- Menu handlers -----

        private void OnAddRoot(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "DocTree に追加するルートフォルダを選択してください",
                ShowNewFolderButton = false
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var selectedPath = Path.GetFullPath(dlg.SelectedPath);
                var root = new RootFolder
                {
                    Name = GetDefaultRootName(selectedPath),
                    Path = selectedPath,
                    ReadOnly = ReadOnlyMode.Inherit
                };

                var added = SettingsRootWriter.AddRoot(_appContext.ProjectSettingsPath, root);
                if (!added)
                {
                    MessageBox.Show(this,
                        "このルートフォルダは既に設定されています。\n\n" + selectedPath,
                        "ルートフォルダの追加",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                OnReloadSettings(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ShowError("ルートフォルダを追加できませんでした。", ex);
            }
        }

        private void OnOpenSettings(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _appContext.SettingsPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowError("設定ファイルを開けませんでした。", ex);
            }
        }

        private void OnReloadSettings(object? sender, EventArgs e)
        {
            try
            {
                _appContext.ReloadSettings();
                RebuildTextServices();
                LoadRootsIntoTree();
                UpdateStatus(path: "", encoding: "", eol: "", readOnly: _appContext.Settings.ReadOnly);
            }
            catch (Exception ex)
            {
                ShowError("設定ファイルの再読み込みに失敗しました。", ex);
            }
        }

        private void OnOpenDocs(object? sender, EventArgs e)
        {
            var exeDocs = Path.Combine(AppPaths.ExeDirectory, "docs");
            var repoDocs = Path.GetFullPath(Path.Combine(AppPaths.ExeDirectory, "..", "..", "..", "..", "docs"));
            var target = Directory.Exists(exeDocs) ? exeDocs
                       : Directory.Exists(repoDocs) ? repoDocs
                       : null;
            if (target is null)
            {
                MessageBox.Show(this, "docs フォルダが見つかりませんでした。", "DocTree",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try { Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true }); }
            catch (Exception ex) { ShowError("docs フォルダを開けませんでした。", ex); }
        }

        private void OnAbout(object? sender, EventArgs e)
        {
            MessageBox.Show(this,
                "DocTree\n\nWindows Forms 製の閲覧専用ドキュメントビューア。\n" +
                "標準ライブラリのみで実装されています。",
                "バージョン情報",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ----- Helpers -----

        private void UpdateStatus(string path, string encoding, string eol, bool readOnly)
        {
            statusPath.Text = path;
            statusEncoding.Text = encoding;
            statusEol.Text = eol;
            statusReadOnly.Text = readOnly ? "読取専用" : "";
            statusReadOnly.ForeColor = readOnly ? Color.OrangeRed : SystemColors.ControlText;
        }

        private void UpdateStatusFromBuffer(DocumentBuffer buf)
        {
            UpdateStatus(buf.FullPath, buf.Encoding.Display(), buf.LineEnding.Display(), buf.IsReadOnly);
        }

        private void ShowError(string message, Exception ex)
        {
            MessageBox.Show(this, message + "\n\n" + ex.Message, "DocTree",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static string GetDefaultRootName(string path)
        {
            var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var name = Path.GetFileName(trimmed);
            return string.IsNullOrWhiteSpace(name) ? trimmed : name;
        }

        // ----- Context menu -----

        private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var node = explorerTree.SelectedNode;
            var tag = node?.Tag as NodeTag;

            bool hasPath = tag is not null && !string.IsNullOrEmpty(tag.Path);
            bool isFile = hasPath && tag!.IsDirectory == false;
            bool isDir  = hasPath && tag!.IsDirectory;
            bool isRoot = hasPath && node?.Parent is null && tag!.IsDirectory;

            ctxOpen.Enabled = hasPath;
            ctxOpenAsText.Enabled = isFile;
            ctxRevealInExplorer.Enabled = hasPath;
            ctxCopyPath.Enabled = hasPath;
            ctxRefresh.Enabled = isDir;
            ctxRemoveRoot.Visible = isRoot;
            ctxRemoveRoot.Enabled = isRoot;

            // 「別なアプリで開く」サブメニューを動的構築
            ctxOpenWith.DropDownItems.Clear();
            var editors = _appContext.Settings.ExternalEditors ?? new();
            if (editors.Count == 0 || !isFile)
            {
                var none = new ToolStripMenuItem("(設定された外部エディタがありません)") { Enabled = false };
                ctxOpenWith.DropDownItems.Add(none);
                ctxOpenWith.Enabled = isFile;
            }
            else
            {
                foreach (var ed in editors)
                {
                    var item = new ToolStripMenuItem(ed.Name)
                    {
                        Tag = ed,
                        Font = ed.Default ? new Font(ctxOpenWith.Font, FontStyle.Bold) : ctxOpenWith.Font
                    };
                    item.Click += (_, _) => LaunchExternal(ed, tag!.Path);
                    ctxOpenWith.DropDownItems.Add(item);
                }
                ctxOpenWith.Enabled = true;
            }
        }

        private void LaunchExternal(ExternalEditor ed, string path)
        {
            try { _launcher.Open(ed, path); }
            catch (Exception ex) { ShowError($"\"{ed.Name}\" で開けませんでした。", ex); }
        }

        private void ForceOpenAsText(TreeNode? node)
        {
            if (node?.Tag is not NodeTag tag || tag.IsDirectory) return;
            var outcome = _textLoader.Load(tag.Path, new TextLoadOptions
            {
                IsReadOnly = _appContext.ReadOnly.IsReadOnly(tag.Path),
                ForceTextEvenIfNotAllowed = true
            });
            if (!outcome.Success) { ShowOpenFailure(tag.Path, outcome.Failure!); return; }
            CreateTabForBuffer(outcome.Buffer!);
        }

        private void RevealSelected()
        {
            if (explorerTree.SelectedNode?.Tag is not NodeTag tag) return;
            try { _launcher.RevealInExplorer(tag.Path, tag.IsDirectory); }
            catch (Exception ex) { ShowError("エクスプローラで表示できませんでした。", ex); }
        }

        private void CopyPathOfSelected()
        {
            if (explorerTree.SelectedNode?.Tag is not NodeTag tag) return;
            try { Clipboard.SetText(tag.Path); }
            catch { /* クリップボード一時ロックはスルー */ }
        }

        private void RefreshSelectedNode()
        {
            var node = explorerTree.SelectedNode;
            if (node?.Tag is not NodeTag tag || !tag.IsDirectory) return;
            node.Nodes.Clear();
            if (Directory.Exists(tag.Path))
            {
                node.Nodes.Add(new TreeNode("...") { Name = DummyNodeKey });
                node.Collapse();
                node.Expand();
            }
        }

        private void RemoveSelectedRoot()
        {
            if (explorerTree.SelectedNode?.Tag is not NodeTag tag) return;

            var result = MessageBox.Show(this,
                "このルートフォルダの設定を解除しますか？\n\n" + tag.Path,
                "ルートフォルダの設定解除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return;

            try
            {
                var removed = SettingsRootWriter.RemoveRoot(_appContext.ProjectSettingsPath, tag.OwningRoot.Path);
                if (!removed)
                {
                    MessageBox.Show(this,
                        "このルートフォルダは設定ファイル内に見つかりませんでした。\n\n" + tag.Path,
                        "ルートフォルダの設定解除",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                OnReloadSettings(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ShowError("ルートフォルダの設定を解除できませんでした。", ex);
            }
        }

        // ----- App state restore / persist -----

        private void OnFormLoadRestoreState(object? sender, EventArgs e)
        {
            var st = AppStateStore.Load();
            if (st.WindowWidth is int w and > 200 && st.WindowHeight is int h and > 150)
            {
                StartPosition = FormStartPosition.Manual;
                Size = new Size(w, h);
            }
            if (st.WindowX is int x && st.WindowY is int y)
            {
                var screen = Screen.FromPoint(new Point(x, y)).WorkingArea;
                if (screen.Contains(x, y)) Location = new Point(x, y);
            }
            if (st.Maximized == true) WindowState = FormWindowState.Maximized;
            if (st.SplitterDistance is int sd && sd > 50 && sd < ClientSize.Width - 50)
                splitContainer.SplitterDistance = sd;
            if (st.WordWrap == true) viewWordWrapMenu.Checked = true;

            foreach (var p in st.OpenFiles)
            {
                if (File.Exists(p)) OpenFile(p);
            }
            if (!string.IsNullOrEmpty(st.ActiveFile))
            {
                foreach (TabPage page in documentTabs.TabPages)
                {
                    if (page.Tag is DocumentBuffer b &&
                        string.Equals(b.FullPath, st.ActiveFile, StringComparison.OrdinalIgnoreCase))
                    {
                        documentTabs.SelectedTab = page;
                        break;
                    }
                }
            }
        }

        private void OnFormClosingPersistState(object? sender, FormClosingEventArgs e)
        {
            var st = new AppState
            {
                WindowX = WindowState == FormWindowState.Normal ? Location.X : null,
                WindowY = WindowState == FormWindowState.Normal ? Location.Y : null,
                WindowWidth = WindowState == FormWindowState.Normal ? Size.Width : null,
                WindowHeight = WindowState == FormWindowState.Normal ? Size.Height : null,
                Maximized = WindowState == FormWindowState.Maximized,
                SplitterDistance = splitContainer.SplitterDistance,
                WordWrap = viewWordWrapMenu.Checked,
                ActiveFile = (documentTabs.SelectedTab?.Tag as DocumentBuffer)?.FullPath,
                OpenFiles = documentTabs.TabPages
                    .Cast<TabPage>()
                    .Select(p => (p.Tag as DocumentBuffer)?.FullPath ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList()
            };
            AppStateStore.Save(st);
        }

        private sealed record NodeTag(string Path, bool IsDirectory, RootFolder OwningRoot);
    }
}
