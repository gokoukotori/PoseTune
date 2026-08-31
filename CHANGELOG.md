# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [0.4.0] - 2026-08-31

### Added

- `PoseSelectionSyncMode.SharedExclusivePoseId`を追加し、同期される手動操作可能なexclusive groupをSaved属性ごとの共有`Int`（既定namespaceでは`PT/PoseId`／`PT/PoseIdTransient`、`0=Off`、`1..255=Pose`）へ集約可能に変更
- 共有Pose IDについて、対象・同期cost・専用`Int`へのfallback理由を表示する`PT-P010`と、255 poses超過・複数初期Pose・既存Expression Parameter属性競合を検出する`PT-P011`～`PT-P013`を追加

### Removed

- **破壊的変更:** 自動thumbnail／icon生成、PNG保存、cache、生成UI、生成AutoFix、diagnostic `PT-M004`／`PT-M005`／`PT-KB001`を削除
- **破壊的変更:** `PoseTuneRoot.enableIconGeneration`／`previewSettings`、`PoseMenu.generateIcons`、`PoseGroup.suppressIconGeneration`、`PoseClip.suppressIconGeneration`／`cameraOffset`、`PoseTunePreviewSettings`と生成専用Editor APIを削除

### Changed

- `PoseClip.customIcon`と`PoseGroup.icon`は常にmenu iconとして使用し、Group icon未指定時は初期Pose、次に先頭Poseの手動iconを使用
- Kawaii BlendTree flatten fallbackのdiagnosticをthumbnail用`PT-KB001`から独立した`PT-KB-FALLBACK`へ変更
- 旧版で生成済みのicon Assetと既存の`customIcon`参照は削除・変換せず、通常の手動iconとして維持
- Assistantのメニュータブを生成予定menu previewから実際のvalidation／allocation経路に基づくExpression Parameters previewへ変更し、PoseMenu未配置時も名前・型・同期・local・Saved属性を表示
- Animator／post-build validationを強化し、生成後Expression Parameterの重複、型、Saved／Synced属性と、共有Pose IDのentry／exit条件およびexclusive reset driverを検証
- NDMF生成時の重複Auto preemption遷移を統合し、handoff clipを共有、Height生成の未参照中間Assetを破棄して、互換動作を維持したままAnimator生成時間とAsset数を削減

## [0.3.0] - 2026-08-27

### Removed

- **破壊的変更:** custom Stable GUID、`StableComponentGuid`、Root／Group／PoseのStable GUID API、修復menu、重複diagnostic `PT-R007`／`PT-G007`／`PT-C011`を削除
- **破壊的変更:** `PoseTunePreset`、`AvatarAdjustmentPreset`、Preset data／適用API／Inspector／Assistantタブを削除

### Changed

- Editor永続Assetの識別をComponentの`GlobalObjectId` hashへ変更し、未保存Sceneではthumbnail生成とKawaii migrationを変更前に停止
- NDMF build内のRoot／Group／Pose、Animator名、内部parameter、graph hash、generated marker照合を、Component型・Avatar相対sibling-index path・同型Component indexから生成する構造IDへ統一
- Kawaii migration manifestをRoot／Poseの`GlobalObjectId`を保存する新形式へ変更し、run単位の生成先とrollbackを新しい識別子へ切り替え
- `PT-C004`をbuildを停止しないwarningへ変更し、NDMF build中の生成clipからTransform／Animator／BlendShape以外のsource curveを除外

### Fixed

- HierarchyでPoseTune GameObjectを複製すると複製前の識別子を共有し、`PT-C011`などの重複errorでbuildできなくなる問題を解消

## [0.2.0] - 2026-08-25

### Changed

- **破壊的変更:** tracking policy の最低単位を `PoseGroup` に統一し、`PoseClip.tracking`、`PoseClip.emitTrackingControl`、`PoseClipPresetData` の tracking 関連 field、および `PoseDefinition` の pose 単位 tracking field を削除
- **破壊的変更:** 汎用 Animator Controller import component、Assistant の import UI、関連する公開 API と diagnostic を削除
- **破壊的変更:** Bool 条件を `If`／`IfNot` のみに限定し、Bool の `Equals`／`NotEquals` と serialized `boolValue` を削除。Int の `Equals`／`NotEquals` は維持
- **破壊的変更:** 廃止予定だった Editor API shim と preset の英語名 `Pose Groups` fallback を削除
- `PoseTrackingPolicy` の解決順を `Group component → Root component → GroupKind default` に変更し、Group 内の全 Pose で Base／Desktop／VR／FBT policy と `generateResetOnExit` を共有
- tracking vote を Pose variant 単位から Group 内の distinct policy profile 単位へ変更し、同一 profile の Pose／Base／VR が vote ID を共有、全 `NoChange` profile は vote を生成しないように変更
- `PoseTunePreset` を schema v3 に更新して Root／Group policy のみを保存し、schema v1/v2 および空／不正な Stable GUID を変更前に原子的に拒否。Group／Pose の照合は Stable GUID のみに統一
- 無効な `PoseTrackingPolicy` を resolver、validation、preset capture で「存在しない」として扱い、preset Replace だけが物理 component の有無を完全一致させる契約に統一
- 旧 `PoseClip.tracking`／`PoseClip.emitTrackingControl` の inline YAML は自動移行せず、再シリアライズ時に失われ得る非互換境界として明文化

