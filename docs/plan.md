# DocTree 実装プラン

## Context

VSCode / Obsidian 風のドキュメント閲覧アプリ「DocTree」を Windows Forms (.NET 10) で新規開発する。閲覧専用に振り切ることでシンプルかつ軽量に保ち、設定はファイルベースで完結させる（GUIなし）。サードパーティライブラリ（NuGet）は使わず、標準ライブラリのみで実装する。シンボリックリンク・ショートカットは混乱の元になるため除外し、テキスト以外（バイナリ）は開かない方針で安全性も確保する。

現状は `DocTree.slnx` + `DocTree/DocTree.csproj`（`net10.0-windows`、`UseWindowsForms=true`）に空の `Form1` がある状態。これを土台に、機能を段階的に積み上げていく。

### 確定仕様（ユーザー回答済み）
- 設定ファイル形式: **JSONC**（`System.Text.Json` の `JsonCommentHandling.Skip` で読込）
- 文字エンコーディング対応: **UTF-8 / UTF-16 のみ**（Shift_JIS は対応しない。BOMなし非UTF8は化けることをdocsで明記）
- ルートフォルダ: **複数同時対応**（VSCode風）
- 設定/状態ファイル配置: **exe同梱を優先 → なければ `%AppData%\DocTree\` を使用**。アプリ状態（最後に開いたタブ・ウィンドウ位置・最近のフォルダ）は常に `%AppData%\DocTree\state.json` に分離

---

## プロジェクト構成

```
DocTree/
├── DocTree.slnx                              （既存）
├── docs/                                     （新規・後述ドキュメント群）
└── DocTree/
    ├── DocTree.csproj                        （既存・要編集: docsをルートに移すため Folder Include を削除）
    ├── Program.cs                            （既存・要編集: AppContextを起動し MainForm を渡す）
    ├── App/
    │   ├── AppContext.cs                     設定ロード結果と各サービスの寄せ場（手動DI）
    │   └── AppPaths.cs                       exeディレクトリ / %AppData%\DocTree の解決
    ├── Models/
    │   ├── AppSettings.cs                    設定全体のルートPOCO
    │   ├── RootFolder.cs                     ルートフォルダ定義
    │   ├── ExternalEditor.cs                 「別なアプリで開く」エディタ定義
    │   ├── PathOverride.cs                   パス単位ReadOnly上書き
    │   ├── ReadOnlyMode.cs                   enum: Inherit / ReadOnly / Writable
    │   └── DocumentBuffer.cs                 開いたファイルのテキスト＋メタ情報
    ├── Services/
    │   ├── Settings/
    │   │   ├── SettingsLocator.cs            exe同梱→%AppData% の探索順
    │   │   ├── SettingsLoader.cs             JSONC読込
    │   │   └── DefaultSettingsWriter.cs      初回起動時のテンプレ生成（埋め込みリソース）
    │   ├── State/
    │   │   └── AppStateStore.cs              %AppData%\DocTree\state.json の読み書き
    │   ├── FileSystem/
    │   │   ├── FileSystemScanner.cs          ツリー列挙・遅延展開
    │   │   ├── LinkDetector.cs               ReparsePoint / .lnk 判定
    │   │   └── PathClassifier.cs             UNC / ローカル / マップド判定
    │   ├── Text/
    │   │   ├── BinaryDetector.cs             ヌルバイト + 拡張子ホワイトリスト
    │   │   ├── EncodingDetector.cs           BOM + UTF-8妥当性検証（UTF-8/16のみ）
    │   │   └── TextLoader.cs                 検出 + 読込 + 行終端正規化
    │   ├── ReadOnly/
    │   │   └── ReadOnlyResolver.cs           app < root < override の3階層解決
    │   └── ExternalOpen/
    │       └── ExternalEditorLauncher.cs     Process.Start ラッパ
    ├── Forms/
    │   ├── MainForm.cs / .Designer.cs        メイン画面
    │   └── AboutForm.cs                      バージョン情報
    ├── Controls/
    │   ├── ExplorerTreeView.cs               TreeView派生（遅延読込・コンテキストメニュー連携）
    │   ├── DocumentTabControl.cs             TabControl派生（×ボタン・ミドルクリック閉じ）
    │   └── DocumentEditor.cs                 TextBox(Multiline)ラップ・ReadOnly反映
    └── Resources/
        └── default-settings.jsonc            初回テンプレ（埋め込みリソース）
