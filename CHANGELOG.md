# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

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
