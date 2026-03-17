# CameraSongScript

Beat SaberのカメラMod（[Camera2](https://github.com/kinsi55/CS_BeatSaber_Camera2) / [CameraPlus](https://github.com/Snow1226/CameraPlus)）に **曲別カメラスクリプト（SongScript）** 機能を追加・拡張するプラグインです。

## 特徴

* **Camera2 / CameraPlus 両対応** - どちらのカメラModでも動作します。インストールされているカメラModを自動検出します
* **曲別スクリプト自動検出** - 譜面フォルダ内のJSONファイルや、`UserData\CameraSongScript\SongScripts`フォルダに配置されたスクリプトを自動的にスキャンして利用します
* **BeatSaver ID(bsr)連携** - SongDetailsCacheを利用してBeatSaverのID(bsr)から`SongScripts`フォルダのスクリプトを自動マッチングします
* **CameraPlus互換スクリプト形式** - Camera2でもCameraPlusのMovementScript形式のJSONをそのまま使用できます
* **汎用スクリプト（CommonScript）** - SongScriptが無い曲ときに代わりに使える汎用カメラスクリプト機能
* **複数スクリプト選択可能** - `SongScript.json`以外の任意の名前の.jsonファイルが使用可能で、複数あっても選択できます。
* **高さオフセット** - スクリプトごとにカメラのY座標オフセットを調整・保存可能
* **ステータスパネル** - メニュー画面の3D空間にSongScriptの状態をリアルタイム表示（12箇所のプリセット位置）
* **メタデータ表示** - スクリプトに含まれるカメラスクリプト作者名、曲名、マッパー名、アバター身長、説明文を表示
* **ZIP対応** - `UserData/CameraSongScript/SongScripts`フォルダについてはZIPファイル内のスクリプトも読み込み可能
* **日本語/英語対応** - UIのホバーヒントは日本語・英語を切り替え可能（ゲーム言語自動検出に対応）

## インストール方法

1. [リリースページ](https://github.com/rynan4818/CameraSongScript/releases)から最新のCameraSongScriptのリリースをダウンロードします
2. ダウンロードしたzipファイルを`Beat Saber`フォルダに解凍して下さい
   * `Plugins`フォルダに`CameraSongScript.dll`を配置します
   * `Libs`フォルダに`CameraSongScript.Cam2.dll`と`CameraSongScript.CamPlus.dll`を配置します

## 使い方

### カメラスクリプトの配置

カメラスクリプトは以下の2つの方法で配置できます。

#### 方法1: 譜面フォルダに配置

譜面の`CustomLevels`や`CustomWIPLevels`フォルダ内にカメラスクリプトのJSONファイル（例: `SongScript.json`）を直接配置します。

```
Beat Saber/Beat Saber_Data/CustomLevels/[譜面フォルダ]/SongScript.json
```

#### 方法2: SongScriptsフォルダに配置

`UserData/CameraSongScript/SongScripts/`フォルダにスクリプトを配置します。スクリプトのJSON内の`metadata.mapId`または`metadata.hash`が一致する曲で自動的に使用されます。加えて、パス由来の`mapId`候補として、1. スクリプトJSONファイル名先頭の1〜6文字の16進数、2. スクリプトJSONの親フォルダ名先頭の1〜6文字の16進数（ZIP内フォルダも含む）、3. ZIPファイルを置いた親フォルダ名先頭の1〜6文字の16進数、の順で最初に見つかったものも`mapId`として扱います。`mapId`はBeatSaverのID(bsr)、`hash`は譜面ハッシュです。英字の大文字小文字は区別しません。サブフォルダでの整理やZIPファイルにも対応しています。

```
Beat Saber/UserData/CameraSongScript/SongScripts/[任意のフォルダ構成]/スクリプト.json
Beat Saber/UserData/CameraSongScript/SongScripts/スクリプト.zip
```

### 基本的な流れ

1. 曲選択画面の左側タブに「CameraSongScript」タブが追加されます
2. 曲を選択すると、対応するカメラスクリプトが自動的にスキャンされます
3. スクリプトが見つかった場合はステータスに表示され、プレイ開始時に自動的にカメラが制御されます

### 汎用スクリプト（CommonScript）

特定の曲に紐づかない汎用的なカメラスクリプトを`UserData/CameraSongScript/CommonScripts/`フォルダに配置すると、SongScriptが無い曲で代替として、または全曲で強制的に使用できます。

```
Beat Saber/UserData/CameraSongScript/CommonScripts/スクリプト.json
```

## 設定の詳細

### ゲームプレイ設定タブ（CameraSongScript タブ）

曲選択画面の左側パネルに表示される設定タブです。

#### 基本設定

* **Camera Mod** - 検出されたカメラModを表示します（Camera2 / CameraPlus / None）。緑色で表示されていれば正常に検出されています
* **Enabled** - SongScript機能の有効/無効を切り替えます。OFFにすると曲専用カメラスクリプト(SongSpecificScript)は実行されません。CameraPlusで設定された汎用スクリプトは実行されます。
* **ステータス表示** - 現在の曲に対するスクリプトの検出状況を表示します
  * `X script(s) found - [スクリプト名]` - スクリプトが見つかった場合（緑色）
  * `Common Script: [スクリプト名]` - 汎用スクリプトが使用される場合（橙色）
  * `No camera scripts` - スクリプトが見つからない場合（灰色）

#### スクリプト選択

* **Script File** - 選択中の曲で利用可能なカメラスクリプトをドロップダウンから選択します。複数のスクリプトが見つかった場合に切り替えられます。選択は曲・難易度ごとに保存されます

#### メタデータパネル

選択中のスクリプトにメタデータが含まれている場合に表示されます。

* **Camera** - カメラスクリプト作者名
* **Song / Mapper** - 曲名とマッパー名
* **Avatar Height** - スクリプト作成時のアバター身長（cm）
* **説明文** - スクリプトの説明テキスト

#### 高さオフセット

* **Height Y Offset (cm)** - カメラのY座標（高さ）をスクリプト基準からオフセットします（-200cm〜+200cm）。アバターの身長差を補正するために使用します。`Per-Script Height Offset`設定がONの場合、スクリプトごとに個別に保存されます
* **Reset Offset** - オフセット値を0にリセットします

#### Camera2専用設定

Camera2が検出された場合のみ表示されます。

* **Use Audio Sync** - ONの場合、曲のタイムラインに同期してスクリプトを進行します。OFFの場合、リアルタイム（DateTime基準）で進行します。通常はONのままで使用してください
* **Target Camera** - スクリプトを適用するカメラを選択します
  * `(All)` - Camera2のすべてのアクティブカメラに適用（デフォルト）
  * カメラ名を選択すると、そのカメラのみに適用されます
* **Custom Scene** - SongScript検出時に自動的に切り替えるCamera2のカスタムシーンを選択します
  * `(Default)` - カスタムシーンの切り替えを行わない（デフォルト）
  * シーン名を選択すると、プレイ開始時にそのカスタムシーンに切り替わり、プレイ終了時に元に戻ります
* **Add 'CameraSongScript' Custom Scene** - 上の「Target Camera」で選択中のカメラを「CameraSongScript」という名前のカスタムシーンとしてCamera2に登録・更新します。スクリプト再生用のカメラだけを有効にしたカスタムシーンを簡単に作成できます

#### CameraPlus専用設定

CameraPlusが検出された場合のみ表示されます。

* **SongScript Profile** - SongScript検出時に切り替えるCameraPlusのプロファイルを選択します
  * `(NoChange)` - プロファイルを変更しない（デフォルト）
  * `(Delete)` - プロファイル設定を空にする（曲固有プロファイルをクリア）
  * プロファイル名を選択すると、そのプロファイルに切り替えます

#### Common Script（汎用スクリプト）設定

* **Fallback to Common** - ONにすると、SongScriptが見つからない曲で汎用スクリプトを自動的に使用します
* **Force Common Script** - ONにすると、SongScriptの有無やEnabled設定に関係なく常に汎用スクリプトを強制使用します
* **Common Script** - 使用する汎用スクリプトを選択します
  * `(Random)` - プレイごとにランダムに選択（デフォルト）
  * ファイル名を選択すると、常にそのスクリプトを使用します

**Camera2使用時の追加設定:**

* **CS Target Camera** - 汎用スクリプト使用時のターゲットカメラ
  * `(Same as SongScript)` - 通常のSongScript設定と同じカメラを使用（デフォルト）
* **CS Custom Scene** - 汎用スクリプト使用時のカスタムシーン
  * `(Same as SongScript)` - 通常のSongScript設定と同じシーンを使用（デフォルト）

**CameraPlus使用時の追加設定:**

* **CS Profile** - 汎用スクリプト使用時のプロファイル
  * `(Same as SongScript)` - 通常のSongScript設定と同じプロファイルを使用（デフォルト）

#### Status Panel（ステータスパネル）設定

* **Show Status Panel** - メニュー画面の3D空間にステータスインジケータパネルを表示します。タブを開かなくてもSongScriptの状態が確認できます
* **Panel Position** - ステータスパネルの表示位置をプリセットから選択します。12箇所のプリセット位置があります
  * Left系: 左側の壁付近（UpperRight, UpperLeft, LowerRight, LowerLeft）
  * Center系: 正面パネル付近（UpperRight, UpperLeft, LowerRight, LowerLeft）
  * Right系: 右側の壁付近（UpperRight, UpperLeft, LowerRight, LowerLeft）

### Mod設定メニュー（BSML Settings）

BSMLの設定メニュー（左メニューの歯車アイコン）内に表示される設定です。

* **Per-Script Height Offset** - ONの場合、高さオフセットをスクリプトごとに個別保存します（スクリプトファイルのSHA1ハッシュをキーとして保存）。OFFの場合、1つの共通設定として使用します
* **Hover-Hint Language** - UIのホバーヒント（マウスオーバー時の説明テキスト）の表示言語を選択します
  * `Auto` - ゲーム本体の言語設定に自動追従（デフォルト）
  * `English` - 英語固定
  * `Japanese` - 日本語固定
* **Show Hover-Hints** - ホバーヒントの表示/非表示を切り替えます

## UIに無い設定

設定ファイル `UserData/CameraSongScript.json` を直接編集することで変更できる設定です。

### ステータスパネルのビジュアル設定

| 項目 | デフォルト値 | 説明 |
|------|------------|------|
| `StatusFontSize` | 3.5 | ステータスパネルのフォントサイズ |
| `StatusCanvasWidth` | 100 | パネルのキャンバス幅 |
| `StatusCanvasHeight` | 10 | パネルのキャンバス高さ |
| `StatusScale` | 0.025 | パネルのワールド空間でのスケール |

### ステータスパネルのプリセット位置

12箇所のプリセット位置はそれぞれ位置（PosX, PosY, PosZ）と回転（RotX, RotY, RotZ）をJSON設定ファイルで調整できます。

設定名は `Preset[エリア][位置]Pos[軸]` / `Preset[エリア][位置]Rot[軸]` の形式です。

例:
* `PresetLeftUpperRightPosX`, `PresetLeftUpperRightPosY`, `PresetLeftUpperRightPosZ` - Left/UpperRight位置の座標
* `PresetLeftUpperRightRotX`, `PresetLeftUpperRightRotY`, `PresetLeftUpperRightRotZ` - Left/UpperRight位置の回転

エリア: `Left`, `Center`, `Right`
位置: `UpperRight`, `UpperLeft`, `LowerRight`, `LowerLeft`

## データファイル

| パス | 説明 |
|------|------|
| `UserData/CameraSongScript.json` | メインの設定ファイル（BSIPA自動管理） |
| `UserData/CameraSongScript/CameraSongScript_SongSettings.json` | 曲ごとのスクリプト選択とスクリプトごとの高さオフセット保存ファイル |
| `UserData/CameraSongScript/SongScripts/` | 曲別カメラスクリプト配置フォルダ（metadata.mapId / metadata.hash / ファイル名・フォルダ名由来のmapIdでマッチング） |
| `UserData/CameraSongScript/CommonScripts/` | 汎用カメラスクリプト配置フォルダ |

## カメラスクリプト形式

CameraPlusのMovementScript形式に準拠したJSONファイルを使用します。

### 対応機能

| 機能 | Camera2 | CameraPlus |
|------|---------|------------|
| Position / Rotation 補間 | 対応 | CameraPlus側で対応 |
| FOV アニメーション | 対応 | CameraPlus側で対応 |
| EaseTransition（イージング） | 対応 | CameraPlus側で対応 |
| TurnToHead（頭追従） | 対応 | CameraPlus側で対応 |
| TurnToHeadHorizontal（水平のみ） | 対応 | CameraPlus側で対応 |
| VisibleObject制御 | 対応 | CameraPlus側で対応 |
| Duration / Delay | 対応 | CameraPlus側で対応 |
| ループ再生 | 対応 | CameraPlus側で対応 |
| ActiveInPauseMenu | 対応 | CameraPlus側で対応 |
| metadata | 対応 | 対応 |
| CameraEffect（DoF, Wipe等） | 非対応 | CameraPlus側で対応 |
| WindowControl | 非対応 | CameraPlus側で対応 |

### スクリプトJSONの例

```json
{
  "ActiveInPauseMenu": "true",
  "Movements": [
    {
      "StartPos": { "x": "0", "y": "1.5", "z": "-3", "FOV": "90" },
      "StartRot": { "x": "15", "y": "0", "z": "0" },
      "EndPos": { "x": "2", "y": "2", "z": "-2", "FOV": "60" },
      "EndRot": { "x": "10", "y": "30", "z": "0" },
      "Duration": "5.0",
      "Delay": "1.0",
      "EaseTransition": "true",
      "TurnToHead": "false"
    }
  ],
  "metadata": {
    "cameraScriptAuthorName": "作者名",
    "songName": "曲名",
    "levelAuthorName": "マッパー名",
    "mapId": "1234a",
    "hash": "0123456789abcdef0123456789abcdef01234567",
    "avatarHeight": 170.0,
    "description": "スクリプトの説明"
  }
}
```

### metadataフィールド

| フィールド | 説明 |
|-----------|------|
| `cameraScriptAuthorName` | カメラスクリプト作者名 |
| `songName` | 曲名 |
| `songSubName` | 曲サブ名 |
| `songAuthorName` | 曲アーティスト名 |
| `levelAuthorName` | マッパー名 |
| `mapId` | BeatSaverのマップID（SongScriptsフォルダでのマッチングに使用。大文字小文字は無視。加えてファイル名先頭、親フォルダ名先頭、ZIP配置親フォルダ名先頭の16進数1〜6文字も優先順位付きの代替mapIdとして使用可能） |
| `hash` | 譜面ハッシュ（SongScriptsフォルダでのマッチングに使用。大文字小文字は無視） |
| `bpm` | BPM |
| `duration` | 曲の長さ |
| `avatarHeight` | スクリプト作成時のアバター身長（cm） |
| `description` | スクリプトの説明文 |

## Camera2とCameraPlusの動作の違い

### Camera2モード

CameraSongScriptがCamera2のSDKを通じて直接カメラのPosition/Rotation/FOVを毎フレーム制御します。VisibleObject制御もCamera2のOverrideTokenを通じて行われます。

### CameraPlusモード

CameraPlusが元々持っているMovementScript実行機能を利用します。CameraSongScriptはパッチを使ってCameraPlusにスクリプトパスを渡し、実際のカメラ制御はCameraPlus側が行います。

## ライセンス

このプラグインはMITライセンスで公開されています。

カメラ移動部分のスクリプト形式は、すのーさんの[CameraPlus](https://github.com/Snow1226/CameraPlus)に準拠しています。

- CameraPlusの著作権表記・ライセンスは以下の通りです。
  - https://github.com/Snow1226/CameraPlus/blob/master/LICENSE
