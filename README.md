# PoseTune

PoseTune は、VRChat アバター向けの非破壊ポーズ authoring / compiler ツールです。アバター上に authoring component を配置し、NDMF build 時に Animator Controller、Expression Parameters、Expression Menu、Modular Avatar component を生成します。

## 主な機能

- Standing / Chair / Floor / Prone / Supine / Custom の PoseGroup 管理
- 手動モード、自動コンテキスト切替、初期ポーズ、優先度付き自動選択
- PoseGroup 共通の tracking policy、FBT guard、終了時 tracking reset
- Action / Base target layer への Animator 生成
- Pose menu、height radial、Motion Time radial、option menu の自動生成
- VRChat Expression Menu の 8 controls 制限に合わせた自動ページ分割
- 高さ調整、接地補正、avatar scale / EyeHeight 補正
- Pose preview、thumbnail / icon 生成、Quest / 低メモリモード
- KawaiiPosing からの移行、Gorone System EX との併用 guard
- build 前後の validation と safe auto-fix

## 要件

| 項目 | 要件 |
| --- | --- |
| Unity | 2022.3 以上 |
| VRChat SDK Avatars | `com.vrchat.avatars >= 3.10.3` |
| NDMF | `nadena.dev.ndmf >= 1.13.1` |
| Modular Avatar | `nadena.dev.modular-avatar >= 1.17.1` |
| Avatar | `VRCAvatarDescriptor` と Animator を持つ Humanoid Avatar |

PoseTune は VRChat の PC / Android 両方を対象にしています。最終挙動は VRChat SDK、Animator、Expression Parameters、Quest 制約、FBT / tracking 状態に依存するため、重要な構成はアップロード前後に実機で確認してください。

## インストール

