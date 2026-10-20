# Changelog

이 파일은 [Semantic Versioning](https://semver.org/) 형식을 따른다.

## [Unreleased]

### Changed

- **Editor:** 레이아웃 도구 — **경로** / **스모크** 탭. Demo **부트 샘플 등록**은 `Tools → Addressable Layout → Demo` 메뉴로 이동

## [0.2.0] - 2026-10-18

### Added

- **Runtime:** `IPrefabInstanceProvider` · `ResourcesManager.SetInstanceProvider` — `SpawnPrefab` / `Despawn` 인스턴스 생성·반환 seam (기본 Instantiate/Destroy)
- **Tests:** `PrefabInstanceProviderTests` (Edit Mode)

### Changed

- **Docs:** README 불변조건 — provider 등록·인스턴스 수명 vs Addressables 캐시 · LeanPool은 호스트 `IPrefabInstanceProvider` 구현

## [0.1.1] - 2026-10-18

### Added

- **Runtime:** `CacheOwnership` — owner-set 캐시 · Direct/라벨 handle 분리 · PrimaryKey in-flight `Task` 공유 · `ReleaseAll` generation 가드
- **Editor:** **레이아웃 도구** 창 — PathMeta · ForEditor 스모크 · Demo **부트 샘플 등록** · batchmode `AddressableLayoutVerify`
- **Sample:** Demo 마일스톤 검증 `DemoPlayModeVerification` · Test Runner용 지연 부트스트랩
- **Tests:** `AddressableLayout.EditorTests` / `CacheOwnershipTests` · `AddressableLayout.PlayModeTests` / `DemoPlayModeTests`
- **Docs:** Architecture · README — Play 캐시 소유권 · 레이아웃 도구 · Test Runner · `scripts/verify.ps1`

### Changed

- **Editor:** Path Settings · Refresh Paths 등 단독 Tools 메뉴 → 레이아웃 도구로 통합
- **Sample:** `DemoAddressableHost` — 수동 Play·부트스트랩 스케줄만 (검증 로직 분리)

### Notes

- **Git URL pin은 `#v0.1.1`을 권장한다.** `v0.1.0` 태그는 embedded 전환만 포함한 조기 스냅샷이다.

## [0.1.0] - 2026-10-18

### Added

- UPM embedded 패키지 `com.monet5379.addressable-layout` (조기 태그)
- **Runtime:** `PathManager`, `AddressableAssetManager`, `AddressableLabels`, `ResourcesManager` 골격
- **Editor:** Path Settings, Refresh Paths, `ResourcesManagerForEditor`, Edit Mode 스모크 메뉴
- **Sample:** `Samples~/Demo` 스켈레톤 (`DemoBootSample`, `DemoAddressableSetup`)

### Notes

- PathMeta 출력 위치: `Assets/Resources/Data/PathMetaData.json`
- 콘텐츠 루트: `Assets/Addressables/` (필수), `Assets/Resources/` (이중 스캔 시 선택)
