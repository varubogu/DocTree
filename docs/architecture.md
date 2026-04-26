# Architecture

## レイヤー構成

```
DocTree/
├── App/         アプリ起動・サービス寄せ場 (手動DI)
├── Models/      設定・バッファのPOCO定義
├── Services/    機能ロジック (画面非依存・テスト容易)
│   ├── Settings/      JSONC 読込、初期テンプレ生成
│   ├── State/         アプリ状態 (タブ復元等) の永続化
│   ├── FileSystem/    ツリー列挙、リンク・パス分類
│   ├── Text/          バイナリ判定、エンコーディング判定、テキスト読込
│   ├── ReadOnly/      3階層の読み取り専用解決
│   └── ExternalOpen/  外部エディタ起動、エクスプローラ表示
├── Forms/       Windows Forms 画面 (MainForm)
├── Controls/    再利用UIコントロール (将来用、現状は MainForm 内に直接実装)
└── Resources/   埋め込みリソース (default-settings.jsonc)
```

## 起動シーケンス

```
Program.Main
 └─ AppContext.Bootstrap
     ├─ SettingsLocator.Locate           (exe同梱 → %AppData% の順)
     ├─ DefaultSettingsWriter.WriteDefaultIfMissing  (初回のみ)
     ├─ SettingsLoader.Load              (JSONC → AppSettings)
     └─ ReadOnlyResolver / FileSystemScanner を作成
 └─ MainForm(appContext) を表示
     ├─ AppStateStore.Load → ウィンドウ位置・タブ復元
     └─ ルート群をツリーに追加
```

## ファイルを開く流れ

```
TreeNode dblclick / Enter
  → MainForm.OpenFile(path)
      → ReadOnlyResolver.IsReadOnly(path)   (3階層解決)
      → TextLoader.Load(path, opts)
          ├─ BinaryDetector.IsExtensionAllowed
          ├─ 先頭 N バイト読み
          ├─ BinaryDetector.LooksBinary
          ├─ EncodingDetector.Detect       (BOM → UTF-8厳格 → fallback)
          └─ File.ReadAllBytes → Encoding.GetString → CRLF 正規化
      → CreateTabForBuffer(buf)            (TabPage + TextBox 生成)
      → UpdateStatusFromBuffer             (Encoding/EOL/RO バッジ)
```

## リモートパスの遅延展開

```
TreeView.BeforeExpand
  if dummy node only:
      if PathClassifier.IsRemote:
          Task.Run( EnumerateChildren )    ← UI スレッドをブロックしない
            └─ BeginInvoke → ApplyChildren
      else:
          PopulateChildren                  ← 同期で OK (高速)
```

## 設計方針

- **画面非依存ロジックはすべて `Services/` 配下** に置き、`Forms/` から呼ぶ
- **DIコンテナは入れない**。依存は `AppContext` から手動で配線 (規模相応)
- **設定はファイルのみ**。GUI 設定画面を作らない判断 (要件)
- **NuGet 禁止**。`System.Text.Json` の JSONC コメントスキップで設定を読む
- **シンボリックリンク・バイナリは「触らない」** ことで安全性とシンプルさを両立