VPM repository [https://vpm.gokoukotori.com/](https://vpm.gokoukotori.com/) を VCC または互換 package manager に追加し、`com.gokoukotori.posetune` を project に追加してください。依存 package は `package.json` の `vpmDependencies` に定義されています。

ローカル package として使う場合は、このフォルダを Unity project の `Packages/com.gokoukotori.posetune` に配置します。Unity Package Manager の `Add package from disk...` から `package.json` を選択することもできます。

## クイックスタート

1. `VRCAvatarDescriptor` を持つアバター、またはその配下を選択します。
2. `GameObject > PoseTune > テンプレート` を実行します。
3. `PoseTuneRoot` の Inspector から Assistant を開きます。
4. **ポーズ**タブで `PoseGroup` と `PoseClip` を追加し、`AnimationClip` または `sourceMotion` を設定します。
5. **メニュー**、**トラッキング**、**高さ**、**プレビュー**を調整します。
6. **検証**タブで error / warning を確認します。
7. VRChat SDK / NDMF の通常の build / upload を実行します。

## Authoring component

### PoseTuneRoot

PoseTune 全体のルートです。原則として 1 avatar につき 1 つ配置します。

- `parameterNamespace`: 生成 parameter の namespace。既定値は `PT`
- `targetLayer`: `Action` または `Base`
- `enableAutoContextSwitch`: 自動コンテキスト切替
- `defaultMode`: `Off` / `Auto` / `Manual`
- `poseSelectionSyncMode`: `DirectGroupParameter` / `CompressedPoseId`
- `poseWriteDefaultsMode`: 生成 Animator の Write Defaults 方針
- `enableHeightAdjust`: 高さ調整
- `enableIconGeneration`: thumbnail / icon 生成
- `questLowMemoryMode`: Quest / 低メモリ向け設定
- `disableWhenFullBodyTracking`: FBT 時に PoseTune を無効化

### PoseGroup

複数の `PoseClip` をまとめ、group ごとの menu と Animator layer を生成します。

- `kind`: `Standing` / `Chair` / `Floor` / `Prone` / `Supine` / `Custom`
- `parameterName`: 明示 parameter 名。通常は空欄で自動生成します。
- `exclusive`: 手動選択時に他の排他 group を解除
- `saved` / `synced`: group parameter の保存・同期
- `activationMode`: `Manual` / `Auto` / `ManualAndAuto`
- `autoPoseSelectionMode`: 自動時の pose 選択方式
- `emitTrackingControl`: group 全体の tracking control
- `groupConditions`: group 共通条件
- `poseSpace`: Temporary Pose Space の制御

入れ子の `PoseGroup` では、各 `PoseClip` は最も近い親 group だけに所属します。`InitialPoseOnly` の勝者は `priority → isInitial → menuOrder → displayName → 構造ID` の順で決定します。

### PoseClip

実際のポーズです。`PoseGroup` の子に配置します。

- `clip` / `sourceMotion`: 入力 Motion
- `compatibilityProfile`: KawaiiPosing などの互換プロファイル
- `adjustmentClip` / `adjustmentApplyMode`: アバター個別補正
- `customIcon`: menu icon
- `rootOffset`, `rootYawOffsetDegrees`, `humanoidOrientationOffsetYDegrees`: 姿勢補正
- `cameraOffset`: thumbnail framing 用補正
- `isInitial`, `loop`, `explicitMenuValue`, `priority`, `blendMode`
- `motionTime`: state time / custom float / height parameter 連動
- `clipConditions`: pose 個別条件

### PoseMenu

Expression Menu の install 位置、表示名、自動ページ分割、group submenu、icon 利用、Prone / Supine 配置を設定します。

### PoseTrackingPolicy

Root fallback または Group 共通の tracking policy を定義します。解決順は **Group component → Root component → group kind 既定値**です。`PoseTrackingPolicy` は `PoseTuneRoot` と同じ GameObject、または `PoseGroup` と同じ GameObject にだけ配置できます。

Base、Desktop lower-body、VR、FBT override、`generateResetOnExit` は Group 内の全 Pose で共有されます。複数 group が同時に有効な場合は、各部位を `Animation > Tracking > NoChange` の順で合成します。`PoseGroup.emitTrackingControl=false` の group は tracking vote と reset を生成しません。

### PoseHeightAdjust / PoseCondition / PoseOption

- `PoseHeightAdjust`: 高さ parameter、blend tree、radial menu、scale / EyeHeight 補正を生成します。
- `PoseCondition`: Bool / Int / Float parameter 条件を GameObject 単位で定義します。同一 GameObject の複数 component は OR branch です。
- `PoseOption`: 頭・手・足・移動の lock option menu を生成します。

### PoseTuneGoroneSystemExCompatibility

Gorone System EX marker と Int 型の `VRCSupine` parameter を検出し、対象 PoseGroup に guard 条件を追加します。

## Assistant

| タブ | 用途 |
| --- | --- |
| ポーズ | group / PoseClip の追加と編集 |
| メニュー | PoseMenu 設定と生成予定 menu preview |
| トラッキング | FBT 設定と effective tracking policy |
| 高さ | HeightAdjust 設定、接地補正候補計算、Pose への適用 |
| プレビュー | Pose preview、thumbnail / icon 生成、preview reset |
| 検証 | validation report、safe auto-fix、parameter clear |

## 識別子と生成Asset

Editorで永続化するthumbnail、icon、Kawaii migration assetは、保存済みSceneまたはPrefab上のComponentの`GlobalObjectId`を使用します。path用の識別子は`GlobalObjectId.ToString()`をSHA-256でhash化した先頭16桁です。Hierarchy上でGameObjectを複製した場合や、同じGameObjectに同型Componentが複数ある場合もComponentごとに異なる識別子になります。

NDMF build cloneでは`GlobalObjectId`を使わず、Component型、Avatar相対のsibling-index path、同じGameObject上の同型Component indexから決定的な構造IDを生成します。Root / Group / Pose、Animator名、内部parameter、graph hash、generated markerの照合はこの構造IDを共有します。

未保存Sceneでは永続IDを確定できないため、thumbnail生成とKawaii migrationはAssetやauthoring構成を変更する前に停止します。通常のInspector編集とNDMF buildは構造IDで継続できます。Scene内の移動やPrefab構造の変更により`GlobalObjectId`や構造IDが変わる場合があります。

生成iconは次の場所に保存されます。

```text
Assets/PoseTuneGenerated/<AvatarName>/<RootGlobalObjectIdHash>/Icons
```

既存の生成AssetをPoseTuneが自動削除または変換することはありません。

## Build時の動作

1. **Resolving**: authoringを収集し、validationを実行します。
2. **Generating**: parameter plan、Animator Controller、menu plan、Modular Avatar componentを生成します。
3. **Transforming**: Modular Avatar後のbuild outputを検証します。
4. **Optimizing**: authoring componentと不要なgenerated helper objectをbuild avatarから削除します。

通常、`PoseTune Generated`とauthoring componentは最終buildから削除されます。調査用に残す場合は`PoseTuneRoot > 詳細設定 > 生成オブジェクトを Build に残す`を使用します。

## KawaiiPosingからの移行

保存済みSceneまたはPrefab上のAvatarを選択し、`GameObject > PoseTune > KawaiiPosing から移行`を実行します。dry-run、既存Rootへのmerge、新規Root作成、custom icon保持、FootHeight / BlendTree / MotionTime / PoseSpace互換を選択できます。

`BakeAtMigration`の生成Motionとmanifestはrun単位で次の場所へ保存されます。既存Assetは上書きせず、失敗時はそのrunで新規作成したAssetだけをrollbackします。

```text
Assets/PoseTuneGenerated/KawaiiMigration/<AvatarName>_<AvatarGlobalObjectIdHash>/<RunId>/
```

manifestにはAvatar、Root、Pose、移行元Componentの`GlobalObjectId`、作成Asset path、移行option、移行元GameObjectの変更前状態を記録します。移行後はvalidationを実行し、生成されたgroup、pose、height、tracking、menuを確認してください。

## Validation

Assistantの**検証**タブとNDMF buildで、Root配置、Humanoid Animator、空group、Motion欠落、curve、parameter衝突・budget、menu上限、tracking policy、FBT、Gorone System EX、Kawaii互換、thumbnail、generated outputを検証します。

一部のissueにはsafe auto-fixがあります。Asset書き込みを伴うfixは明示表示をONにしてから個別に適用してください。

## 開発者向け構成

```text
Runtime/
  Components/      Authoring component
  Data/            Enum、条件、tracking、height、settings
Editor/
  Build/           NDMF plugin、build cleanup、post validation
  Compiler/        Graph、parameter、menu、animator、validation
  GUI/             Inspector、Assistant、preview UI
  Migration/       KawaiiPosing migration
  Preview/         Pose preview、thumbnail generation
Tests/Editor/      EditMode regression tests
```

## よくあるトラブル

- buildされない: `PoseTuneRoot`が`VRCAvatarDescriptor`配下にあり、Avatar内に1つだけ存在することを確認します。
- menuにPoseが出ない: `includeInBuild`、`activationMode`、Motion設定を確認します。
- parameter conflict: 明示parameter名、Motion Time parameter、既存Expression Parametersを確認します。
- iconが出ない: Scene / Prefabを保存し、`enableIconGeneration`、`generateIcons`、suppress設定、低メモリモードを確認します。
- FBTで意図しない動作になる: `disableWhenFullBodyTracking`、Root / Group policy、FBT override、TrackingType条件を確認します。

## ライセンス

PoseTuneは`AGPL-3.0-only`で配布されています。Unity、VRChat SDK、NDMF、Modular Avatarと連携するUnity Editor packageとしてcompile / link / reference / load / run / distributeできる追加許可が[LICENSE.md](LICENSE.md)に含まれています。
