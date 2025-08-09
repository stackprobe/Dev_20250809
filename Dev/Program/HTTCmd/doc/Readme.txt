===============================================================================

　HTTCmd

===============================================================================


■ソフトの概要

　起動するだけで簡単に使える（コマンドライン版の）ウェブサーバーです。
　ドキュメントルート（公開フォルダ）の配下にあるファイルの閲覧・ダウンロードのみ可能です。


■動作環境

　Windows 11


■開発環境（推奨動作環境）

　Windows 11 Home


■インストール方法

　アーカイブの中身をローカルディスク上の任意の場所にコピーして下さい。


■アンインストール方法

　レジストリなどは一切使っていません。
　ファイルを削除するだけでアンインストールできます。


■起動方法

　コマンドライン

　　HTTCmd.exe [DOC-ROOT [PORT-NO [OPTIONS...]]]

　　　DOC-ROOT ⇒ ドキュメントルートのフォルダ (省略時：カレントディレクトリ)
　　　PORT-NO  ⇒ ポート番号 (省略時：80)
　　　OPTIONS  ⇒ 拡張オプションを参照


　実行例

　　HTTCmd.exe C:\www\DocRoot

　　　⇒ "C:\www\DocRoot" をドキュメントルートにして、ポート番号 80 でサーバーを起動する。

　　HTTCmd.exe . 8080

　　　⇒ カレントディレクトリをドキュメントルートにして、ポート番号 8080 でサーバーを起動する。

　　HTTCmd.exe D:\HP 58946

　　　⇒ "D:\HP" をドキュメントルートにして、ポート番号 58946 でサーバーを起動する。

　　HTTCmd.exe

　　　⇒ カレントディレクトリをドキュメントルートにして、ポート番号 80 でサーバーを起動する。


■終了方法

　以下の引数で起動して下さい。実行中のサーバーが停止します。

　　HTTCmd.exe /S


■拡張オプション

　以下のオプションの内必要なものを順不同に指定して下さい。

　　/K

　　　⇒ 非シフト系キー入力でサーバーを停止できるようになります。

　　/T TSV-FILE

　　　⇒ ファイル拡張子と Content-Type の組み合わせを追加(デフォルト設定を上書き)します。
　　　　TSV-FILE には以下の内容のファイルを指定して下さい。

　　　　　ファイル形式：Tab-Separated Values (TSV)
　　　　　文字コード：US-ASCII (UTF-8でも良い)
　　　　　改行コード：CR-LF または LF

　　　　　ｎ行２列、１列目に拡張子、２列目に Content-Type を記述します。
　　　　　拡張子はドットから始まることに注意して下さい。

　　　　　記述例：

　　　　　　.html【水平タブ】text/html【改行】
　　　　　　.xlsx【水平タブ】application/vnd.openxmlformats-officedocument.spreadsheetml.sheet【改行】
　　　　　　.pdf【水平タブ】application/pdf【改行】

　　/H TSV-FILE

　　　⇒ リクエストヘッダの Host の値によってドキュメントルートを切り替えたい場合に使用します。
　　　　TSV-FILE には以下の内容のファイルを指定して下さい。

　　　　　ファイル形式：Tab-Separated Values (TSV)
　　　　　文字コード：UTF-8
　　　　　改行コード：CR-LF または LF

　　　　　ｎ行２列、１列目にホスト名、２列目にドキュメントルートを記述します。
　　　　　どのホスト名にも一致しなかった場合は、コマンド引数に指定されたドキュメントルートを使用します。

　　　　　記述例：

　　　　　　happy-tea-time.test【水平タブ】C:\HTT\DocRoot【改行】
　　　　　　darjeeling-tea.test【水平タブ】D:\Assam\orange-pekoe【改行】
　　　　　　earlgrey.test【水平タブ】E:\Earl Grey【改行】
　　　　　　localhost【水平タブ】C:\HTT\DocRoot【改行】
　　　　　　127.0.0.1【水平タブ】C:\HTT\DocRoot【改行】
　　　　　　127.0.0.2【水平タブ】C:\test2【改行】
　　　　　　127.0.0.3【水平タブ】C:\test3【改行】

　　/N HTML-FILE

　　　⇒ リクエストされたパスが見つからなかった場合に表示される所謂オリジナル４０４ページを設定します。
　　　　ステータスコード = 404, Content-Type: text/html と共に HTML-FILE の内容を応答します。

	/C

　　　⇒ 大きなファイルを応答する場合 Chunked-Encoding を使用します。


　実行例

　　HTTCmd.exe C:\www\DocRoot 80 /K

　　　-- /K のみ

　　HTTCmd.exe C:\www\DocRoot 80 /T C:\www\ContentTypes.tsv

　　　-- /T のみ

　　HTTCmd.exe C:\www\DocRoot 80 /H C:\www\DocRoots.tsv

　　　-- /H のみ

　　HTTCmd.exe C:\www\DocRoot 80 /N C:\www\404.html

　　　-- /N のみ

　　HTTCmd.exe C:\www\DocRoot 80 /C

　　　-- /C のみ

　　HTTCmd.exe C:\www\DocRoot 80 /K /T C:\www\ContentTypes.tsv /H C:\www\DocRoots.tsv /N C:\www\404.html /C

　　　-- 全部


■補足

　本プログラムは HTTCmd.exe 単体で動作します。


■取り扱い種別

　フリーソフト


■作者への連絡先

　stackprobes@gmail.com

