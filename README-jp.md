# HoloPointCloud

### 概要
- HoloLens単体で点群データ(PointCloudData)を作成するテストプログラムです。

### 動作確認環境
- Unity Editor Version: 5.5.1f1
- Visual Studio 2015

### 動作に必要なUnity Asset
- <a href="https://github.com/Microsoft/HoloToolkit-Unity">HoloToolkit-Unity</a>
- <a href="https://www.assetstore.unity3d.com/#!/content/19811?aid=1100lGoW">Point Cloud Free Viewer</a><br />
  点群の描画でShaderだけ使っています、なのでエラーが出る時はShader以外のフォルダを削除してください。

### 使い方
1. スキャンしたい方向のSpacialMappingに中央カーソルを向けます。(gaze)
2. AirTap します。<br />
   手を使うと点群データに手の色が乗るので、クリッカー推奨です。
3. 数フレーム後にスキャンした点群データが表示されます。
4. スキャン後、以下の場所に点群データがoffファイル形式でセーブされます。<br />
   User Files \ LocalAppData \ HoloPointCloud_* \ LocalState \ *.off
5. offファイルは <a href="http://www.meshlab.net/">MeshLab</a> 等を使う事で、PC上で閲覧できます。

※10回スキャンしたあたりで動作が怪しくなり、15回すぎるとメモリ不足でフリーズします。
  （あくまで動作実験という事で）
