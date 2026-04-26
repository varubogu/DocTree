# DocTree

VSCode / Obsidian 風の **閲覧専用** ドキュメントビューア (Windows / .NET 10 / Windows Forms)。

- 左ペイン: フォルダツリー (エクスプローラ)
- 右ペイン: タブ付きエディタ
- 設定はすべて `settings.jsonc` で管理 (GUI 設定なし)
- 標準ライブラリのみで実装 (NuGet 依存なし)

## 主な特徴

- **シンボリックリンク・ショートカット (.lnk) は表示しない** (実体のみ)
- **ネットワークドライブ対応** (UNC, マップドライブ)
- **テキストファイル限定**で開く (バイナリは開かない)
- **読み取り専用モード**: アプリ全体 / ルートフォルダ単位 / パス単位 の3階層
- **「○○で開く」**: サクラエディタや VS Code を右クリックメニューから起動

## ドキュメント

- [getting-started.md](getting-started.md) — 初回起動とルートフォルダ追加
- [configuration.md](configuration.md) — `settings.jsonc` の全項目
- [read-only-modes.md](read-only-modes.md) — 読み取り専用モードの優先順位
- [encoding.md](encoding.md) — 対応エンコーディングと制限
- [external-editors.md](external-editors.md) — 「○○で開く」設定例
- [symlinks-and-shortcuts.md](symlinks-and-shortcuts.md) — リンクを除外する理由
- [architecture.md](architecture.md) — 設計概要
- [build-and-run.md](build-and-run.md) — ビルド・実行・配布
- [keyboard-shortcuts.md](keyboard-shortcuts.md) — ショートカット一覧
- [troubleshooting.md](troubleshooting.md) — トラブルシューティング
