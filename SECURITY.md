# セキュリティと設定の範囲

## 動作

SLViewerAssociationは、Second Lifeのビューアーを登録し、`secondlife://` リンクを受け付けるWindows用ツールです。

- 標準ユーザー権限で動作します。実行マニフェストは `asInvoker` です。
- 本ツールに通信、広告、テレメトリー、アップロード、自動更新・ダウンロードの機能はありません。
- 検索対象はローカルのインストール情報、標準フォルダー、既知のビューアーのプロセスです。ネットワークドライブやディスク全体の検索は行いません。
- 選んだビューアーを直接起動し、`--url` の値としてリンクを渡します。コマンドシェルを経由しません。
- 設定画面または選択画面を閉じると処理を終了します。サービス、スタートアップ、常駐プロセスは登録しません。
- セキュリティ機能の無効化、Defenderの除外登録、UACの回避は行いません。

ビューアー自身のログイン・通信は、それぞれのビューアーが行います。

## 変更する設定

「選んだ設定を適用」を押すと、現在のユーザーの次のレジストリ情報を設定します。

```text
HKEY_CURRENT_USER\Software\Classes\secondlife
HKEY_CURRENT_USER\Software\Classes\SLViewerAssociation.SecondLife
HKEY_CURRENT_USER\Software\SLViewerAssociation\Capabilities
HKEY_CURRENT_USER\Software\RegisteredApplications
  値: SLViewerAssociation
```

リンクの受付先として本ツールを登録します。HTTP/HTTPS全体の既定のブラウザーは変更しません。
Windowsの `UserChoice` を直接書き換える機能はありません。Windows側の既定のアプリが優先される場合は、利用者がWindowsの設定画面で選択します。

変更前のユーザー設定をバックアップし、「設定前に戻す」から復元します。
自動検索、一覧への登録・編集・削除だけでは、Windowsの関連付けは変更しません。

## 個人情報を含むファイル

```text
%LOCALAPPDATA%\SLViewerAssociation\viewers.xml
%LOCALAPPDATA%\SLViewerAssociation\before.xml
```

登録名、実行ファイルのパス、前回の選択、動作モード、復元用の関連付け情報を保存します。
開いた場所URL、アカウント、パスワードは保存しません。
これらのXMLにはユーザー名などを含むパスが入る場合があります。Gitや公開配布物へ含めないでください。

## 署名とWindowsの警告

現在の配布exeにはAuthenticode署名がありません。未署名のアプリや評判の蓄積がないファイルは、SmartScreenやSmart App Controlなどにより警告・ブロックされる可能性があります。
ソース公開・GPLライセンス・ローカルでのテストは、署名やWindowsの評価を代替するものではありません。

実際の判定はWindowsの設定やファイルの評価に依存し、警告やブロックが発生しないことは保証しません。
警告を出さないために難読化や実行ファイルの圧縮を行ったり、セキュリティを迂回したりする設計にはしていません。

公開配布の署名を整備する場合は、発行元を確認できる証明書によるコード署名やMicrosoft Store配布などを検討できます。署名があるだけでSmartScreenの警告が必ずなくなるわけではありません。

Microsoftの説明：

- [SmartScreen reputation for Windows app developers](https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation)
- [Smart App Control FAQ](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions)

## 不具合・問題の報告

再現手順、ツールのバージョン、Windowsのバージョン、表示されたエラーメッセージを添えて報告してください。
登録XMLやバックアップを添える場合は、個人PCのパスなどを確認し、公開する必要のない情報を取り除いてください。
