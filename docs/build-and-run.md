# Build & Run

## 必須環境

- Windows 10 / 11
- .NET 10 SDK (`net10.0-windows`)

## ビルド

```
dotnet build DocTree.slnx
```

## デバッグ実行

```
dotnet run --project DocTree/DocTree.csproj
```

## リリースビルド

```
dotnet build DocTree.slnx -c Release
```

成果物は `DocTree/bin/Release/net10.0-windows/` に出力されます。

## Self-contained 配布 (.NET ランタイムを同梱)

```
dotnet publish DocTree/DocTree.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true
```

成果物は `DocTree/bin/Release/net10.0-windows/win-x64/publish/` に出力されます。
配布する場合は exe と同じフォルダに `settings.jsonc` を置けば **ポータブル運用** ができます。

## Framework-dependent 配布 (.NET ランタイム別途必要)

```
dotnet publish DocTree/DocTree.csproj -c Release -r win-x64 --self-contained false
```

エンドユーザーが .NET 10 デスクトップランタイムをインストール済みなら、
こちらの方が exe が軽量です。

## アンインストール

DocTree はインストーラを持ちません。以下を削除すれば完全にクリーンな状態に戻ります。

- exe を置いたフォルダ
- `%AppData%\DocTree\` (`settings.jsonc`, `state.json` が入っています)
