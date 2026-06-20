# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]

### Changed

- Pose Options 使用時にも pose group の active parameter を生成し、Action Playable を制御しない設定でも pose 選択状態を追跡できるように変更
- README に VPM repository URL を追記

### Fixed

- PoseTune 無効時または pose 未選択時に、tracking options と Locomotion Lock が解除されるように修正
- VR で頭/手/足ロックを OFF にした後、ジャンプなどの再評価まで tracking が戻らない問題を修正

## 0.1.0 - 2026-06-19

- 初回リリース
