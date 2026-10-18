# Addressable Layout

파일명 기반 **Addressables / Resources** 경로 Lookup, 라벨 preload, Play 모드 **동기 로드 = 캐시 hit만**을 제공하는 UPM 패키지입니다.

## 요구 사항

- Unity **6000.3** 이상
- `com.unity.addressables` **2.8.1** (패키지 의존성으로 설치됨)

## 설치

### Git URL (권장)

1. **Window → Package Manager → + → Install package from git URL**
2. 아래 URL 입력 (태그는 릴리스 버전에 맞게 변경):

```text
https://github.com/monet5379/unity-addressable.git?path=/project/unity-addressable/Packages/com.monet5379.addressable-layout#v0.1.0
```

### 로컬 임베디드

이 레포의 `project/unity-addressable/Packages/com.monet5379.addressable-layout` 폴더를 사용하는 Unity 프로젝트의 `Packages/` 아래에 두면 Package Manager **In Project**에 표시됩니다.

## 프로젝트 초기 설정

패키지는 **게임 콘텐츠 경로를 스캔하지 않습니다.** 호스트 프로젝트에 아래를 준비한 뒤 PathMeta를 한 번 갱신하세요.

1. **`Assets/Addressables/`** 폴더를 만듭니다. (단수 `Addressable/` 사용 금지)
2. (선택) 이관 중이면 **`Assets/Resources/`** 도 사용할 수 있습니다.
3. **Tools → Addressable Layout → Refresh Paths** (또는 **Path Settings**에서 Refresh)를 실행합니다.
4. **`Assets/Resources/Data/PathMetaData.json`** 이 생성·갱신되었는지 확인합니다.

런타임 Lookup 전에 **`PathManager.Load()`** 로 PathMeta를 로드하는 흐름을 부트에 넣으세요.

## Editor 메뉴

| 메뉴 | 설명 |
| --- | --- |
| **Tools → Addressable Layout → Refresh Paths** | Resources + Addressables 이중 스캔 → PathMeta JSON 갱신 |
| **Tools → Addressable Layout → Path Settings** | Refresh + 엔트리 수 확인 |
| **Tools → Addressable Layout → Smoke ForEditor (Edit Mode)** | Edit Mode 로드 스모크 (Play 캐시 미사용) |

## Demo 샘플 (선택)

Package Manager에서 **Addressable Layout → Samples → Demo → Import** 후:

1. **Tools → Addressable Layout → Demo → Register Boot Sample** — 샘플 에셋·라벨 등록
2. **Refresh Paths** 다시 실행
3. Play로 부트·place 라벨 동작 확인

Demo는 출시 카탈로그가 아닌 **놀이터**입니다.

## Runtime API (요약)

| 타입 | 역할 |
| --- | --- |
| `PathManager` | 파일명 → 경로 · PathMeta 갱신 |
| `AddressableAssetManager` | Addressables 로드 · 캐시 |
| `AddressableLabels` | 종류 / 지역 라벨 상수 |
| `ResourcesManager` | `LoadResource` / async / 라벨 preload · `SpawnPrefab` / `Despawn` |
| `ResourcesManagerForEditor` (Editor) | Edit Mode 전용 로드 |

**Play 동기 `LoadResource`:** Addressables 경로는 **캐시 hit만** (miss → null). Preload 또는 `LoadResourceAsync`를 사용하세요.

## 레포 문서

상세 불변조건·아키텍처·마일스톤은 [GitHub 레포 README](https://github.com/monet5379/unity-addressable) 및 `docs/` 를 참고하세요.

## License

[MIT](https://github.com/monet5379/unity-addressable/blob/main/LICENSE)
