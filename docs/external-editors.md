# 「○○で開く」設定

ツリーで右クリック > **「○○で開く」** から、設定済みの外部エディタで対象ファイルを起動できます。

## 設定例

`settings.jsonc` の `externalEditors` 配列に追加します。

### サクラエディタ

```jsonc
{
  "name": "サクラエディタ",
  "exe":  "C:\\Program Files (x86)\\sakura\\sakura.exe",
  "args": "\"{path}\""
}
```

### VS Code

```jsonc
{
  "name": "VS Code",
  "exe":  "code",                         // PATH 上の code を解決
  "args": "--goto \"{path}:{line}\"",     // 行番号は将来対応 (現状は 1 固定)
  "default": true                          // 太字表示
}
```

### 秀丸エディタ

```jsonc
{
  "name": "秀丸",
  "exe":  "C:\\Program Files\\Hidemaru\\Hidemaru.exe",
  "args": "\"{path}\" /j{line}"
}
```

### メモ帳

```jsonc
{
  "name": "メモ帳",
  "exe":  "notepad.exe",
  "args": "\"{path}\""
}
```

## 引数のプレースホルダ

| プレースホルダ | 内容 |
|---|---|
| `{path}` | 対象ファイルのフルパス |
| `{line}` | 行番号 (現状は常に `1`) |
| `{column}` | 列番号 (現状は常に `1`) |

## 動作

- `UseShellExecute = true` で起動するため、`code` のような PATH 解決や `.bat` も動作します
- パスにスペースを含む可能性があるため、`args` 中の `{path}` は `"..."` で囲ってください
- 既定 (`default: true`) のエディタは UI 上で太字表示されます

## トラブルシュート

- 「指定されたファイルが見つかりません」 → `exe` のパスが正しいか、PATH に通っているか確認
- 引数が解釈されない → `args` のクォートが正しいか確認 (二重引用符は `\"` でエスケープ)
