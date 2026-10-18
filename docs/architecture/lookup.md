# Lookup · PathMeta

**파일명(확장자 포함) → 로드에 쓸 문자열** 매핑이 PathMeta이다. 게임·툴은 경로 대신 **전역 파일명 키**를 쓰고, Resources → Addressables 이관은 Lookup 값만 바뀐다.

## 저장 위치

| 항목 | 값 |
| --- | --- |
| 디스크 | `Assets/Resources/Data/PathMetaData.json` |
| Runtime 로드 | `Resources.Load("Data/PathMetaData")` — 확장자 없는 leaf |
| 상수 | `PathManager.TargetPath` |

JSON 형식은 `StringKeyedMap`(keys/values 배열) → `Dictionary<string,string>`.

## 경로 두 종류

`PathManager.IsAddressablePath(path)`:

- `true` — `Assets/`로 시작 → Addressables 주소. Play sync는 **캐시 hit-only**.
- `false` — Resources.Load **leaf** (확장자 없음), 예: `Demo/Foo`

| 출처 폴더 | PathMeta 값 예 | `IsAddressablePath` |
| --- | --- | --- |
| `Assets/Addressables/.../Bar.prefab` | `Assets/Addressables/.../Bar.prefab` | true |
| `Assets/Resources/Demo/Foo.asset` | `Demo/Foo` | false |

**콘텐츠 루트:** 복수 `Assets/Addressables/`만 스캔한다. 단수 `Addressable/`는 금지 — [README](../../README.md).

## Refresh (Editor)

`PathMetaRefresh.Refresh()` → `PathManager.UpdatePathMetadata()` → `AssetDatabase.Refresh()`.

스캔 순서 (**이중 스캔, F 마일스톤**):

```text
1. Assets/Resources/**   (전 파일, 재귀)
2. Assets/Addressables/** (전 파일, 재귀)
```

각 파일은 **파일명(확장자 포함)** 을 dictionary 키로 등록한다.

### 중복 규칙

| 상황 | 동작 |
| --- | --- |
| 같은 트리 안에서 동일 파일명 두 번 | 첫 path 유지, 경고 `Duplicate filename in PathMeta; keeping first path` |
| Resources와 Addressables에 동일 파일명 | **Resources wins** — Addressables 쪽 무시, 경고 `Resources wins` |

이관 중 같은 이름이 양쪽에 남아 있으면 Addressables 항목은 Lookup에 안 잡히므로, **이동 후 Resources 쪽 삭제 + Refresh**가 필요하다.

### 제외

- 확장자: `.meta`, `.shader`, `.cginc`, `.text` (`IgnoreExtensionSet`)
- `PathMetaData.json` 자체는 Resources 스캔에서 제외
- `IgnoreDirectorySet` — 기본 비어 있음, 필요 시 코드에서 확장

## Lookup API

| API | 용도 |
| --- | --- |
| `Lookup("Foo.prefab")` | 일반 파일명 → path |
| `FindPrefabPath("Foo")` | `.prefab` 붙여 Lookup |
| `FindImagePath` / `FindMaterialPath` / … | 확장자별 헬퍼 |
| `Load()` | JSON → `_database` |
| `CheckLoaded()` | 메모리 + 디스크 파일 존재 |

miss 시 경고: Refresh Paths 안내.

## Spawn / Load와의 연결

```text
SpawnPrefab("DemoSpawnSample")
  → FindPrefabPath → Lookup("DemoSpawnSample.prefab")
  → LoadResource<GameObject>(path)
       ├─ Resources leaf → Resources.Load
       └─ Assets/...     → TryGetCached only
```

`LoadResourceByName<T>("DemoBootSample.asset")`도 동일하게 Lookup 후 `LoadResource` 규칙을 탄다.

## 파일 단위 이관 (자동 마이그레이터 없음)

1. `Assets/Resources/.../X` → `Assets/Addressables/.../X`로 이동
2. Resources 쪽 파일 삭제 (중복 파일명 방지)
3. Addressable 등록·라벨 유지
4. **Refresh Paths** → Lookup 값이 `Assets/...`로 바뀜
5. gameplay는 파일명 키 변경 없음 — preload·라벨만 재확인

## PathMeta vs Addressables 라벨

| | PathMeta | 라벨 |
| --- | --- | --- |
| 키 | 파일명 | 라벨 문자열 |
| 용도 | 이름 → 단일 path | 여러 에셋 일괄 로드 |
| Editor Refresh | 디스크 스캔 | Groups UI |
| 라벨 없음 | path Lookup 가능 | Play/Edit 라벨 로드 실패 또는 빈 목록 |

폴더에만 있고 Addressable·라벨이 없으면 `*ForEditor` path 로드는 될 수 있어도, **라벨 preload는 실패**할 수 있다 — [runtime-editor.md](runtime-editor.md).
