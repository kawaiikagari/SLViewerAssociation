# SLViewerAssociation

Second Lifeのリンクを開くWindows用ビューアーを指定・変更する設定ツールです。

地域名や座標などのURLを固定せず、リンクをクリックしたときのURLを選択したビューアーへ渡します。

## 使い方

1. `SLViewerAssociation.exe` をダブルクリックします。
2. 使用するビューアーの `.exe` を選びます。
3. **このビューアーに設定** を押します。

以後は普段通りリンクを開いてください。HTTPSの地図ページでは **Teleport** を押します。

詳細は [取扱説明書](取扱説明書.md) を参照してください。

## ファイル構成

- `SLViewerAssociation.exe`：設定ツール本体
- `SLViewerAssociation.exe.config`：.NET Frameworkの実行設定
- `取扱説明書.md`：操作方法、復元方法、困ったときの対処
- `使い方.txt`：テキスト形式の簡易説明
- `source/`：C#ソース、マニフェスト、ビルドスクリプト
- `LICENSE`：GNU GPL v3のライセンス全文

## 動作環境

Windows 10 / 11、.NET Framework 4.8。

通常の利用にコンパイラーや開発環境は不要です。ユーザー単位の関連付けを変更するため、管理者権限も不要です。
ビューアーは `--url` 引数に対応している必要があります。

## 開発・管理

管理先：`C:\Users\KawaiiKagari\Documents\GitHub\SLViewerAssociation`

実装はC# / Windows Formsです。ソースを変更して再ビルドする場合だけ、PowerShellから次を実行します。

```powershell
.\source\build.ps1
```

ビルドスクリプトはWindowsの .NET Framework 付属C#コンパイラーを使用し、リポジトリ直下の実行ファイルを生成します。
再ビルド前に設定ツールを終了してください。

設定ツール自体のフォルダーを移動しても、指定したビューアーへの関連付けはそのままです。
ビューアーの実行ファイルを移動・削除した場合は、指定し直してください。

## ライセンス

**GNU General Public License v3.0 or later (`GPL-3.0-or-later`)**。

本プログラムはGNU GPLバージョン3、またはそれ以降のバージョンの条件に従って再配布・改変できます。
本プログラムは無保証です。ライセンスの条件と全文は [LICENSE](LICENSE) を参照してください。
配布時には実行ファイルとともに、対応するソースコードとライセンスを含めてください。
