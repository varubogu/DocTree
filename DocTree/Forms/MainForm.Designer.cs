namespace DocTree.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem fileAddRootMenu;
        private ToolStripMenuItem fileOpenSettingsMenu;
        private ToolStripMenuItem fileReloadSettingsMenu;
        private ToolStripSeparator fileSeparator1;
        private ToolStripMenuItem fileExitMenu;
        private ToolStripMenuItem viewMenu;
        private ToolStripMenuItem viewToggleExplorerMenu;
        private ToolStripMenuItem viewWordWrapMenu;
        private ToolStripMenuItem helpMenu;
        private ToolStripMenuItem helpOpenDocsMenu;
        private ToolStripMenuItem helpAboutMenu;
        private SplitContainer splitContainer;
        private TreeView explorerTree;
        private ContextMenuStrip explorerContextMenu;
        private ToolStripMenuItem ctxOpen;
        private ToolStripMenuItem ctxOpenWith;
        private ToolStripSeparator ctxSep1;
        private ToolStripMenuItem ctxRevealInExplorer;
        private ToolStripMenuItem ctxCopyPath;
        private ToolStripMenuItem ctxRefresh;
        private ToolStripSeparator ctxSep2;
        private ToolStripMenuItem ctxOpenAsText;
        private TabControl documentTabs;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusPath;
        private ToolStripStatusLabel statusEncoding;
        private ToolStripStatusLabel statusEol;
        private ToolStripStatusLabel statusReadOnly;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            menuStrip = new MenuStrip();
            fileMenu = new ToolStripMenuItem();
            fileAddRootMenu = new ToolStripMenuItem();
            fileOpenSettingsMenu = new ToolStripMenuItem();
            fileReloadSettingsMenu = new ToolStripMenuItem();
            fileSeparator1 = new ToolStripSeparator();
            fileExitMenu = new ToolStripMenuItem();
            viewMenu = new ToolStripMenuItem();
            viewToggleExplorerMenu = new ToolStripMenuItem();
            viewWordWrapMenu = new ToolStripMenuItem();
            helpMenu = new ToolStripMenuItem();
            helpOpenDocsMenu = new ToolStripMenuItem();
            helpAboutMenu = new ToolStripMenuItem();
            splitContainer = new SplitContainer();
            explorerTree = new TreeView();
            explorerContextMenu = new ContextMenuStrip(components);
            ctxOpen = new ToolStripMenuItem();
            ctxOpenWith = new ToolStripMenuItem();
            ctxSep1 = new ToolStripSeparator();
            ctxRevealInExplorer = new ToolStripMenuItem();
            ctxCopyPath = new ToolStripMenuItem();
            ctxRefresh = new ToolStripMenuItem();
            ctxSep2 = new ToolStripSeparator();
            ctxOpenAsText = new ToolStripMenuItem();
            documentTabs = new TabControl();
            statusStrip = new StatusStrip();
            statusPath = new ToolStripStatusLabel();
            statusEncoding = new ToolStripStatusLabel();
            statusEol = new ToolStripStatusLabel();
            statusReadOnly = new ToolStripStatusLabel();

            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();

            // menuStrip
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, helpMenu });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.TabIndex = 0;

            // fileMenu
            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                fileAddRootMenu, fileOpenSettingsMenu, fileReloadSettingsMenu, fileSeparator1, fileExitMenu
            });
            fileMenu.Name = "fileMenu";
            fileMenu.Text = "ファイル(&F)";

            fileAddRootMenu.Name = "fileAddRootMenu";
            fileAddRootMenu.Text = "ルートフォルダを追加...";
            fileAddRootMenu.ShortcutKeys = Keys.Control | Keys.Shift | Keys.O;

            fileOpenSettingsMenu.Name = "fileOpenSettingsMenu";
            fileOpenSettingsMenu.Text = "設定ファイルを開く";

            fileReloadSettingsMenu.Name = "fileReloadSettingsMenu";
            fileReloadSettingsMenu.Text = "設定を再読み込み";
            fileReloadSettingsMenu.ShortcutKeys = Keys.Control | Keys.R;

            fileExitMenu.Name = "fileExitMenu";
            fileExitMenu.Text = "終了";

            // viewMenu
            viewMenu.DropDownItems.AddRange(new ToolStripItem[] { viewToggleExplorerMenu, viewWordWrapMenu });
            viewMenu.Name = "viewMenu";
            viewMenu.Text = "表示(&V)";

            viewToggleExplorerMenu.Name = "viewToggleExplorerMenu";
            viewToggleExplorerMenu.Text = "エクスプローラを表示/非表示";
            viewToggleExplorerMenu.ShortcutKeys = Keys.Control | Keys.B;
            viewToggleExplorerMenu.CheckOnClick = true;
            viewToggleExplorerMenu.Checked = true;

            viewWordWrapMenu.Name = "viewWordWrapMenu";
            viewWordWrapMenu.Text = "折り返し";
            viewWordWrapMenu.ShortcutKeys = Keys.Alt | Keys.Z;
            viewWordWrapMenu.CheckOnClick = true;

            // helpMenu
            helpMenu.DropDownItems.AddRange(new ToolStripItem[] { helpOpenDocsMenu, helpAboutMenu });
            helpMenu.Name = "helpMenu";
            helpMenu.Text = "ヘルプ(&H)";

            helpOpenDocsMenu.Name = "helpOpenDocsMenu";
            helpOpenDocsMenu.Text = "ドキュメントを開く";

            helpAboutMenu.Name = "helpAboutMenu";
            helpAboutMenu.Text = "バージョン情報";

            // splitContainer
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 24);
            splitContainer.Name = "splitContainer";
            splitContainer.Orientation = Orientation.Vertical;
            splitContainer.Panel1.Controls.Add(explorerTree);
            splitContainer.Panel2.Controls.Add(documentTabs);
            splitContainer.Size = new Size(1000, 576);
            splitContainer.SplitterDistance = 280;
            splitContainer.TabIndex = 1;

            // explorerContextMenu
            explorerContextMenu.Items.AddRange(new ToolStripItem[]
            {
                ctxOpen, ctxOpenWith, ctxSep2, ctxOpenAsText, ctxSep1,
                ctxRevealInExplorer, ctxCopyPath, ctxRefresh
            });
            explorerContextMenu.Name = "explorerContextMenu";

            ctxOpen.Name = "ctxOpen"; ctxOpen.Text = "開く";
            ctxOpenWith.Name = "ctxOpenWith"; ctxOpenWith.Text = "別なアプリで開く";
            ctxOpenAsText.Name = "ctxOpenAsText"; ctxOpenAsText.Text = "テキストとして強制で開く";
            ctxRevealInExplorer.Name = "ctxRevealInExplorer"; ctxRevealInExplorer.Text = "エクスプローラで表示";
            ctxCopyPath.Name = "ctxCopyPath"; ctxCopyPath.Text = "パスをコピー";
            ctxRefresh.Name = "ctxRefresh"; ctxRefresh.Text = "再読み込み";

            // explorerTree
            explorerTree.Dock = DockStyle.Fill;
            explorerTree.HideSelection = false;
            explorerTree.Name = "explorerTree";
            explorerTree.TabIndex = 0;
            explorerTree.ShowRootLines = true;
            explorerTree.ShowLines = true;
            explorerTree.ContextMenuStrip = explorerContextMenu;

            // documentTabs
            documentTabs.Dock = DockStyle.Fill;
            documentTabs.Name = "documentTabs";
            documentTabs.TabIndex = 0;
            documentTabs.DrawMode = TabDrawMode.OwnerDrawFixed;

            // statusStrip
            statusStrip.Items.AddRange(new ToolStripItem[]
            {
                statusPath, statusEncoding, statusEol, statusReadOnly
            });
            statusStrip.Location = new Point(0, 600);
            statusStrip.Name = "statusStrip";
            statusStrip.TabIndex = 2;

            statusPath.Name = "statusPath";
            statusPath.Spring = true;
            statusPath.TextAlign = ContentAlignment.MiddleLeft;

            statusEncoding.Name = "statusEncoding";
            statusEncoding.Text = "";

            statusEol.Name = "statusEol";
            statusEol.Text = "";

            statusReadOnly.Name = "statusReadOnly";
            statusReadOnly.Text = "";

            // MainForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 622);
            Controls.Add(splitContainer);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            Name = "MainForm";
            Text = "DocTree";

            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
