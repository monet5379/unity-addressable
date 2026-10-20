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
https://github.com/monet5379/unity-addressable.git?path=/project/unity-addressable/Packages/com.monet5379.addressable-layout#v0.2.0
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
| **Tools → Addressable Layout → 레이아웃 도구** | PathMeta 갱신(경로 탭) · ForEditor 스모크(스모크 탭) |
| **Tools → Addressable Layout → Demo → 부트 샘플 등록** | Demo Sample Addressables·PathMeta 등록 |

## Demo 샘플 (선택)

Package Manager에서 **Addressable Layout → Samples → Demo → Import** 후:

1. **Tools → Addressable Layout → Demo → 부트 샘플 등록** (PathMeta 갱신 포함)
2. 필요 시 **갱신**만 다시 실행
3. **Window → General → Test Runner → Play Mode** — `DemoPlayModeTests` 실행 (또는 Play로 `DemoAddressableHost` 수동 확인)

Demo는 출시 카탈로그가 아닌 **놀이터**이다. 마일스톤 A–F 로직은 `DemoPlayModeVerification`에 있다.

## Runtime API (요약)

| 타입 | 역할 |
| --- | --- |
| `PathManager` | 파일명 → 경로 · PathMeta 갱신 |
| `AddressableAssetManager` | Addressables 로드 · owner-set 캐시 · Direct/라벨 handle |
| `AddressableLabels` | 종류 / 지역 라벨 상수 |
| `IPrefabInstanceProvider` | `SpawnPrefab` / `Despawn` 인스턴스 생성·반환 (풀링 등 — 호스트 구현) |
| `ResourcesManager` | `LoadResource` / async / 라벨 preload · `SpawnPrefab` / `Despawn` · `SetInstanceProvider` |
| `ResourcesManagerForEditor` (Editor) | Edit Mode 전용 로드 |

**Play 동기 `LoadResource`:** Addressables 경로는 **캐시 hit만** (miss → null). Preload 또는 `LoadResourceAsync`를 사용한다.

**`SpawnPrefab` / `Despawn`:** prefab asset 로드까지는 패키지, **인스턴스**는 `IPrefabInstanceProvider` (미등록 시 Instantiate/Destroy). provider는 **첫 `SpawnPrefab` 전** 등록. Play마다 `RuntimeInitializeOnLoadMethod` 등록 권장. `Release` / `ReleaseLabel` / `ReleaseAll` 전에 해당 prefab으로 만든 인스턴스(풀 비활성 포함)는 호스트가 정리 — 패키지는 풀을 모른다.

## Tests

| Test Runner 탭 | asmdef | 테스트 | 전제 |
| --- | --- | --- | --- |
| **Edit Mode** | `AddressableLayout.EditorTests` | `CacheOwnershipTests` · `PrefabInstanceProviderTests` | 없음 |
| **Play Mode** | `AddressableLayout.PlayModeTests` | `DemoPlayModeTests` | Demo import · **부트 샘플 등록** |

## 레포 문서

상세 불변조건·Architecture는 [GitHub 레포 README](https://github.com/monet5379/unity-addressable) 및 `docs/` 를 참고한다.

## License

[MIT](https://github.com/monet5379/unity-addressable/blob/main/LICENSE)
