# 読み取り専用モード

DocTree は閲覧専用ツールですが、誤編集を防ぐため明示的に読み取り専用を 3 階層で制御できます。

## 優先順位 (高い順)

| 順位 | 設定箇所 | キー |
|---|---|---|
| 1 | パス単位オーバーライド (前方一致最長マッチ) | `overrides[].readOnly` |
| 2 | ルートフォルダ単位 | `roots[i].readOnly` |
| 3 | アプリ全体 | `readOnly` (トップレベル) |

それぞれの設定値:

| 値 | 意味 |
|---|---|
| `"inherit"` | 上位階層に従う |
| `"readOnly"` | この階層で読み取り専用に固定 |
| `"writable"` | この階層で編集可能に固定 (NTFS 属性 `ReadOnly` は尊重) |

加えて、NTFS の `FileAttributes.ReadOnly` 属性が立っているファイルは
**常に読み取り専用** に倒されます (誤編集防止)。

## 例

```jsonc
{
  "readOnly": false,                   // アプリ全体: 編集可
  "roots": [
    { "name": "A", "path": "C:\\A", "readOnly": "readOnly" },   // A 配下: 全部 RO
    { "name": "B", "path": "C:\\B", "readOnly": "inherit"  }    // B 配下: アプリ全体に従う = 編集可
  ],
  "overrides": [
    { "path": "C:\\A\\drafts", "readOnly": "writable" },        // A 配下だけど drafts は編集可
    { "path": "C:\\B\\sealed",  "readOnly": "readOnly" }        // B 配下だけど sealed は RO
  ]
}
```

| パス | 結果 | 理由 |
|---|---|---|
| `C:\A\notes\foo.md` | RO | root A が ReadOnly、override 該当なし |
| `C:\A\drafts\bar.md` | 編集可 | override `C:\A\drafts` が Writable (最長一致) |
| `C:\B\baz.md` | 編集可 | root B = inherit, アプリ全体 = false |
| `C:\B\sealed\qux.md` | RO | override `C:\B\sealed` が ReadOnly |

## ステータスバー表示

ファイルを開くと右下に **「読取専用」** バッジ (オレンジ) が表示されます。
バッジが出ていれば、その時のタブの TextBox は ReadOnly になっています。
