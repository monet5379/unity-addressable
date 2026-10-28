# Changelog

이 파일은 [Semantic Versioning](https://semver.org/) 형식을 따른다.

## [Unreleased]

## [0.3.0] - 2026-10-28

### Added

- **Runtime:** `AddressableLabelLoader` · `LoadRequest` · `LoadOperation` · `LabelLoadMode` — group id와 라벨 목록 sync/async 적재 · `CustomYieldInstruction` 진행률·완료 · `KeepResident` / `force` Release
- **Runtime:** `ResourcesManager.PreloadTypeLabels` — type 라벨 동기 boot preload (`PreloadLabelSync` 위임)
- **Runtime:** `AddressableAssetManager.PreloadLabelSync` / `PreloadLabelAsync` · `CountLabelLocationsAsync` / `CountLabelLocations` · `IsLabelLoaded` · `CopyLoadedLabels`
- **Runtime:** 라벨 **ref-count** — load group·단일 preload acquire 겹침 시 공유 라벨 eviction 방지
- **Tests:** `AddressableLabelLoaderEditTests` · `AddressableAssetManagerRefCountTests` · Play `LabelLoaderPlayModeTests` · `LabelRefCountPlayModeTests` · `CacheOwnershipTests` 확장

### Changed

- **Runtime:** `ReleaseLabel` — canonical void API; `ReleaseLabelRef`는 한 릴리스 호환용 **obsolete** bool alias
- **Runtime:** `AddressableLabelLoader.Shared` — `ResourcesManager.AssetManager`와 동일 캐시
- **Editor:** 레이아웃 도구 — **경로** / **스모크** 탭. Demo **부트 샘플 등록**은 `Tools → Addressable Layout → Demo` 메뉴로 이동
- **Docs:** README·Architecture — sync boot vs async Demo · load group · Dragon host parity 공개 표면

### Removed

- **Editor:** 레이아웃 도구 **스모크** 탭 · `ForEditorSmoke` (Edit 검증은 `CacheOwnershipTests` 등 테스트로 대체)

### Notes

- **Git URL pin:** `#v0.3.0`. Demo `DemoPlayModeTests`는 **async** boot 회귀만 검증; 호스트 sync boot는 `PreloadTypeLabels` / `LoadGroup(LabelLoadMode.Sync)`를 선택한다.
- **Ship gate:** `scripts/verify.ps1` — PrepareDemo · Edit 15/15 · Play 6/6 (2026-10-28).

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
