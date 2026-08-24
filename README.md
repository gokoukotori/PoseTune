# PoseTune

PoseTune は、VRChat アバター向けの **非破壊ポーズ authoring / compiler ツール**です。アバター上に PoseTune の authoring component を配置し、ビルド時に NDMF 経由で Animator Controller、Expression Parameters、Expression Menu、Modular Avatar component を生成します。

## 主な機能

- VRChat Avatar 用の身体ポーズを、アバター本体を直接破壊せずに構成
- Standing / Chair / Floor / Prone / Supine / Custom の PoseGroup 管理
- 手動モード、自動コンテキスト切替、初期ポーズ、優先度付き自動選択
- PoseGroup 共通の tracking 設定、FBT ガード、終了時 tracking reset
- Action / Base target layer への Animator 生成
- Pose menu、mode menu、height radial、Motion Time radial、option menu の自動生成
- VRChat Expression Menu の 8 controls 制限に合わせた自動ページ分割
- 高さ調整、接地補正、avatar scale / EyeHeight 系の補正用設定
- Pose preview、thumbnail / icon 生成、Quest / 低メモリモード
- KawaiiPosing からの互換移行、Gorone System EX との併用 guard
- PoseTunePreset / AvatarAdjustmentPreset による構成保存と再適用
- 検証レポート、safe auto-fix、Stable GUID 修復

## 要件

| 項目 | 要件 |
| --- | --- |
| Unity | 2022.3 以上 |
| VRChat SDK Avatars | `com.vrchat.avatars >= 3.10.3` |
| NDMF | `nadena.dev.ndmf >= 1.13.1` |
| Modular Avatar | `nadena.dev.modular-avatar >= 1.17.1` |
| Avatar | VRCAvatarDescriptor と Animator を持つ **Humanoid Avatar** |

PoseTune は VRChat の PC / Android 両方のビルド対象で使用することを想定しています。ただし、実機 runtime の挙動は VRChat SDK、アバター構成、Animator Controller、Expression Parameters、Quest 制約、FBT / tracking 状態に依存します。重要な構成はアップロード前後に PC / Android、特に Quest 環境で確認してください。

## インストール

### VPM / VCC で導入する場合

