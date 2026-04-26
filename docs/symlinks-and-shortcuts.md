# シンボリックリンク・ショートカット

DocTree は **以下を意図的にツリーから除外** します (読み込まない):

- シンボリックリンク (`mklink`, `mklink /D`)
- ジャンクション (`mklink /J`)
- マウントポイント
- Windows ショートカット (`.lnk`)

## なぜ除外するか

- リンクのループ (A → B → A) でツリー走査が無限になるリスク
- リンク先がアクセス不可だと例外で列挙が止まる
- 同じ実体を複数経路で開くと「同じファイルが別タブで複数開く」混乱が起こる
- `.lnk` の解決は `IShellLink` COM が必要で、標準ライブラリのみという要件と相性が悪い

## 判定方法

`Directory.EnumerateFileSystemInfos` の各エントリに対して以下のいずれかが真ならスキップ:

| 判定 | 対象 |
|---|---|
| `(info.Attributes & FileAttributes.ReparsePoint) != 0` | シンボリックリンク・ジャンクション・マウントポイント |
| `info.LinkTarget is not null` (.NET 6+) | リンク類 (フォールバック) |
| 拡張子が `.lnk` (大文字小文字無視) | Windows ショートカット |

実装: [DocTree/Services/FileSystem/LinkDetector.cs](../DocTree/Services/FileSystem/LinkDetector.cs)

## 回避策: リンク先を見たい場合

リンク先のフォルダを **そのままルートとして追加** してください。

```jsonc
// project-settings.jsonc
"roots": [
  { "name": "Source",   "path": "C:\\Source" },
  { "name": "LinkDest", "path": "D:\\Actual\\Path" }   // リンクの実体パス
]
```

## OneDrive など クラウド同期フォルダ

クラウドオンリーのファイル (`FileAttributes.Offline`, `RecallOnDataAccess`) を開くと
ダウンロードが発生し、UI が固まる可能性があります。

現状はツリー表示はされますが、開く際にネットワーク待ちで応答が悪くなることがあります。
将来のバージョンで設定でスキップ可能にする予定です。
