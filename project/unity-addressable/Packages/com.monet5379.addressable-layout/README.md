# Addressable Layout

파일명 기반 **Addressables / Resources** 경로 Lookup, 라벨 preload, Play 모드 **동기 로드 = 캐시 hit-only**를 제공하는 UPM 패키지이다.

## 요구 사항

- Unity **6000.3** 이상
- `com.unity.addressables` **2.8.1** (패키지 의존성으로 설치됨)

## 설치

### Git URL (권장)

1. **Window → Package Manager → + → Install package from git URL**
2. 아래 URL 입력 (태그는 릴리스 버전에 맞게 변경):

```text
https://github.com/monet5379/unity-addressable.git?path=/project/unity-addressable/Packages/com.monet5379.addressable-layout#v0.7.0
```

### 로컬 임베디드

이 레포의 `project/unity-addressable/Packages/com.monet5379.addressable-layout` 폴더를 Unity 프로젝트 `Packages/` 아래에 두면 Package Manager **In Project**에 표시된다.

## 프로젝트 초기 설정

패키지는 **게임 콘텐츠 경로를 스캔하지 않는다.** 호스트 프로젝트에 아래를 준비한 뒤 PathMeta를 한 번 갱신한다.

1. **`Assets/Addressables/`** 폴더를 만든다. (단수 `Addressable/` 사용 금지)
2. (선택) 이관 중이면 **`Assets/Resources/`** 도 사용할 수 있다.
3. **Tools → Addressable Layout → 레이아웃 도구** → **갱신**을 실행한다.
4. **`Assets/Resources/Data/PathMetaData.json`** 이 생성·갱신되었는지 확인한다.

런타임 Lookup 전에 **`PathManager.Load()`** 로 PathMeta를 로드하는 흐름을 부트에 넣는다.

## Editor 메뉴

| 메뉴 | 설명 |
| --- | --- |
| **Tools → Addressable Layout → 레이아웃 도구** | PathMeta **갱신** (Resources·Addressables 디스크 스캔) |

## 불변조건 (요약)

