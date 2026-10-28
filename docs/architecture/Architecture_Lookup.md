# Architecture: Lookup

## 개요 (Overview)

*파일명(확장자 포함)을 전역 키로 PathMeta Lookup을 만들고, Resources leaf와 Addressables 주소를 한 dictionary로 이관한다.*

게임·툴은 경로 문자열 대신 파일명 키를 쓰고, Resources → Addressables 이관은 Lookup 값만 바뀐다. 계약 정본은 [루트 README](../../README.md)이다.

## 책임과 경계 (Responsibilities & Boundaries)

*PathMeta 저장·Refresh 스캔·Lookup API·이관 절차만 소유한다. Play 캐시·Editor AssetDatabase는 [Architecture_RuntimeEditor](Architecture_RuntimeEditor.md)를 본다.*

### 포함 범위 (In Scope)

- `PathMetaData.json` 위치 · `StringKeyedMap` · `PathManager.Lookup` / 확장자 헬퍼
- Editor `PathMetaWriter.Write()` 이중 스캔 · 중복 규칙 · 제외 확장자

### 제외 범위 (Out of Scope)

- Addressables Groups·라벨 UI (Unity + 타이틀)
- 자동 일괄 마이그레이터
- 라벨 preload 순서 → [Architecture_BootFlow](Architecture_BootFlow.md)

## 주요 타입과 관계 (Key Types & Relationships)

| 항목 | 값 |
| --- | --- |
| 디스크 | `Assets/Resources/Data/PathMetaData.json` |
| Runtime 로드 | `Resources.Load(PathManager.PathMetaResourcesLoadKey)` — 기본 `"Data/PathMetaData"` |
| 상수 | `PathManager.PathMetaResourcesLoadKey` · `PathMetaWriter.TargetPath` (동일 leaf·제외 규칙의 **단일 정본**) |
| 직렬화 | `StringKeyedMap` → `Dictionary<string,string>` |

| API | 용도 |
| --- | --- |
| `Lookup("Foo.prefab")` | 파일명 → path |
| `FindPrefabPath("Foo")` | `.prefab` 붙여 Lookup |
| `FindImagePath` / `FindMaterialPath` / … | 확장자별 헬퍼 |
| `Load()` | JSON → `_database` |
| `ApplyBuiltDatabase(StringKeyedMap)` | Editor 갱신 직후 메모리 Lookup 동기화 (`PathMetaWriter.Write()` 후) |
| `CheckLoaded()` | 메모리 로드됨 (`_isLoaded` && 항목 수 > 0) |

## 흐름 (Flow)

*Lookup이 path 종류를 나누고 Refresh가 dictionary를 만든다.*

### 런타임 흐름 (Runtime Flow)

```text
LoadResourceByName / SpawnPrefab
  → Lookup(filename) → path
  → IsAddressablePath(path)?
       ├─ false → Resources.Load(leaf)
       └─ true  → AddressableAssetManager TryGetCached (sync hit-only)
```

### 에디터 흐름 (Editor Flow)

```text
Tools → 레이아웃 도구 → 갱신
  → PathMetaRefresh.Refresh()
  → PathMetaWriter.Write()
  → PathManager.ApplyBuiltDatabase (메모리 Lookup)
  → AssetDatabase.Refresh()
```

PathMeta 디스크 갱신은 **Editor `PathMetaWriter.Write()`만** 호출한다. Runtime `PathManager.UpdatePathMetadata()`는 0.5.0에서 제거되었다.

스캔 순서 (**이중 스캔**):

```text
1. Assets/Resources/**   (재귀)
2. Assets/Addressables/** (재귀)
```

각 파일은 **파일명(확장자 포함)** 을 dictionary 키로 등록한다.

트리마다 경로를 `/`로 맞춘 뒤 **Ordinal** 정렬하고, 등록은 `TryRegister` 한곳에서 처리한다. JSON 저장 시 `StringKeyedMap`이 키를 같은 규칙으로 정렬해, 동일 에셋 집합이면 **갱신을 반복해도 diff가 없도록** 한다.

## 불변조건과 주의점 (Invariants & Gotchas)

*PathMeta와 Addressables 라벨은 별 계층이다.*

**경로 두 종류** — `PathManager.IsAddressablePath(path)`:

| | Addressables | Resources leaf |
| --- | --- | --- |
| 판별 | `Assets/` 접두 | 그 외 |
| PathMeta 예 | `Assets/Addressables/.../Bar.prefab` | `Demo/Foo` |
| Play sync | 캐시 hit-only | `Resources.Load` |

**중복** — 키가 이미 있으면 덮어쓰지 않고 `PathMeta 파일명 중복; kept={existing}, skipped={path}` 를 남긴다.

| 상황 | 동작 |
| --- | --- |
| 같은 트리 안 동일 파일명 | Ordinal로 **앞선 전체 경로**가 먼저 등록 · 이후 path는 `skipped` |
| Resources vs Addressables 동일 파일명 | **Resources wins** (Resources 스캔이 먼저) · Addressables path는 `skipped` |

**제외** — `.meta`, `.shader`, `.cginc`, `.text` (확장자 대소문자 무시); 점(`.`)으로 시작하는 파일명; `PathMetaData.json`은 Resources 스캔 제외. 디렉터리 이름 기반 ignore는 없다.

**PathMeta vs 라벨**

| | PathMeta | 라벨 |
| --- | --- | --- |
| 키 | 파일명 | 라벨 문자열 |
| 용도 | 이름 → 단일 path | 일괄 로드 |
| Refresh | 디스크 스캔 | Groups UI |

폴더에만 있고 Addressable·라벨이 없으면 path Lookup은 될 수 있으나 **라벨 preload는 실패**할 수 있다.

**파일 단위 이관** (자동 마이그레이터 없음): Resources → Addressables 이동 → Resources 삭제 → Addressable 등록 유지 → **레이아웃 도구** → 갱신 → gameplay 파일명 키는 동일, preload·라벨만 재확인.

## 변경 가이드 (Change Guidelines)

| 변경 | 경로 |
| --- | --- |
| 스캔·중복·ignore | `PathMetaWriter.cs` |
| PathMeta 갱신 | `PathMetaRefresh.cs`, `AddressableLayoutToolWindow.cs` |
| JSON 형식 | `StringKeyedMap.cs` |

- [ ] `Assets/Addressables/` (복수)만 스캔 — 단수 `Addressable/` 금지
- [ ] README 불변조건 동기화
- [ ] 레이아웃 도구 갱신 후 Lookup spot-check

## 관련 문서 (See also)

- [Architecture_AddressableLayout](Architecture_AddressableLayout.md)
- [Architecture_BootFlow](Architecture_BootFlow.md)
- [Architecture_RuntimeEditor](Architecture_RuntimeEditor.md)
- [루트 README](../../README.md)
