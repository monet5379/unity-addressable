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
https://github.com/monet5379/unity-addressable.git?path=/project/unity-addressable/Packages/com.monet5379.addressable-layout#v0.5.0
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
| **Tools → Addressable Layout → 레이아웃 도구** | PathMeta 갱신 |
| **Tools → Addressable Layout → Demo → 부트 샘플 등록** | Demo Sample Addressables·PathMeta 등록 |

## Demo 샘플 (선택)

Package Manager에서 **Addressable Layout → Samples → Demo → Import** 후:

1. **Tools → Addressable Layout → Demo → 부트 샘플 등록** (PathMeta 갱신 포함)
2. 필요 시 **갱신**만 다시 실행
3. **Window → General → Test Runner → Play Mode** — `DemoPlayModeTests` 실행 (또는 Play로 `DemoAddressableHost` 수동 확인)

Demo는 출시 카탈로그가 아닌 **놀이터**이다. 마일스톤 A–F 로직은 `DemoPlayModeVerification`에 있다. **Demo Play 검증은 async 라벨 preload 부트**를 유지한다. 타이틀 sync boot는 아래 API를 쓰며 Demo 회귀와 별개이다.

## 불변조건 (요약)

레포 [루트 README](https://github.com/monet5379/unity-addressable)가 계약 정본이다. 패키지 소비 시 특히 다음을 지킨다.

- **파일명 전역 키** · **`Assets/Addressables/`** 단일 콘텐츠 루트 · **종류(type) 라벨 ≠ place 라벨**
- Play Addressables path: **`LoadResource` / `LoadResourceByName` = 캐시 hit만** (miss → `null`). Preload 또는 `LoadResourceAsync`
- **`PathManager.Load()`** 후 (선택) 라벨 preload → gameplay
- **라벨 ref-count:** `LoadResourcesByLabelAsync` · `PreloadLabel*` · load group acquire마다 ref++. `ReleaseLabel`은 ref 1 감소; **0일 때만** label handle·label owner eviction. ref에 없는 라벨 release는 no-op
- **`LoadRequest.AssetType`** 은 `(label, typeof(T))` handle 키와 맞출 Addressables asset type (단일 preload·그룹·`LoadResourcesByLabelAsync<T>`는 **같은 T** 권장)
- **`SpawnPrefab` / `Despawn`:** prefab asset은 패키지, **인스턴스**는 `IPrefabInstanceProvider`. `Release` / `ReleaseLabel` / `ReleaseAll` 전 호스트가 인스턴스(풀 비활성 포함) 정리

## Boot: async Demo vs sync 호스트

| 경로 | 용도 |
| --- | --- |
| `LoadResourcesByLabelAsync<T>` · `EnterPlaceAsync<T>` | async preload (Demo `DemoPlayModeTests` 기본) |
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
| `AddressableLabels` | 종류 / Demo place 라벨 상수 (ship place·enemy enum 없음) |
| `AddressableLabelGroupManager` | load group 적재·release |
| `LoadRequest` / `LoadOperation` / `LabelLoadMode` | 그룹 요청·진행·sync/async |
| `IPrefabInstanceProvider` | `SpawnPrefab` / `Despawn` 인스턴스 (호스트 구현) |
| `ResourcesManager` | Play 파사드 · `PreloadTypeLabels` · place · Spawn |
| `ResourcesManagerForEditor` (Editor) | Edit Mode 전용 로드 |

## Host parity (Dragon `TSResourcesManager` 기대 표면)

Project Dragon 임베드 Layout 소비 시 맞추는 **공개 API** prose 목록이다. typed spawn·GameApp·씬 orchestration은 타이틀에 둔다.

**`ResourcesManager`:** `AssetManager`, `LoadResource` / `LoadResourceByName` / `LoadResourceAsync`, `LoadResourcesByLabelAsync`, `PreloadTypeLabels` (및 `failOnEmptyInDev` overload), `EnterPlaceAsync` / `LeavePlace`, `Release` / **`ReleaseLabel`** / **`ReleaseLabelRef`** (obsolete bool), `ReleaseAll`, `SpawnPrefab` / `Despawn`, `SetInstanceProvider`, `IsResourceLoaded`.

**`AddressableAssetManager`:** `TryGetCached`, `LoadResourceAsync`, `LoadResourcesByLabelAsync`, **`PreloadLabelSync<T>`** / **`PreloadLabelAsync<T>`**, **`CountLabelLocationsAsync`** / **`CountLabelLocations`**, **`IsLabelLoaded`**, **`CopyLoadedLabels`**, `Release` / **`ReleaseLabel`** / **`ReleaseLabelRef`**, `ReleaseAll`.

**Load group:** **`AddressableLabelGroupManager.Shared`**, `LoadGroup`, `Release`, `IsLoaded`, `TryGetOperation` — **`LoadRequest`**, **`LoadOperation`**, **`LabelLoadMode`**.

**API 의미 (0.3.0 이후 동일):** Play sync hit-only, `IPrefabInstanceProvider`, `EnterPlaceAsync` / `LeavePlace`, `AddressableLabels` = Demo·제네릭 type 상수만. **0.4.0:** load group 공개 타입명만 `AddressableLabelLoader` → `AddressableLabelGroupManager` (CHANGELOG [0.4.0]).

상세 캐시·owner 규칙: 레포 `docs/architecture/Architecture_AddressableLayout.md` · `Architecture_BootFlow.md`.

## Tests

| Test Runner 탭 | asmdef | 테스트 | 전제 |
| --- | --- | --- | --- |
| **Edit Mode** | `AddressableLayout.EditorTests` | `PathManagerEditTests` · `CacheOwnershipTests` · `PrefabInstanceProviderTests` · `AddressableLabelLoaderEditTests` · `AddressableAssetManagerRefCountTests` | Demo import 시 `Load_CalledTwice` 등 일부 PathMeta 테스트 활성 |
| **Play Mode** | `AddressableLayout.PlayModeTests` | `DemoPlayModeTests` · `LabelLoaderPlayModeTests` · `LabelRefCountPlayModeTests` | Demo import · **부트 샘플 등록** |

레포 루트 `scripts/verify.ps1` — PrepareDemo → Edit (**25**) → Play (**6**) 일괄 (`PathManagerEditTests` 포함).

## 레포 문서

상세 Architecture·ADR: [GitHub 레포 README](https://github.com/monet5379/unity-addressable) 및 `docs/` .

## License

[MIT](https://github.com/monet5379/unity-addressable/blob/main/LICENSE)
