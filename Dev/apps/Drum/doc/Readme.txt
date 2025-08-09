====
Drum
====


----
コマンド

Drum.exe 入力フォルダ 出力ファイル

	圧縮

Drum.exe /D 入力ファイル 出力フォルダ

	展開


----
圧縮ファイルの推奨拡張子

	.drum.gz

例：

	CompressedFile.drum.gz


----
補足

★圧縮対象ファイルのうち同じ内容のファイルデータを共通化します。
同じ内容の大きなファイルが多い場合、高圧縮が期待できます。


入出力の例：

	以下のフォルダ・ファイル構成があった場合

		C:\Data\Input\.
		|
		+--- SubDate\.
		|    |
		|    +--- SubData.dat
		|    +--- SubData2.dat
		|    +--- SubData3.dat
		|
		+--- Data.dat
		+--- Data2.dat
		+--- Data3.dat

	Drum.exe C:\Data\Input C:\temp\AsshukuZumi.drum.gz

	で C:\temp\AsshukuZumi.drum.gz が作成される。

	Drum.exe /D C:\temp\AsshukuZumi.drum.gz C:\temp2\Soba\Udon\Output

	で、以下のフォルダ・ファイル構成が再現される。

		C:\temp2\Soba\Udon\Output\.
		|
		+--- SubDate\.
		|    |
		|    +--- SubData.dat
		|    +--- SubData2.dat
		|    +--- SubData3.dat
		|
		+--- Data.dat
		+--- Data2.dat
		+--- Data3.dat


上書き：

	出力先フォルダが存在しない場合は作成する。
	出力先フォルダが存在する場合、削除せずそのまま書き出す。

