# Changelog

이 파일은 [Semantic Versioning](https://semver.org/) 형식을 따른다.

## [Unreleased]

### Added

- **Tests:** `PathManagerEditTests` — `ParseDatabase` · `CheckLoaded` · `ApplyBuiltDatabase` · `TryLookup` · 반복 `Load` (Edit Mode)
- **Tests:** `PathMetaWriter.BuildDatabase` temp-tree · `StringKeyedMap` 삽입 순서 무관 JSON 동일 (Edit Mode)

### Changed

- **Editor:** `PathMetaWriter.Write()` — Resources·Addressables 디스크 스캔과 `PathMetaData.json` 쓰기를 Runtime `PathManager`에서 분리. 갱신 직후 `PathManager.ApplyBuiltDatabase`로 메모리 Lookup 동기화. Play 중 쓰기 금지
- **Editor:** `PathMetaWriter.BuildDatabase` — 트리별 Unix 경로 정규화 후 Ordinal 정렬, `TryRegister`로 중복 처리. 경고 `kept={existing}, skipped={path}`
- **Runtime:** `StringKeyedMap` — `OnBeforeSerialize`에서 키 Ordinal 정렬 (`PathMetaData.json` 결정적 직렬화)

### Removed

- **Runtime:** `PathManager.UpdatePathMetadata()` — Editor `PathMetaWriter.Write()`로 이동
- **Editor:** `PathMetaWriter.IgnoreDirectorySet` — 미사용 디렉터리 ignore 루프 제거 (확장자·점 파일·`PathMetaData.json` 제외는 유지)

### Notes

- **Breaking (0.x):** PathMeta 갱신 호출을 `PathManager.UpdatePathMetadata()`에서 `PathMetaWriter.Write()`로 바꾼다 (Editor asmdef 참조).

## [0.4.0] - 2026-10-28

### Added

- **Runtime (internal):** `DirectAssetLoader` · `LabelAssetLoader` — PrimaryKey Direct handle·in-flight `Task`와 라벨 `(label, typeof(T))` bulk handle·in-flight `Task`를 `AddressableAssetManager`에서 분리
- **Runtime (internal):** `LabelReferenceTracker` — 라벨 ref-count·asset count (`CopyLoadedLabels` · acquire rollback)
- **Runtime (internal):** `AddressableLogging` — preload empty · settings.json / Invalid path 로드 오류 메시지

### Changed

- **Runtime:** `AddressableAssetManager` — partial 네 파일을 단일 `AddressableAssetManager.cs`로 병합; Direct/라벨 경로는 internal 로더·ref-tracker에 위임. **공개 메서드·캐시·ref-count 의미는 0.3.0과 동일**
- **Runtime:** load group 공개 타입 **`AddressableLabelLoader` → `AddressableLabelGroupManager`**, async runner **`AddressableLabelLoaderRunner` → `AddressableLabelGroupRunner`** (Unity Addressables **Group** 설정과 이름 혼동 방지)
- **Runtime:** `CacheOwnership` — 요청 키·location alias (`AddRequestAlias`, `AddLocationAliasForCache`)로 Lookup path·InternalId 해석
- **Tests:** `AddressableLabelLoaderEditTests` · `LabelLoaderPlayModeTests` — `AddressableLabelGroupManager` 사용 (테스트 클래스명은 유지)
- **Docs:** 패키지 README · repo Architecture — 로더 분리·rename 반영
- **Runtime:** `Runtime/Resource/Addressable/` — `Contracts` · `Cache` · `Loading` · `LabelGroups` 하위 폴더 (공개 API·네임스페이스 변경 없음)

### Fixed

- **Runtime:** 비동기 라벨 로드(`LabelAssetLoader`)에 generation 검사 추가 — `ReleaseAll` 도중 끝난 로드는 캐시에 넣지 않고 빈 결과를 돌려준다 (`DirectAssetLoader.LoadAndCacheAsync`와 동일)

### Removed

- **Runtime:** public API **`AddressableLabelLoader`** · **`AddressableLabelLoader.Shared`** — `#v0.3.0` pin 호스트는 **`AddressableLabelGroupManager` / `.Shared`** 로 타입명만 교체하면 된다 (메서드·`LoadRequest` 계약 동일)

### Notes

- **Breaking (C# 타입명만):** load group manager rename. 0.3.0 pin 호스트는 `#v0.4.0`으로 올릴 때 `using`/타입 참조를 `AddressableLabelGroupManager`로 갱신한다. 0.x 구간이라 breaking 변경을 minor로 올린다.

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
- **타입명:** 이 태그의 load group 공개 API는 **`AddressableLabelLoader`**. 이후 [Unreleased]에서 **`AddressableLabelGroupManager`** 로 rename한다.

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
