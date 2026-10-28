# Architecture: RuntimeEditor

## 개요 (Overview)

*Play Mode의 Addressables 캐시 hit-only sync와 Edit Mode의 AssetDatabase `*ForEditor` 파사드를 분리해, 에디터 검증이 Play preload 상태를 오염시키지 않게 한다.*

같은 PathManager Lookup 규칙을 쓰지만 Addressables 구현·캐시는 다르다. 계약 정본은 [루트 README](../../README.md)이다.

## 책임과 경계 (Responsibilities & Boundaries)

*두 파사드·Play 캐시·Editor PathMeta Refresh 진입점만 소유한다. Lookup 스캔 규칙은 [Architecture_Lookup](Architecture_Lookup.md)를 본다.*

### 포함 범위 (In Scope)

- `ResourcesManager` (Play) vs `ResourcesManagerForEditor` (Edit)
- `AddressableAssetManager` · `CacheOwnership` — owner-set 캐시·alias · Direct/라벨 handle · 릴리즈 API
- Editor: **레이아웃 도구** 창 (PathMeta 갱신) · Demo **부트 샘플 등록** 메뉴

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

**Play 캐시** — 객체·소유자는 `CacheOwnership`(PrimaryKey + alias + owner-set). Addressables handle은 `AddressableAssetManager`가 `_directHandles`(Direct owner)와 `_labelHandles`(`(label, typeof(T))`)로 분리한다. `LoadResourceAsync`는 PrimaryKey별 `_loadingTasks`로 in-flight load를 합치고, Direct handle은 PrimaryKey당 하나만 유지한다. `LoadResourcesByLabelAsync`는 동일 `(label, Type)` handle을 재사용한다. `Release`는 Direct owner와 `_directHandles`만, `ReleaseLabel`은 해당 라벨의 `_labelHandles`와 owner만 제거한다(공유 PrimaryKey는 다른 owner가 남으면 sync hit 유지). `ReleaseAll`은 handle 전부 해제·캐시 비우기·`_generation` 증가로 stale async 완료가 캐시에 재진입하지 않게 한다. 상세 맵 표는 [Architecture_AddressableLayout](Architecture_AddressableLayout.md)를 본다.

**asmdef** — 게임은 `AddressableLayout.Runtime`만 참조. 커스텀 Editor 툴은 `AddressableLayout.Editor` + `ResourcesManagerForEditor`.

## 흐름 (Flow)

*Edit vs Play에서 같은 path 문자열이 다른 API로 해석된다.*

### 런타임 흐름 (Runtime Flow)

1. 라벨 preload (`LoadResourcesByLabelAsync`) — locations 순서로 캐시·alias·라벨 owner 등록
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
| **Tools → Addressable Layout → 레이아웃 도구** | PathMeta 갱신 |
| **Tools → Addressable Layout → Demo → 부트 샘플 등록** | Demo Sample 등록 |

`PathMetaWriter.Write()`는 Edit Mode 전용 디스크 스캔·`PathMetaData.json` 쓰기이다. 갱신 직후 **`PathManager.ApplyBuiltDatabase`** 로 Editor/Play Lookup 메모리를 동기화한다. **`Application.isPlaying`이면 파일·메모리를 건드리지 않는다.** Player 빌드는 packaged JSON만 `PathManager.Load()`한다. (0.5.0: `PathManager.UpdatePathMetadata()` 제거 — 호스트·툴은 Editor `PathMetaWriter.Write()`만 호출.)

## 불변조건과 주의점 (Invariants & Gotchas)

- **Edit `*ForEditor` ≠ Play sync** — Edit 성공이 preload 없는 Play sync hit를 보장하지 않는다
- Edit `*ForEditor` 호출은 Play Addressables 캐시를 채우지 않는다
- Play Addressables sync miss는 의도 — [Architecture_BootFlow](Architecture_BootFlow.md) preload 참고

| 작업 | API |
| --- | --- |
| 게임 Play 로드·스폰 | `ResourcesManager` |
| Edit 툴·프리뷰 | `ResourcesManagerForEditor` |
| Lookup JSON 갱신 | Editor Refresh (Play에서 하지 않음) |
| Play sync hit 검증 | Play Mode + preload 후 `LoadResource` |

**Verify** — batchmode: repo `scripts/verify.ps1` (PrepareDemo → Edit **25** → Play **6**). Test Runner: Edit `AddressableLayout.EditorTests` · Play `AddressableLayout.PlayModeTests` (Demo import · **부트 샘플 등록** 전제). Demo async boot 회귀는 `DemoPlayModeTests` / `DemoPlayModeVerification`.

## 변경 가이드 (Change Guidelines)

| 변경 | 경로 |
| --- | --- |
| Play 캐시·alias | `AddressableAssetManager.cs` |
| Play 파사드 | `ResourcesManager.cs` |
| Edit 파사드 | `ResourcesManagerForEditor.cs`, `AddressableLayoutToolWindow.cs` |
| 레이아웃 도구 UI | `AddressableLayoutToolWindow.cs` |

- [x] README “Edit ≠ Play sync” 유지 (0.3.0)
- [x] `verify.ps1` · `DemoPlayModeTests` · `CacheOwnershipTests` (2026-10-28)

## 관련 문서 (See also)

- [Architecture_AddressableLayout](Architecture_AddressableLayout.md)
- [Architecture_BootFlow](Architecture_BootFlow.md)
- [Architecture_Lookup](Architecture_Lookup.md)
- [루트 README](../../README.md)
