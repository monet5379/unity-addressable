# Runtime / Editor

Play Mode와 Edit Mode는 **같은 PathManager 규칙**을 쓰지만, **Addressables 쪽 구현과 캐시**가 다르다. 혼동하면 “에디터에선 되는데 Play sync는 null” 같은 버그가 난다.

## 두 파사드

| | `ResourcesManager` (Runtime) | `ResourcesManagerForEditor` (Editor) |
| --- | --- | --- |
| 실행 | Player / Play Mode | Edit Mode, Editor 스크립트 |
| Addressables 단일 | 캐시 hit-only sync · async는 Addressables API | `AssetDatabase.LoadAssetAtPath` |
| Resources leaf | `Resources.Load` | `Resources.Load` |
| 라벨 로드 | `LoadResourcesByLabelAsync` → Addressables + **Play 캐시 채움** | `LoadResourcesByLabelForEditor` → settings 엔트리 순회 + AssetDatabase |
| Play 캐시 | `AddressableAssetManager` static 인스턴스 | **절대 채우지 않음** |

불변조건: **Edit `*ForEditor` ≠ Play sync** — [README](../../README.md).

## Play: AddressableAssetManager 캐시

- 키: 주로 `IResourceLocation.PrimaryKey` (GUID). 요청 주소·`InternalId`는 **alias**로 같은 객체를 가리킨다.
- 목적: PathMeta의 `Assets/...` 주소로 `LoadResource` sync hit-only가 동작하게 한다.
- 릴리즈: `Release` / `ReleaseLabel` / `ReleaseAll` — `AsyncOperationHandle`과 dictionary를 함께 정리.

라벨 preload (`LoadResourcesByLabelAsync`):

- `LoadAssetsAsync` + locations로 캐시·라벨→키 목록 갱신
- 이후 `LoadResource` / `SpawnPrefab` sync 사용 가능 (Addressables path)

## Edit: AssetDatabase 경로

### `LoadResourceForEditor<T>(pathOrAddress)`

```text
pathOrAddress
  ├─ IsAddressablePath == false → Resources.Load<T>(leaf)
  └─ true → AssetDatabase.LoadAssetAtPath<T>(Assets/...)
```

- **동기 Addressables API를 부르지 않음**
- Play 캐시를 건드리지 않음 → Edit Mode 스모크 후 Play를 바로 돌려도 preload 상태가 오염되지 않음

### `LoadResourcesByLabelForEditor<T>(label)`

- `AddressableAssetSettingsDefaultObject.Settings`의 모든 group entry에서 `entry.labels.Contains(label)` 필터
- `AssetDatabase.LoadAssetAtPath<T>(entry.AssetPath)`

Play 라벨 로드와 **같은 gotcha**: 에셋이 폴더에만 있고 Addressable entry·라벨이 없으면 **빈 목록**.

## Editor-only: PathMeta 갱신

| 진입점 | 동작 |
| --- | --- |
| **Tools → Addressable Layout → Refresh Paths** | `PathMetaRefresh.Refresh()` |
| **Path Settings** 윈도우 | Refresh + 엔트리 수 표시 |
| **Smoke ForEditor (Edit Mode)** | `LoadResourceForEditor` / `LoadResourcesByLabelForEditor` 스모크 |

`UpdatePathMetadata()`는 `Application.dataPath` 아래 디스크를 스캔하고 JSON을 쓴다. **Player 빌드 런타임 경로가 아님** — shipping 앱은 Refresh 없이 packaged `PathMetaData.json`만 `Load()`한다.

## 언제 무엇을 쓸까

| 작업 | API |
| --- | --- |
| 실제 게임 Play 로드·스폰 | `ResourcesManager` |
| Edit Mode 툴·프리뷰·배치 스크립트 | `ResourcesManagerForEditor` |
| JSON Lookup 갱신 | Editor Refresh (Play에서 하지 않음) |
| “Play와 동일한 sync hit” 검증 | Play Mode + preload 후 `LoadResource` |

Edit Mode에서 `LoadResourceForEditor`로 성공했다고 해서, Play에서 preload 없이 sync Addressables load가 되는 것은 **아니다**.

## asmdef

- `AddressableLayout.Runtime` — Player + Editor Play에서 로드
- `AddressableLayout.Editor` — `includePlatforms: Editor` only, Runtime 참조

게임 어셈블리는 Runtime만 참조하면 되고, 커스텀 Editor 툴은 Editor asmdef에서 `AddressableLayout.Editor` + `ResourcesManagerForEditor`를 쓰면 된다.

## 관련 Verify

- Play: [milestones.md](../milestones.md) A–D, F — `DemoAddressableHost`
- Edit: 마일스톤 E — **Smoke ForEditor (Edit Mode)**