```

---

## 画面設計（MainForm）

```
MainForm
├── MenuStrip (Top)
│   ├── ファイル(F) … ルート追加 / 設定ファイルを開く / 設定再読込(Ctrl+R) / 終了
│   ├── 表示(V)   … エクスプローラ表示切替(Ctrl+B) / 折り返し(Alt+Z)
│   └── ヘルプ(H) … docsフォルダを開く / バージョン情報
├── SplitContainer (Fill, Vertical)
│   ├── Panel1: ExplorerTreeView
│   │   └── ContextMenu: 開く / 新しいタブで開く / ─ / 別なアプリで開く▶(動的) / エクスプローラで表示 / パスをコピー / 再読込
│   └── Panel2: DocumentTabControl
│       └── 各 TabPage に DocumentEditor (TextBox Multiline)
└── StatusStrip (Bottom)
    └── 現在パス / エンコーディング / 行終端(CRLF/LF) / 「読取専用」バッジ
```

主要ホットキー: `Ctrl+W` タブを閉じる、`Ctrl+Tab` タブ切替、`Ctrl+B` エクスプローラ表示切替、`Ctrl+R` 設定再読込。

エディタ本体は標準 `TextBox(Multiline=true)` で十分（閲覧専用、シンタックスハイライトは入れない）。`RichTextBox` は大ファイルで重く行終端処理が独特なので避ける。タブの×ボタンは `TabControl.DrawMode = OwnerDrawFixed` のオーナードローで実装。

---

## 設定ファイル仕様

### 探索順
1. `<exeディレクトリ>\settings.jsonc` （あればポータブル運用）
2. `%AppData%\DocTree\settings.jsonc` （通常運用）
3. どちらもなければ `2` に埋め込みリソースから生成

### サンプル `settings.jsonc`

```jsonc
{
  // アプリ全体の読み取り専用モード。true なら全ファイル編集不可。
  "readOnly": true,

  // テキスト判定のホワイトリスト拡張子（小文字, ドット付き）
  "textExtensions": [".txt", ".md", ".markdown", ".log", ".csv", ".tsv",
                     ".json", ".jsonc", ".xml", ".yaml", ".yml", ".ini",
                     ".cs", ".ts", ".js", ".py", ".sql", ".html", ".css"],

  // 拡張子なしでもテキスト扱いするファイル名（完全一致, 大文字小文字無視）
  "textFilenames": ["README", "LICENSE", "Makefile", "Dockerfile"],

  // バイナリ判定で先頭何バイトを読むか
  "binarySniffBytes": 8192,

  // BOMなし時のフォールバック ("utf-8" | "utf-16le" | "utf-16be")
  "defaultEncoding": "utf-8",

  // 表示フォント
  "font": { "family": "Consolas", "size": 11.0 },

  // ルートフォルダ（複数指定可）
  "roots": [
    {
      "name": "Work Docs",
      "path": "C:\\Users\\me\\Documents\\Work",
      "readOnly": "inherit",       // "inherit" | "readOnly" | "writable"
      "exclude": [".git", "node_modules", "bin", "obj"]
    },
    {
      "name": "Shared",
      "path": "\\\\fileserver\\share\\docs",
      "readOnly": "readOnly"
    }
  ],

  // ファイル/フォルダ単位のオーバーライド（path は前方一致, 最長一致が勝つ）
  "overrides": [
    { "path": "C:\\Users\\me\\Documents\\Work\\drafts", "readOnly": "writable" }
  ],

  // 「別なアプリで開く」候補。{path} {line} {column} を置換
  "externalEditors": [
    { "name": "サクラエディタ", "exe": "C:\\Program Files (x86)\\sakura\\sakura.exe",
      "args": "\"{path}\"", "default": false },
    { "name": "VS Code", "exe": "code", "args": "--goto \"{path}:{line}\"" }
  ]
}
```

設定はユーザー手編集前提とし、アプリは書き換えない（コメント保持を諦める代わりに編集の自由度を確保）。アプリが書き込む値（最後のウィンドウ位置・直近フォルダ・タブ復元情報）は `%AppData%\DocTree\state.json` に分離。

---

## 主要ロジック

### ReadOnly 3階層解決（ReadOnlyResolver）

優先度（高い順）:
1. `overrides[]` のパス前方一致最長マッチ（`Inherit` でないもの）
2. ファイルが属する `roots[i].readOnly`（`Inherit` でないもの）
3. アプリ全体 `readOnly`

加えて、NTFS の `FileAttributes.ReadOnly` が立っていれば常に ReadOnly に倒す（誤編集防止のため安全側）。

### シンボリックリンク・ショートカット判定（LinkDetector）

`Directory.EnumerateFileSystemInfos` の各エントリに対して以下のいずれかが真ならツリーから除外（読み込まない）:
- `(info.Attributes & FileAttributes.ReparsePoint) != 0`（シンボリックリンク・ジャンクション・マウントポイント）
- `info.LinkTarget is not null`（.NET 6+ API）
- ファイル拡張子が `.lnk`（Windowsショートカット）

UNC上は権限・実装で挙動差があるため、列挙時の例外は握りつぶしてスキップ。OneDriveのクラウドオンリー (`FileAttributes.Offline` / `RecallOnDataAccess`) も負荷回避のためスキップ可能にする（設定項目化は v2 候補）。

### バイナリ判定（BinaryDetector）

二段構え:
1. 拡張子が `textExtensions` または ファイル名が `textFilenames` に含まれる → テキスト候補へ
2. 候補に対し、先頭 `binarySniffBytes` を読み `0x00` を含めばバイナリ。ただし**先にBOM判定でUTF-16と確定したら 0x00 検出はスキップ**

非候補（拡張子未登録）はメニューに「テキストとして強制で開く」を出して逃げ道を確保。

### エンコーディング判定（EncodingDetector）

UTF-8/UTF-16 のみサポート:
- `EF BB BF` → UTF-8 (BOM)
- `FF FE` → UTF-16 LE
- `FE FF` → UTF-16 BE
- BOMなし: `Encoding.UTF8` を `DecoderExceptionFallback` で厳格デコード試行 → 成功なら UTF-8 / 失敗なら設定の `defaultEncoding` で読み直し（化ける場合あり）

ステータスバーに判定結果と行終端を表示。VSCode風の "Reopen with Encoding" メニューで再オープン可能にする（v1.1）。

### 「別なアプリで開く」（ExternalEditorLauncher）

```csharp
var args = editor.Args
    .Replace("{path}", path)
    .Replace("{line}", line.ToString())
    .Replace("{column}", col.ToString());

