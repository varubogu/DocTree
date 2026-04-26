# Getting Started

## 1. ビルド・起動

```
dotnet build DocTree.slnx
dotnet run --project DocTree/DocTree.csproj
```

初回起動時、設定ファイルが見つからない場合は自動で以下に生成されます。

```
%AppData%\DocTree\settings.jsonc
```

## 2. ルートフォルダを追加する

DocTree は **ルートフォルダ** を起点にツリーを表示します。

### 方法 A: 設定ファイルを直接編集

`%AppData%\DocTree\settings.jsonc` を開き、`roots` 配列に追記:

```jsonc
"roots": [
  {
    "name": "Work Docs",
    "path": "C:\\Users\\me\\Documents\\Work",
    "readOnly": "inherit"
  }
]
```

保存後、メニュー **[ファイル] > [設定を再読み込み]** (Ctrl+R) で反映。

### 方法 B: メニュー [ファイル] > [ルートフォルダを追加...]

フォルダ選択ダイアログでパスを選ぶと、追記すべき JSON 断片が表示されます。
それを `settings.jsonc` の `roots` 配列にコピペし、Ctrl+R で再読込してください。

(設定 GUI は意図的に持たず、すべてファイル編集で完結する方針です)

## 3. ファイルを開く

ツリーでファイルをダブルクリック (または Enter) で右ペインに新しいタブで開きます。

開けないファイルは、メッセージボックスで理由が表示されます。

- バイナリと判定された (ヌルバイトを検出)
- 拡張子が `textExtensions` ホワイトリストに含まれない
- アクセスが拒否された / ファイルサイズが大きすぎる

「拡張子未登録」のファイルは右クリック > **テキストとして強制で開く** で開けます。

## 4. ポータブル運用

`exe` と同じフォルダに `settings.jsonc` を置くと、`%AppData%` よりも優先されます。
USB メモリで持ち運ぶ運用にも対応します。

ただしアプリ状態 (タブ復元・ウィンドウ位置) は常に `%AppData%\DocTree\state.json` に書かれます。
