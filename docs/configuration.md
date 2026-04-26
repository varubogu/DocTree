# 設定ファイル リファレンス

JSONC (JSON with Comments) 形式。`//` と `/* ... */` のコメント、末尾カンマが許容されます。

DocTree の設定は、ユーザー設定とプロジェクト設定に分かれています。

- `settings.jsonc`: 表示、テキスト判定、外部エディタなどユーザーに対する設定
- `project-settings.jsonc`: `roots` と `overrides` などプロジェクトに対する設定

## 探索順

1. `<exeディレクトリ>\settings.jsonc` (ポータブル)
2. `%AppData%\DocTree\settings.jsonc` (通常)
3. どちらもなければ `2.` に埋め込みリソースから自動生成

`project-settings.jsonc` は、採用された `settings.jsonc` と同じディレクトリから読み込まれます。存在しなければ初期テンプレートが自動生成されます。

既存互換のため、`project-settings.jsonc` が存在しない場合に限り、`settings.jsonc` 内の `roots` / `overrides` も読み込めます。新規設定では `project-settings.jsonc` を使ってください。

## `settings.jsonc` の項目

| キー | 型 | 既定 | 説明 |
|---|---|---|---|
| `readOnly` | bool | `true` | アプリ全体の読み取り専用モード |
| `textExtensions` | string[] | (組み込み一覧) | テキスト判定ホワイトリスト (小文字, ドット付き) |
| `textFilenames` | string[] | `["README", "LICENSE", "Makefile", "Dockerfile", "CHANGELOG"]` | 拡張子なしでもテキスト扱いするファイル名 (大文字小文字無視) |
| `binarySniffBytes` | int | `8192` | バイナリ判定で読む先頭バイト数 |
| `defaultEncoding` | string | `"utf-8"` | BOMなし時のフォールバック (`utf-8` / `utf-16le` / `utf-16be`) |
| `font` | object | `{family: "Consolas", size: 11.0}` | 表示フォント |
| `exclude` | string[] | `[".git"]` | 全ルート共通で除外するフォルダ・ファイル名 (完全一致, 大文字小文字無視) |
| `externalEditors` | ExternalEditor[] | `[]` | 「別なアプリで開く」候補 |

## `project-settings.jsonc` の項目

| キー | 型 | 既定 | 説明 |
|---|---|---|---|
| `roots` | RootFolder[] | `[]` | ルートフォルダ一覧 |
| `overrides` | PathOverride[] | `[]` | パス単位の readOnly 上書き |

## RootFolder

| キー | 型 | 説明 |
|---|---|---|
| `name` | string | ツリー最上位に表示される名前 |
| `path` | string | ローカル絶対パスまたは UNC (`\\server\share\...`) |
| `readOnly` | `"inherit"` \| `"readOnly"` \| `"writable"` | このルート配下の読み取り専用設定 |
| `exclude` | string[] | このルート配下で追加除外するフォルダ・ファイル名 (完全一致, 大文字小文字無視) |

## PathOverride

| キー | 型 | 説明 |
|---|---|---|
| `path` | string | 前方一致で対象化するパス。最長一致が勝つ |
| `readOnly` | `"inherit"` \| `"readOnly"` \| `"writable"` | 上書きする読み取り専用設定 |

## ExternalEditor

| キー | 型 | 説明 |
|---|---|---|
| `name` | string | コンテキストメニューに表示する名前 |
| `exe` | string | 実行ファイルのフルパス、または PATH 上の名前 |
| `args` | string | 引数テンプレート。`{path}` `{line}` `{column}` を置換 |
| `default` | bool | true の項目は太字表示 (将来: ダブルクリック既定にする予定) |

## 完全サンプル

[configuration-sample.jsonc](configuration-sample.jsonc) と [project-configuration-sample.jsonc](project-configuration-sample.jsonc) を参照。
