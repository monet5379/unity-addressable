# Architecture: RuntimeEditor

## 개요 (Overview)

*Play Mode의 Addressables 캐시 hit-only sync와 Edit Mode의 AssetDatabase `*ForEditor` 파사드를 분리해, 에디터 검증이 Play preload 상태를 오염시키지 않게 한다.*

같은 PathManager Lookup 규칙을 쓰지만 Addressables 구현·캐시는 다르다. 계약 정본은 [루트 README](../../README.md)이다.

## 책임과 경계 (Responsibilities & Boundaries)

*두 파사드·Play 캐시·Editor PathMeta Refresh 진입점만 소유한다. Lookup 스캔 규칙은 [Architecture_Lookup](Architecture_Lookup.md)를 본다.*

### 포함 범위 (In Scope)

- `ResourcesManager` (Play) vs `ResourcesManagerForEditor` (Edit)
- `AddressableAssetManager` 캐시 키·alias · 릴리즈 API
- Editor: Refresh Paths · Path Settings · **Smoke ForEditor (Edit Mode)**

### 제외 범위 (Out of Scope)

- 부트·place preload 순서 → [Architecture_BootFlow](Architecture_BootFlow.md)
- 타이틀 커스텀 Editor 툴 asmdef 배치 (가이드만 제공)

## 주요 타입과 관계 (Key Types & Relationships)

| | `ResourcesManager` (Runtime) | `ResourcesManagerForEditor` (Editor) |
| --- | --- | --- |
| 실행 | Player / Play Mode | Edit Mode |
| Addressables 단일 | 캐시 hit-only sync · async는 Addressables API | `AssetDatabase.LoadAssetAtPath` |
| Resources leaf | `Resources.Load` | `Resources.Load` |
| 라벨 로드 | `LoadResourcesByLabelAsync` → **Play 캐시 채움** | `LoadResourcesByLabelForEditor` → settings 엔트리 + AssetDatabase |
| Play 캐시 | static `AddressableAssetManager` | **채우지 않음** |

**Play 캐시** — 키는 주로 `IResourceLocation.PrimaryKey`; PathMeta `Assets/...` 주소는 **alias**로 동일 객체를 가리킨다. `Release` / `ReleaseLabel` / `ReleaseAll`은 handle과 dictionary를 함께 정리한다.

**asmdef** — 게임은 `AddressableLayout.Runtime`만 참조. 커스텀 Editor 툴은 `AddressableLayout.Editor` + `ResourcesManagerForEditor`.

## 흐름 (Flow)

*Edit vs Play에서 같은 path 문자열이 다른 API로 해석된다.*

### 런타임 흐름 (Runtime Flow)

1. 라벨 preload (`LoadResourcesByLabelAsync`) — locations로 캐시·라벨→키 갱신
2. `LoadResource` / `SpawnPrefab` — Addressables path는 `TryGetCached`만 (miss → null)
3. Resources leaf — `Resources.Load` sync

### 에디터 흐름 (Editor Flow)

**`LoadResourceForEditor<T>(pathOrAddress)`**

```text
pathOrAddress
  ├─ IsAddressablePath == false → Resources.Load<T>(leaf)
  └─ true → AssetDatabase.LoadAssetAtPath<T>(Assets/...)
```

동기 Addressables API를 부르지 않으며 Play 캐시를 건드리지 않는다.

**`LoadResourcesByLabelForEditor<T>(label)`** — `AddressableAssetSettingsDefaultObject.Settings` group entry에서 label 필터 후 AssetDatabase 로드. entry·라벨 없으면 **빈 목록**.

**PathMeta 갱신**

| 진입점 | 동작 |
| --- | --- |
| **Tools → Addressable Layout → Refresh Paths** | `PathMetaRefresh.Refresh()` |
| **Path Settings** | Refresh + 엔트리 수 |
| **Smoke ForEditor (Edit Mode)** | `*ForEditor` 스모크 |

`UpdatePathMetadata()`는 Editor 디스크 스캔이다. Player 빌드는 packaged JSON만 `Load()`한다.

## 불변조건과 주의점 (Invariants & Gotchas)

- **Edit `*ForEditor` ≠ Play sync** — Edit 성공이 preload 없는 Play sync hit를 보장하지 않는다
- Edit Mode 스모크 후 Play를 바로 돌려도 preload 상태가 Edit API로 오염되지 않아야 한다
- Play Addressables sync miss는 의도 — [Architecture_BootFlow](Architecture_BootFlow.md) preload 참고

| 작업 | API |
| --- | --- |
| 게임 Play 로드·스폰 | `ResourcesManager` |
| Edit 툴·프리뷰 | `ResourcesManagerForEditor` |
| Lookup JSON 갱신 | Editor Refresh (Play에서 하지 않음) |
| Play sync hit 검증 | Play Mode + preload 후 `LoadResource` |

**Verify** — Play: Sample Demo `DemoAddressableHost` (**Register Boot Sample**). Edit: **Smoke ForEditor (Edit Mode)**.

## 변경 가이드 (Change Guidelines)

| 변경 | 경로 |
| --- | --- |
| Play 캐시·alias | `AddressableAssetManager.cs` |
| Play 파사드 | `ResourcesManager.cs` |
| Edit 파사드·스모크 | `ResourcesManagerForEditor.cs`, `ForEditorSmokeMenu.cs` |
| Path Settings UI | `PathSettingsWindow.cs` |

- [ ] README “Edit ≠ Play sync” 유지
- [ ] Smoke ForEditor · Demo Play

## 관련 문서 (See also)

- [Architecture_AddressableLayout](Architecture_AddressableLayout.md)
- [Architecture_BootFlow](Architecture_BootFlow.md)
- [Architecture_Lookup](Architecture_Lookup.md)
- [루트 README](../../README.md)
