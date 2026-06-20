# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]

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