레포 [루트 README](https://github.com/monet5379/unity-addressable)가 계약 정본이다. 패키지 소비 시 특히 다음을 지킨다.

- **파일명 전역 키** · **`Assets/Addressables/`** 단일 콘텐츠 루트 · **종류(type) 라벨 ≠ place 라벨**
- Play Addressables path: **`LoadResource` / `LoadResourceByName` = 캐시 hit만** (miss → `null`). Preload 또는 `LoadResourceAsync`
- **`PathManager.Load()`** 후 (선택) 라벨 preload → gameplay
- **라벨 ref-count:** `LoadResourcesByLabelAsync` · `PreloadLabel*` · load group acquire마다 ref++. `ReleaseLabel`은 ref 1 감소; **0일 때만** label handle·label owner eviction. ref에 없는 라벨 release는 no-op
- **`LoadRequest.AssetType`** 은 `(label, typeof(T))` handle 키와 맞출 Addressables asset type (단일 preload·그룹·`LoadResourcesByLabelAsync<T>`는 **같은 T** 권장)
- **`SpawnPrefab` / `Despawn`:** prefab asset은 패키지, **인스턴스**는 `IPrefabInstanceProvider`. `Release` / `ReleaseLabel` / `ReleaseAll` 전 호스트가 인스턴스(풀 비활성 포함) 정리

## Boot: async vs sync 호스트

| 경로 | 용도 |
| --- | --- |
| `LoadResourcesByLabelAsync<T>` · `EnterPlaceAsync<T>` | async preload (레포 dev Play 테스트 기본) |
| `ResourcesManager.PreloadTypeLabels` | type 라벨 **동기** boot — 내부 `PreloadLabelSync<Object>` |
| `AddressableAssetManager.PreloadLabelSync<T>` / `PreloadLabelAsync<T>` | 단일 라벨 sync 또는 coroutine/async 파이프라인 |
| `AddressableLabelGroupManager.LoadGroup(LoadRequest)` | group id + 라벨 목록. `LabelLoadMode.Sync`면 Play에서 첫 gameplay frame 전 완료 가능; `Async`는 runner coroutine |

로딩 UI: `LoadOperation`은 `CustomYieldInstruction` (`Progress`, `Completed`, `LabelAssetCounts`, `TotalAssetCount`). 사전 카운트: `CountLabelLocationsAsync` / `CountLabelLocations`.

## Load group (요약)

- **`LoadRequest`:** `Id`, `Labels`, `Mode` (`Sync` / `Async`), `KeepResident`, `FailOnEmptyInDev`, `AssetType`
- **`AddressableLabelGroupManager.Shared`:** `ResourcesManager.AssetManager`와 **동일** 캐시
- **`Release(groupId, force: false)`:** `KeepResident`면 무시; `force: true`면 resident 포함 해제
- 그룹 release는 acquire한 라벨마다 ref-count 1 감소 — 다른 groupId가 같은 라벨을 잡고 있으면 캐시 유지

## Runtime API (요약)

| 타입 | 역할 |
| --- | --- |
| `PathManager` | 파일명 → 경로 Lookup · PathMeta `Load()` |
| `PathMetaWriter` (Editor) | PathMeta 디스크 스캔 · `PathMetaData.json` 갱신 (Play 중 금지) |
| `AddressableAssetManager` | Addressables 로드 · owner-set 캐시 · Direct/라벨 handle · label preload/ref-count |
| `AddressableLabelGroupManager` | load group 적재·release |
| `LoadRequest` / `LoadOperation` / `LabelLoadMode` | 그룹 요청·진행·sync/async |
| `IPrefabInstanceProvider` | `SpawnPrefab` / `Despawn` 인스턴스 (호스트 구현) |
| `ResourcesManager` | Play 파사드 · `PreloadTypeLabels` · place · Spawn |
| `ResourcesManagerForEditor` (Editor) | Edit Mode 전용 로드 · `ForEachLabeledAsset` · `EditModeSyncCacheSeed` |
| `PathMetaValidation` (Editor) | PathMeta 디스크·라벨 검증 (호스트 EditMode 테스트) |

## Host parity (Dragon `TSResourcesManager` 기대 표면)

Project Dragon 임베드 Layout 소비 시 맞추는 **공개 API** prose 목록이다. typed spawn·GameApp·씬 orchestration은 타이틀에 둔다.

**`ResourcesManager`:** `AssetManager`, `LoadResource` / `LoadResourceByName` / `LoadResourceAsync`, `LoadResourcesByLabelAsync`, `PreloadTypeLabels` (및 `failOnEmptyInDev` overload), `EnterPlaceAsync` / `LeavePlace`, `Release` / **`ReleaseLabel`** / **`ReleaseLabelRef`** (obsolete bool), `ReleaseAll`, `SpawnPrefab` / `Despawn`, `SetInstanceProvider`, `IsResourceLoaded`. **Editor:** `RegisterSyncCacheHit` / `ClearSyncCacheSeeds` (EditMode fixture).

**Editor:** `EditModeSyncCacheSeed.Seed` · `PathMetaValidation` · `ResourcesManagerForEditor.ForEachLabeledAsset`.

**`AddressableAssetManager`:** `TryGetCached`, `LoadResourceAsync`, `LoadResourcesByLabelAsync`, **`PreloadLabelSync<T>`** / **`PreloadLabelAsync<T>`**, **`CountLabelLocationsAsync`** / **`CountLabelLocations`**, **`IsLabelLoaded`**, **`CopyLoadedLabels`**, `Release` / **`ReleaseLabel`** / **`ReleaseLabelRef`**, `ReleaseAll`.

**Load group:** **`AddressableLabelGroupManager.Shared`**, `LoadGroup`, `Release`, `IsLoaded`, `TryGetOperation` — **`LoadRequest`**, **`LoadOperation`**, **`LabelLoadMode`**.

**API 의미 (0.3.0 이후 동일):** Play sync hit-only, `IPrefabInstanceProvider`, `EnterPlaceAsync` / `LeavePlace`. **type·place 라벨 문자열**은 호스트 Addressables 설정과 동일한 상수를 프로젝트에 둔다 (UPM에 `AddressableLabels` 없음). **0.4.0:** load group 공개 타입명만 `AddressableLabelLoader` → `AddressableLabelGroupManager` (CHANGELOG [0.4.0]). **0.7.0:** dev place 라벨은 레포 `DevTestLabels` 등 fixture 전용.

상세 캐시·owner 규칙: 레포 `docs/architecture/Architecture_AddressableLayout.md` · `Architecture_BootFlow.md`.

## Tests (레포 dev 전용)

통합 테스트 asmdef·fixture는 UPM에 포함되지 않는다. **`project/unity-addressable/Assets/AddressableLayoutDev/`** 에 `AddressableLayout.EditorTests` · `AddressableLayout.PlayModeTests` · Demo fixture가 있다.

| Test Runner 탭 | asmdef | 테스트 | 전제 |
| --- | --- | --- | --- |
| **Edit Mode** | `AddressableLayout.EditorTests` | `PathManagerEditTests` · `CacheOwnershipTests` · … · `SyncCacheSeedTests` | `PrepareTestCatalog` 후 PathMeta 일부 케이스 활성 |
| **Play Mode** | `AddressableLayout.PlayModeTests` | `DemoPlayModeTests` · `LabelLoaderPlayModeTests` · `LabelRefCountPlayModeTests` | dev catalog (`RegisterBootSample` / verify `prepare-test-catalog`) |

레포 루트 `scripts/verify.ps1` — `prepare-test-catalog` → Edit (**34**) → Play (**6**).

## 레포 문서

| | |
| --- | --- |
| [루트 README](https://github.com/monet5379/unity-addressable/blob/main/README.md) | 불변조건 |
| [CONTEXT.md](https://github.com/monet5379/unity-addressable/blob/main/CONTEXT.md) | 용어 |
| [docs/architecture/](https://github.com/monet5379/unity-addressable/tree/main/docs/architecture) | BootFlow · Lookup · 캐시·handle |

## License

[MIT](https://github.com/monet5379/unity-addressable/blob/main/LICENSE)