### Fixed

- 検証結果を diagnostic 単位で集約し、静的 pose の 0 秒 clip、KawaiiPosing の静的 root curve、全件未生成の thumbnail が大量の warning になる問題を修正
- NDMF build 時に同じ validation warning が PoseTune と NDMF の両方から二重出力される問題を修正
- Root／Group 以外に置かれた `PoseTrackingPolicy` を `PT-T004` validation error として build 前に検出。自動移行は行わず、Group policyへの手動移行を要求
- InspectorからGroup policyを追加しただけでbase／FBT／reset実効値が変わる問題を修正し、emission無効時は全`NoChange`の実効値を保持
- 削除済みのPose単位tracking fieldがPoseClip／preset drawerのproperty一覧に残っていた問題を修正
- 新規作成した`PoseTunePreset`がlegacy schemaとして拒否される問題を修正し、schema v3のnull policyデータはReplace前に原子的に拒否

## [0.1.3] - 2026-07-23

### Changed

- Animator Tracking Control を単一 context から group 別 vote と部位別 arbiter へ変更し、複数 pose を `Animation > Tracking > NoChange` で合成
- tracking reset を全身 reset から PoseTune が明示変更した部位だけの reset request へ変更し、Lock OFF 時も残存 vote を再適用
- `PoseTrackingPolicy` の優先順位を Pose component、legacy inline、Group、Root、kind default に統一し、Base／FBT／reset を同じ所有者から解決
- Animator import で Tracking Control Behaviour の有無を保持し、複数 Behaviour の `NoChange` を単位元として合成
- `PoseTunePreset` を schema v2 に更新し、Root／Group／Pose policy、全10部位、FBT override、reset を保存
- Kawaii の `mergeTrackingControl` と PoseTune の `emitTrackingControl` を分離し、`addTrackingPolicy` 選択時だけ近似 policy を生成
- Auto pose の勝者順を `priority → initial → menuOrder → displayName → id` に固定し、上位 pose への切替を cleanup/handoff 経由に統一
- PoseTunePreset の `Replace` を、GameObject を保持したままプリセット外の PoseGroup / PoseClip component を除去する完全置換へ変更
- nested PoseGroup の PoseClip 所有権を最も近い親 group に限定
- Kawaii migration の移行元処理を安全側の明示選択に変更し、BakeAtMigration 生成 Motion を永続 asset 化
- Kawaii OverrideDefines import を通常 pose と同じ互換変換・永続化経路へ統合

### Fixed

- `DesktopOnly` pose の Desktop／VR variant が有効な `VRMode` のまま即時退出し、手動選択中に handoff と再進入を反復する問題、および VR exclusive commit state 名から `_VR` が欠落する問題
- 非排他 group、Pose 切替、Override/Additive bucket、Desktop／VR／FBT variant で旧 tracking vote が残留または他 group の vote を消す問題
- tracking 無効構成でも全身 Tracking Behaviour を生成する問題、および NoChange 部位・外部 Eyes/Mouth 制御まで reset する問題
- 既定値と同じ Root policy が build で無視される問題と、`generateResetOnExit=false` への偽 warning
- legacy inline tracking を新規 authoring UI で編集できた問題を修正し、Undo 対応 component 変換を追加
- Preset Replace が PoseGroup と同居する PoseClip の GameObject 全体を削除する問題
- Kawaii migration が途中 Error 後も移行元を EditorOnly / 非アクティブ化する問題
- Auto-only `SelectedPosePerGroup` が未定義 selection parameter を参照する問題
- 既存同名 Expression Parameter を budget へ二重計上する問題
- nested Animator StateMachine の親遷移条件を import 時に失う問題
- Grounding が所有していない source Motion clip を破棄対象にする問題
- 不正な条件型 / 演算子、循環 ExpressionsMenu、Thumbnail の AnimationMode / Texture 所有権、生成 Animator validation の未接続を修正

## [0.1.2] - 2026-06-22

### Fixed

- Auto pose 時に pose の tracking option が頭/手/足ロックを上書きする問題を修正

## [0.1.1] - 2026-06-20

### Changed

- Pose Options 使用時にも pose group の active parameter を生成し、Action Playable を制御しない設定でも pose 選択状態を追跡できるように変更
- README に VPM repository URL を追記

### Fixed

- PoseTune 無効時または pose 未選択時に、tracking options と Locomotion Lock が解除されるように修正
- VR で頭/手/足ロックを OFF にした後、ジャンプなどの再評価まで tracking が戻らない問題を修正
- 条件付き pose の exit transition で `>=` / `<=` の閾値が entry と重なり、即時退出やフラッピングが起きうる問題を修正
- BlendShape curve と BlendTree leaf clip の validation が実際の対応範囲とずれていた問題を修正
- FBT override の manual entry が exclusive commit state を迂回し、他グループの reset が漏れる問題を修正
- PoseHeightAdjust、root menu 上限、local-only parameter、BlendTree parameter、空 condition parameter の validation 漏れを修正
- `LocomotionLock` parameter の保存挙動を他の lock option と同じ Saved に統一

## [0.1.0] - 2026-06-19

- 初回リリース
