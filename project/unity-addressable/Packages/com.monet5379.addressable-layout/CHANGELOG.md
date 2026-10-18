# Changelog

이 파일은 [Semantic Versioning](https://semver.org/) 형식을 따른다.

## [0.1.0] - 2026-10-18

### Added

- UPM 패키지 `com.monet5379.addressable-layout` 초기 공개
- **Runtime:** `PathManager`, `AddressableAssetManager`, `AddressableLabels`, `ResourcesManager` (파일명 Lookup, 라벨 로드, 동기 hit-only, `SpawnPrefab` / `Despawn`)
- **Editor:** Path Settings, Refresh Paths, `ResourcesManagerForEditor`, Edit Mode 스모크 메뉴
- **Sample:** Demo (`Samples~/Demo`) — 부트·place 라벨·Register Boot Sample 연동

### Notes

- PathMeta 출력 위치: `Assets/Resources/Data/PathMetaData.json`
- 콘텐츠 루트: `Assets/Addressables/` (필수), `Assets/Resources/` (이중 스캔 시 선택)