PoseTune は VPM repository [https://vpm.gokoukotori.com/](https://vpm.gokoukotori.com/) からインストールできます。

VPM package として取り込む場合は、VCC または互換 package manager にこの repository を追加し、`com.gokoukotori.posetune` を project に追加してください。依存 package は `package.json` の `vpmDependencies` に定義されています。

### ローカル package として導入する場合

1. このフォルダを Unity project の `Packages/com.gokoukotori.posetune` に配置します。
2. VRChat SDK Avatars、NDMF、Modular Avatar を project に追加します。
3. Unity を再読み込みし、compile error が出ていないことを確認します。

Unity Package Manager から導入する場合は、`Package Manager > + > Add package from disk...` でこの package の `package.json` を選択します。

## クイックスタート

1. Unity Hierarchy で `VRCAvatarDescriptor` を持つアバター、またはその配下の GameObject を選択します。
2. `GameObject > PoseTune > テンプレート` を実行します。
3. アバター配下に `PoseTune テンプレート` が作成されます。中には `PoseTuneRoot`、`PoseTuneAssistant`、`PoseMenu`、既定の PoseGroup、`PoseTrackingPolicy`、`PoseHeightAdjust` が入ります。
4. `PoseTuneRoot` の Inspector で **アシスタントを開く** を押します。
5. **ポーズ** タブで group を確認し、各 group の **ポーズ追加** から `PoseClip` を作成します。
6. 作成した `PoseClip` に `AnimationClip` または `sourceMotion` を設定し、表示名、初期ポーズ、loop、menu order、conditions などを調整します。tracking は所属する `PoseGroup` の `PoseTrackingPolicy` で編集します。
7. **メニュー**、**トラッキング**、**高さ**、**プレビュー** タブで生成内容を確認します。
8. **検証** タブで error / warning を確認し、必要に応じて safe fix を適用します。
9. 通常どおり VRChat SDK / NDMF の build / upload を実行します。PoseTune は build 時に必要な Modular Avatar component と Animator asset を生成します。

## 基本構成

### PoseTuneRoot

PoseTune 全体のルート component です。原則として 1 avatar につき 1 つだけ配置します。

主な設定:

- `displayName`: 表示名
- `parameterNamespace`: 生成 parameter の namespace。既定値は `PT`
- `targetLayer`: `Action` または `Base`
- `enableAutoContextSwitch`: 自動コンテキスト切替を生成
- `defaultMode`: 初期 mode。`Off` / `Auto` / `Manual`
- `poseSelectionSyncMode`: `DirectGroupParameter` または `CompressedPoseId` を選択します。`CompressedPoseId` では Pose ごとの `sourceSyncedParameterValue` を圧縮 ID として使用します。
- `poseWriteDefaultsMode`: 生成 Animator の Write Defaults 方針
- `enableHeightAdjust`: 高さ調整を有効化
- `enableIconGeneration`: thumbnail / icon 生成を有効化
- `questLowMemoryMode`: Quest / 低メモリ向け設定。高さ parameter などを local-only に寄せます。
- `disableWhenFullBodyTracking`: FBT 時に PoseTune を無効化

### PoseGroup

複数の `PoseClip` をまとめる group です。PoseTune は group ごとに menu と Animator layer を生成します。

主な設定:

- `kind`: `Standing` / `Chair` / `Floor` / `Prone` / `Supine` / `Custom`
- `displayName`: menu 表示名
- `parameterName`: 明示 parameter 名。通常は空欄にして PoseTune に生成させます。
- `exclusive`: 手動選択時に他の排他 group を解除
- `saved` / `synced`: group parameter の保存・同期
- `activationMode`: `Manual` / `Auto` / `ManualAndAuto`
- `autoPoseSelectionMode`: 自動時の pose 選択方式
- `autoContextProfile`: 標準または KawaiiPosing 近似
- `emitTrackingControl`: Group 全体の tracking control を生成
- `groupConditions`: group 全体に追加する条件
- `poseSpace`: Temporary Pose Space の制御

`PoseGroup` を入れ子にした場合、各 `PoseClip` は最も近い親 `PoseGroup` だけに所属します。子 group を無効化または `includeInBuild=false` にしても、その pose が親 group へ取り込まれることはありません。親 group に所属させたい pose は、子 group の外へ移動してください。

`activationMode=Auto` と `autoPoseSelectionMode=SelectedPosePerGroup` の組み合わせでは、menu を生成せずに pose 選択用 Int parameter だけを生成します。

`InitialPoseOnly` では、条件に一致する pose を `priority → isInitial → menuOrder → displayName → Stable GUID` の順で比較し、最初の pose を勝者にします。上位 pose が有効になった場合も、現在 pose の cleanup/handoff を通ってから切り替えます。

### PoseClip

実際のポーズを表す component です。`PoseGroup` の子に置きます。

主な設定:

- `clip`: ポーズに使う `AnimationClip`
- `sourceMotion`: 既存 Motion / BlendTree を元にする場合の入力
- `compatibilityProfile`: KawaiiPosing などの互換プロファイル
- `adjustmentClip`: アバター個別補正用 clip
- `adjustmentApplyMode`: 調整 clip の適用方法
- `customIcon`: menu icon
- `rootOffset`, `rootYawOffsetDegrees`, `humanoidOrientationOffsetYDegrees`: 姿勢補正
- `cameraOffset`: thumbnail 生成時の camera 補正。VRChat runtime の視点位置は変更しません。
- `isInitial`: group の初期 pose
- `loop`: PoseClip の loop 設定
- `explicitMenuValue`: 明示 menu value。基本は自動割り当て推奨です。
- `priority`: 自動選択と Animator transition 生成順
- `blendMode`: `Override` / `Additive`
- `motionTime`: state time / custom float / height parameter との連動
- `clipConditions`: pose 個別条件

### PoseMenu

Expression Menu 生成の設定です。

- `installMode`: root menu に追加、root へ直接展開、または menu 生成なし
- `rootMenuName`: 生成 menu 名
- `autoSplitMenu`: VRChat の 8 controls 制限に合わせて自動分割
- `generateIcons`: icon を menu に使用
- `useSubMenusPerGroup`: group ごとに submenu を作る
- `lyingMenuLayout`: Prone / Supine の menu 配置

### PoseTrackingPolicy

Root fallbackまたはGroup共通のeffective tracking policyを定義するcomponentです。優先順位は **Group component → Root component → group kind既定値** です。componentが存在すれば、値が既定値と同じでも明示overrideとして扱います。Base、Desktop lower-body、VR、FBT override、`generateResetOnExit` はGroup内の全Poseで共有され、同じ勝者componentから取得されます。

`PoseTrackingPolicy` は `PoseGroup` と同じGameObject、またはRoot-levelにだけ配置できます。`PoseClip`やその他の子GameObjectに有効なpolicyが残っている場合は`PT-T004 UnsupportedTrackingPolicyOwner`でbuildを停止します。自動移行は行わないため、必要な値をGroup policyへ手動で移し、unsupported ownerのpolicyを削除してください。

旧`PoseClip.tracking`／`PoseClip.emitTrackingControl`のinline YAML値は読み取り・自動移行されません。旧fieldは削除済みのため、Unityで再シリアライズすると値が失われる可能性があります。schema v1/v2 presetも移行元として使用できません。

- 頭、手、腰、足、指、目、口を `NoChange` / `Tracking` / `Animation` で指定
- FBT 用 override を別途指定可能
- `generateResetOnExit` により、PoseTune がその Group で明示変更した部位だけに終了時 reset request を生成

複数 group が同時に有効な場合、各部位を `Animation > Tracking > NoChange` の順で合成します。`NoChange` は現在値を維持する単位元であり、他 pose の明示 vote を消しません。頭・手・足の Lock は対象部位に対する最優先の `Animation` vote です。Lock を OFF にすると残っている pose vote を再適用し、残存 vote がなければその対象部位だけを `Tracking` へ戻します。

`generateResetOnExit=true` でも他の active pose が同じ部位を所有していれば、その勝者を再適用します。最後の owner が消えた部位だけが `Tracking` へ戻ります。`false` は sticky な値を維持する明示契約です。VRChat Tracking Control API は外部 Animator が以前保持していた値を取得できないため、PoseTune 外部の過去値を完全復元するものではありません。

`PoseGroup.emitTrackingControl=false` の場合はRoot fallbackやkind既定値も含めて無効化し、全項目を`NoChange`として扱い、reset／FBT overrideも生成しません。Pose OptionsのLockは独立したユーザー操作なので引き続き機能します。InspectorからGroup policyを追加すると、追加直前のbase／FBT／reset実効値を複製するため、追加操作だけでは生成結果や`emitTrackingControl`は変化しません。emission無効中に追加した場合は明示的な全`NoChange` policyを作成するため、後でemissionを有効化するときはそのGroup policyを編集または削除してください。

無効な`PoseTrackingPolicy`は実効値の解決、重複・unsupported検証、preset captureでは存在しないものとして扱います。InspectorでGroup policyを追加すると、同じGroup上の無効componentと保存されていた値を削除し、現在の実効値を持つ新しい有効componentへ置き換えます。`PoseTrackingPolicy`はRootまたはGroupごとに有効なものを1つだけ使用し、有効な重複があればvalidation errorになります。

tracking vote IDはPose数ではなく、Group内のdistinctな非`NoChange` policy profileごとに割り当てます。同じpolicyの複数PoseやBase／VRは同じIDを共有し、全`NoChange` profileにはIDを割り当てません。

### PoseHeightAdjust

高さ調整用の parameter、blend tree、radial menu を生成します。

- `applyMode`: `RootOrHipsYOffset` / `HumanoidLevelOffset` / `Disabled`
- `blendProfile`: `Standard` / `KawaiiPosing`
- `lowOffset` / `midOffset` / `highOffset`: height radial の対応値
- `autoCorrectionMode`: `RuntimeScaleFactor` / `RuntimeEyeHeightMeters` など
- `referenceEyeHeightMeters`, `maxAutoOffset`: avatar scale / EyeHeight 補正用設定
- `generateRadialMenu`: height radial menu を生成

### PoseCondition

GameObject 単位で条件をまとめる component です。条件は `And` / `Or` で合成できます。group conditions と pose conditions は Animator transition 条件に反映されます。
同じ GameObject に複数の `PoseCondition` component がある場合、それぞれは OR branch として扱われます。

使用できる比較は値型ごとに異なります。

| 値型 | 使用できる比較 |
| --- | --- |
| Bool | `If`, `IfNot` |
| Int | `Equals`, `NotEquals`, `Greater`, `Less`, `GreaterOrEqual`, `LessOrEqual` |
| Float | `Greater`, `Less`, `GreaterOrEqual`, `LessOrEqual` |

型に対応しない比較は build validation Error になります。既存の不正なserialized値は自動変換されないため、Inspectorで明示的に修正してください。

### PoseOption

PoseTune の option menu を生成するための component です。

- 頭をロック
- 手をロック
- 足をロック
- 移動ロック

### PoseTuneGoroneSystemExCompatibility

Gorone System EX との併用向け guard を生成する component です。EX marker と Int 型の `VRCSupine` parameter を検出し、`LowerBodyPoseGroups` または `AllPoseGroups` の対象に `VRCSupine == 0` 条件を追加します。通常版 Gorone / Supine は EX として扱わず警告し、`requireGoroneSystemEx` が有効なのに EX を検出できない場合は error になります。

## Assistant のタブ

| タブ | 用途 |
| --- | --- |
| ポーズ | group 追加、PoseClip 追加 |
| メニュー | PoseMenu 設定、生成予定 menu preview |
| トラッキング | FBT 設定、effective tracking policy 確認 |
| 高さ | HeightAdjust 設定、接地補正候補計算、調整 preset 保存 |
| プレビュー | Pose preview、thumbnail / icon 生成、preview reset |
| プリセット | PoseTunePreset / AvatarAdjustmentPreset の保存・適用 |
| 検証 | validation report、safe auto-fix、parameter clear |

## Build 時の動作

PoseTune は NDMF plugin として動作します。

1. **Resolving**: PoseTune authoring を収集し、validation を実行します。
2. **Generating**: parameter plan、Animator Controller、menu plan を作成し、Modular Avatar component を生成します。
3. **Transforming**: Modular Avatar 後の build output を検証します。
4. **Optimizing**: PoseTune の authoring component と不要な generated helper object を build avatar から削除します。

生成される主な内容:

- `ModularAvatarParameters`
- `ModularAvatarMenuInstaller` / `ModularAvatarMenuItem`
- target layer 用 `ModularAvatarMergeAnimator`
- 必要に応じた FX layer 用 `ModularAvatarMergeAnimator`
- build 中の `PoseTune Generated` hierarchy
- NDMF AssetSaver による generated Animator asset

通常、`PoseTune Generated` や authoring component は最終 build から削除されます。調査用に残したい場合は `PoseTuneRoot > 詳細設定 > 生成オブジェクトを Build に残す` を使用します。

## Parameter と Menu の考え方

既定 namespace は `PT` です。例:

- `PT/Mode`
- `PT/Height`
- `PT/SupineFlag`
- `PT/LockHead`
- `PT/LockHands`
- `PT/LockFeet`
- `PT/LocomotionLock`

group の manual control parameter は、`PoseGroup.parameterName` が空の場合に PoseTune が安定した名前を割り当てます。明示名を使う場合は VRChat built-in parameter、既存 Expression Parameters、他の PoseTune parameter と衝突しない名前にしてください。

Expression Parameters の同期 budget、parameter count、parameter type は VRChat の制限を受けます。PoseTune の validation は衝突や budget 超過を検出しますが、既存アバター側の controller や menu との相互作用は必ず実機で確認してください。

Tracking arbiter が使用する `PTI/TrackingVote/<groupGuid>` と部位別 reset request は Animator-only の内部 parameter です。Expression Parameters budget や公開 menu parameter には追加されません。tracking と Pose Options のどちらも使わない構成では、これらの parameter、arbiter layer、Tracking Control Behaviour は生成しません。

Expression Menu は VRChat の 1 menu あたり 8 controls 制限を前提にしています。`autoSplitMenu` が有効な場合、PoseTune は page submenu を自動生成します。

## Runtime / VRChat 制約

PoseTune は editor build 時に構成を生成しますが、最終挙動は VRChat runtime に依存します。特に次の項目は確認が必要です。

- **Humanoid Avatar**: Avatar の Animator に Humanoid avatar が設定されている必要があります。
- **PC / Android**: PC と Android では表現負荷、Animator 評価、Quest 用制限が異なります。
- **Quest**: Quest / 低メモリ運用では `questLowMemoryMode` を検討してください。thumbnail / icon 生成や同期 parameter の扱いも確認してください。
- **Expression Parameters**: synced parameter の byte budget、parameter count、型衝突を確認してください。
- **Animator Controller**: 既存 playable layer、Write Defaults、Action layer weight、FX layer との干渉を確認してください。
- **Temporary Pose Space**: `PoseSpacePolicy` は VR / Desktop、delay、pose space entry / exit の runtime 体験に影響します。
- **avatar scale**: `ScaleFactor`、`EyeHeightAsMeters`、`EyeHeightAsPercent` を利用する補正は avatar scale と runtime tracking 状態に依存します。
- **FBT**: `disableWhenFullBodyTracking`、FBT override、TrackingType 条件の組み合わせは、3点 / 4点以上の環境で確認してください。

## Preview と icon 生成

Assistant の **プレビュー** タブから pose preview と icon 生成を実行できます。

- `PoseTuneRoot.enableIconGeneration` が ON の場合に icon を生成できます。
- `PoseMenu.generateIcons` が OFF の場合、menu には icon を使用しません。
- `PoseClip.cameraOffset` は thumbnail framing 用です。VRChat runtime の視点や camera は変更しません。
- 生成 icon は `Assets/PoseTuneGenerated/<AvatarName>/<RootGuid>/Icons` 配下に保存されます。

## KawaiiPosing からの移行

KawaiiPosing / PosingSystem が入った avatar を選択し、`GameObject > PoseTune > KawaiiPosing から移行` を実行します。移行 window では dry-run、既存 root への merge、新規 PoseTuneRoot 作成、custom icon 保持、FootHeight / BlendTree / MotionTime / PoseSpace 互換などを選択できます。

`OverrideDefines`を取り込む場合も同じBlendTree／回転／Root再中心化／adjustment／MotionTime／PoseSpace設定を適用します。`ImportAllAsCustomDisabled`はCustom groupとposeの両方をbuild対象外で作成します。

移行元の `mergeTrackingControl` は「全 controller の Tracking Control を統合する」設定であり、PoseTune の `emitTrackingControl` とは同じ意味ではありません。そのため、この値を `emitTrackingControl` へ直結しません。`addTrackingPolicy` を選択した場合だけ group kind に基づく近似 policy を追加します。元 controller 全体の統合結果や Override policy は厳密には復元できないため、専用 diagnostic を確認して部位別設定を調整してください。

移行元は既定では変更しません。移行元GameObjectを`EditorOnly`または非アクティブにする場合、Avatar Root全体には適用できません。共有GameObjectへ適用する場合は、影響するcomponentと子階層を確認してから明示的に許可してください。移行元を保持すると旧・新両方のauthoring systemがbuildへ作用し得ます。

`BakeAtMigration`で生成したClip／BlendTreeは `Assets/PoseTuneGenerated/KawaiiMigration/<Avatar>/<RootGuid>/Motions` 配下へ永続化されます。既存assetは上書きせず、移行失敗時はその実行で新規作成したassetだけをrollbackします。

成功した移行は同じRoot配下の`Reports`にmanifestを保存します。manifestには作成asset path、移行option、移行元GameObjectの変更前tag／active状態が記録されます。SceneをUndoしても生成assetとmanifestは残るため、Redo後のMotion参照を維持できます。

移行後は validation を実行し、生成された group、pose、height、tracking、menu を確認してください。

## Preset

### PoseTunePreset

`PoseTunePreset` schema v3 は group、pose、menu、height に加えて、Root／Groupの有効なpolicy componentの有無、全10部位、FBT override、`generateResetOnExit`を保存します。無効なpolicyはcaptureせず、Pose単位のtracking fieldも保存しません。Group／PoseのStable GUIDは空でない正しいGUID形式が必須で、照合にもGUIDだけを使用します。Assistantの**プリセット**タブから現在の構成を保存し、別avatarに`Merge`または`Replace`で適用できます。

schema v1/v2 presetは自動移行せず、適用前に原子的に拒否します。`Merge`はpresetに有効なpolicyデータがあるRoot／Groupだけを追加・更新し、`present=false`なら適用先を変更しません。`Replace`の`present=false`は無効なものも含めて対象ownerの全policyを削除します。`present=true`の適用先に無効なpolicyしかない場合は、そのcomponentを再有効化せず削除して新しい有効なpolicyを作成します。プリセット外の`PoseGroup`／`PoseClip` componentを削除した後、同じGameObject上のownerがなくなる`PoseCondition`／`PoseTrackingPolicy`も除去しますが、それ以外の兄弟component、GameObject、子階層は保持します。適用全体は1回のUndo/Redoで扱います。

### AvatarAdjustmentPreset

`AvatarAdjustmentPreset` は avatar 個別の root offset、camera offset、adjustment clip などを保存します。Stable GUID を使って pose と対応付けるため、PoseTuneRoot / PoseGroup / PoseClip の Stable GUID を不用意に再生成しないでください。

## Validation と auto-fix

Assistant の **検証** タブ、または build 時の NDMF validation で、次のような問題を検出します。

- PoseTuneRoot が VRCAvatarDescriptor 配下にない
- avatar に Animator / Humanoid avatar がない
- avatar 配下に複数の PoseTuneRoot がある
- 空の PoseGroup、または有効な PoseClip がない group
- PoseClip に AnimationClip / sourceMotion がない
- zero length clip、loop 設定不一致、root transform curve、unsupported curve
- parameter 名の空欄、予約名、型衝突、Expression Parameters budget 超過
- menu control overflow
- FBT、Gorone System EX、Kawaii 互換に関する警告
- 有効な`PoseTrackingPolicy`の重複と、Root／Group以外にあるunsupported owner
- generated output の欠落や古い graph hash

一部の issue には auto-fix が用意されています。`安全な修正を一括適用` は safe / reversible な修正だけを適用します。Asset 書き込みを伴う fix は明示表示を ON にしてから個別に適用してください。

Stable GUID が壊れた場合は `Tools > PoseTune > Stable GUIDを修復` を実行できます。

## 開発者向け構成

```text
Runtime/
  Components/      Authoring component
  Data/            Enum、条件、tracking、height、settings
  Presets/         PoseTunePreset / AvatarAdjustmentPreset
Editor/
  Build/           NDMF plugin、build cleanup、post validation
  Compiler/        Graph collection、parameter/menu/animator compiler、validation
  GUI/             Inspector、Assistant、preview UI
  Migration/       KawaiiPosing migration
  Preview/         Pose preview、thumbnail generation
Tests/Editor/      EditMode regression tests
```

## よくあるトラブル

### PoseTuneRoot が見つからない、または build されない

`PoseTuneRoot` が `VRCAvatarDescriptor` の配下にあるか確認してください。1 avatar 配下に複数の `PoseTuneRoot` を置かないでください。

### メニューに pose が出ない

`PoseGroup.includeInBuild` と `PoseClip.includeInBuild` が ON か確認してください。`PoseGroup.activationMode` が `Auto` のみの場合、manual menu control は生成されません。

### parameter conflict が出る

`PoseGroup.parameterName`、Motion Time の custom parameter、既存 Expression Parameters を確認してください。通常は group parameter を空欄にして PoseTune の自動生成名を使う方が安全です。

### icon が出ない

`PoseTuneRoot.enableIconGeneration`、`PoseMenu.generateIcons`、`PoseClip.suppressIconGeneration`、`PoseGroup.suppressIconGeneration` を確認してください。Quest / 低メモリモードでは icon 生成ボタンが無効化されます。

### FBT で意図せず pose が入る、または入らない

`disableWhenFullBodyTracking`、`allowFullBodyTracking`、Root／Groupのtracking policy、`generateResetOnExit`、FBT overrideを確認してください。TrackingType条件が必要な構成ではvalidation warningを確認してください。

### Root transform curve の warning が出る

PoseClip の root transform curve は意図しない移動や回転を起こす場合があります。必要な補正は `rootOffset`、`rootYawOffsetDegrees`、`humanoidOrientationOffsetYDegrees`、adjustment clip へ移すことを検討してください。

## ライセンス

PoseTune は `AGPL-3.0-only` で配布されています。Unity、VRChat SDK、NDMF、Modular Avatar と連携する Unity Editor package として compile / link / reference / load / run / distribute できる追加許可が `LICENSE.md` に含まれています。詳細は [LICENSE.md](LICENSE.md) を確認してください。