Process.Start(new ProcessStartInfo {
    FileName = editor.Exe,
    Arguments = args,
    UseShellExecute = true,        // PATH解決と.bat吸収
    WorkingDirectory = Path.GetDirectoryName(path) ?? ""
});
```

`default: true` のエディタを ContextMenu の最上位 / ダブルクリックに割当て可能（設定で切替）。

---

## 実装順序（マイルストーン）

| M | 内容 | 概要 |
|---|---|---|
| **M0** | スケルトン整備 | `Form1` → `MainForm` リネーム、`SplitContainer + TreeView + TabControl + StatusStrip + MenuStrip` 配置。`docs/` 作成、csprojの `Folder Include` 削除（実体作るため不要） |
| **M1** | 設定読込 + 単一ルート表示 | `SettingsLocator/Loader/DefaultWriter`、`AppContext`、`FileSystemScanner`（同期版）、`LinkDetector`。リンク・.lnk 除外でツリー表示 |
| **M2** | ファイルを開く + テキスト判定 + エンコーディング | ダブルクリック / Enter で開く → `BinaryDetector` → `EncodingDetector` → `TextLoader` → 新規タブに表示。StatusStrip に encoding/EOL/ReadOnly バッジ |
| **M3** | 読み取り専用3階層 + 複数ルート | `ReadOnlyResolver`、ツリー最上位を「ルート名」グループノードに、`overrides[]` 反映 |
| **M4** | 「別なアプリで開く」 + 右クリックメニュー充実 | `ExternalEditorLauncher`、ContextMenu の動的構築、エクスプローラで表示 / パスコピー / 再読込 |
| **M5** | 遅延展開 + ネットワークドライブ最適化 | TreeView `BeforeExpand` でその場列挙（ダミーノード方式）、UNCのI/Oは `Task.Run` でUIスレッド非ブロック化 |
| **M6** | 仕上げ + docs整備 | About、ショートカット、フォント反映、エラーハンドリング、`AppStateStore`（タブ復元）、`docs/` 一通り |

Phase 2候補（v2以降）: クイックオープン (Ctrl+P)、全文検索、Markdownプレビュー（要 WebView2 = NuGet なので要相談）、シンタックスハイライト、`Reopen with Encoding`、`FileSystemWatcher`。

---

## 重要ファイル一覧（実装で触るもの）

### 既存・修正
- [DocTree/DocTree.csproj](DocTree/DocTree.csproj) — 埋め込みリソース追加、`Folder Include` 削除
- [DocTree/Program.cs](DocTree/Program.cs) — `AppContext` 初期化 → `MainForm` 渡し
- [DocTree/Form1.cs](DocTree/Form1.cs) / [DocTree/Form1.Designer.cs](DocTree/Form1.Designer.cs) — `MainForm` にリネームして本格実装

### 新規（中核）
- `DocTree/Forms/MainForm.cs` — 全マイルストーン中核
- `DocTree/Services/Settings/SettingsLoader.cs` — JSONC読込、全機能の入口
- `DocTree/Services/FileSystem/FileSystemScanner.cs` — ツリー列挙とリンク除外
- `DocTree/Services/Text/EncodingDetector.cs` — BOM + UTF-8妥当性検証
- `DocTree/Services/ReadOnly/ReadOnlyResolver.cs` — 3階層優先順位
- `DocTree/Services/ExternalOpen/ExternalEditorLauncher.cs` — 外部エディタ起動
- `DocTree/Resources/default-settings.jsonc` — 初回テンプレ

---

## docs フォルダに作成するドキュメント

- `docs/README.md` — 概要・特徴
- `docs/getting-started.md` — 初回起動手順、ルートフォルダの追加方法
- `docs/configuration.md` — `settings.jsonc` 全項目リファレンス
- `docs/configuration-sample.jsonc` — フルサンプル（実ファイル）
- `docs/read-only-modes.md` — 3階層優先順位の説明（表 + 例）
- `docs/encoding.md` — UTF-8/UTF-16のみ対応の理由・限界・`defaultEncoding` 切替方法
- `docs/external-editors.md` — 「別なアプリで開く」設定例（サクラエディタ・VS Code・秀丸）
- `docs/symlinks-and-shortcuts.md` — なぜ除外するか、判定方法、回避策（リンク先を root として手動追加）
- `docs/architecture.md` — Models / Services / Forms / Controls の責務、依存関係
- `docs/build-and-run.md` — ビルド方法、配布方法（self-contained publish 例）
- `docs/keyboard-shortcuts.md` — ショートカット一覧
- `docs/troubleshooting.md` — UNC遅い・OneDrive固まる・非UTF-8が化ける時の対処

---

## 検証方法（End-to-End）

各マイルストーン後に以下を手動確認:

### ビルド確認
```bash
dotnet build DocTree.slnx
```

### 起動確認（M0以降）
```bash
dotnet run --project DocTree/DocTree.csproj
```
MainForm が表示され、左ツリー / 右タブ / メニュー / ステータスバーが見えること。

### 機能確認シナリオ
1. **設定ロード（M1）**: 初回起動で `%AppData%\DocTree\settings.jsonc` が生成され、編集後 `Ctrl+R` で再読込されること
2. **ルート追加（M1）**: settings.jsonc に複数 root を書く → 再起動でツリーに反映
3. **リンク除外（M1）**: `mklink` で作ったシンボリックリンク / `.lnk` ファイルがツリーに表示されないこと
4. **ファイル開閉（M2）**:
   - `.txt` `.md` `.cs` (UTF-8) を開く → 正しく表示、エンコーディング表示が "UTF-8"
   - UTF-8 BOM ファイル → "UTF-8 (BOM)"
   - UTF-16 LE/BE BOM ファイル → "UTF-16 LE" / "UTF-16 BE"
   - `.exe` `.png` 等のバイナリ → 開かない（メッセージ表示）
   - 拡張子未登録ファイル → 開かない、「テキストとして強制で開く」を案内
5. **ReadOnly 3階層（M3）**:
   - app=true → 全ファイルReadOnlyバッジ
   - app=false, root=ReadOnly → そのroot配下のみReadOnly
   - overrides で root配下の特定パスを Writable に → そこだけ編集可
6. **複数ルート（M3）**: 2つ以上の root を設定 → ツリー最上位に root 名が並ぶ
7. **UNC/ネットワークドライブ（M3-M5）**:
   - `\\server\share\` 形式で root 設定 → ツリーに表示
   - マップドライブ `Z:\...` でも同様
   - 列挙が遅くてもUIが固まらないこと（M5以降）
8. **「別なアプリで開く」（M4）**: 右クリック → サクラエディタが起動し、対象ファイルが開かれる
9. **状態復元（M6）**: 起動時に前回開いていたタブが復元される、ウィンドウ位置が維持される

### 自動テストについて
M1-M3 の Services 層（純粋ロジック）は、後で xUnit プロジェクトを追加してユニットテスト可能（NuGet禁止だが xUnit はテストプロジェクト向けで本体に同梱しない場合は議論余地あり。MVP では手動テストで進め、テスト導入は v2 で検討）。