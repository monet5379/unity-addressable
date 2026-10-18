# Changelog

이 파일은 [Semantic Versioning](https://semver.org/) 형식을 따른다.

## [Unreleased]

### Changed

- **Runtime:** `AddressableAssetManager` — `CacheOwnership` owner-set 캐시 · Direct/라벨 handle 분리 · PrimaryKey in-flight `Task` 공유 · `ReleaseAll` generation 가드
- **Editor:** **레이아웃 도구** 창 — PathMeta · ForEditor 스모크 · Demo **부트 샘플 등록** (Path Settings · Refresh Paths 등 단독 Tools 메뉴 제거)
- **Sample:** Demo 검증을 `DemoPlayModeVerification`으로 분리 · `DemoAddressableHost`는 수동 Play·지연 부트스트랩만
- **Tests:** `AddressableLayout.EditorTests` / `CacheOwnershipTests` · `AddressableLayout.PlayModeTests` / `DemoPlayModeTests` (Test Runner **PlayMode** 탭)
- **Docs:** Architecture · README — Play 캐시 소유권 · 레이아웃 도구 · Test Runner 절차

## [0.1.0] - 2026-10-18

### Added

- UPM 패키지 `com.monet5379.addressable-layout` 초기 공개
- **Runtime:** `PathManager`, `AddressableAssetManager`, `AddressableLabels`, `ResourcesManager` (파일명 Lookup, 라벨 로드, 동기 hit-only, `SpawnPrefab` / `Despawn`)
- **Editor:** Path Settings, Refresh Paths, `ResourcesManagerForEditor`, Edit Mode 스모크 메뉴
- **Sample:** Demo (`Samples~/Demo`) — 부트·place 라벨·Register Boot Sample 연동

### Notes

- PathMeta 출력 위치: `Assets/Resources/Data/PathMetaData.json`
- 콘텐츠 루트: `Assets/Addressables/` (필수), `Assets/Resources/` (이중 스캔 시 선택)
