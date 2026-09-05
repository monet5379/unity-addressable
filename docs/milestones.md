# 마일스톤

**Addressable Layout** 제작 순서예요. 계약(불변조건) 정본은 루트 [README](../README.md)에 두고, 이 파일은 **다음에 무엇을 만들지**만 적어요. Treasure식 Plan 트리가 아니에요.

**Done (패키지 목표):** **C + F + D**. **E**는 선택(Editor 편의) — 이 repo에서는 완료예요.

클래스 목표 (Treasure/Core형): `AddressableAssetManager` · `AddressableLabels` · `PathManager` · `ResourcesManager` (+ Editor Path Settings / `*ForEditor`).

```text
A → B → C → F → D → (E)
         └─────────── Done = C+F+D   ← E optional Editor도 완료
```

| ID | 이름 | 상태 |
|----|------|------|
| A | Addressables 래퍼 + 라벨 | done |
| B | Path Lookup + PathMeta | done |
| C | Preload + 동기 hit-only 파사드 | done |
| F | Resources ↔ Addressables 이중 스캔 | done |
| D | Spawn / Despawn 파사드 | done |
| E | Editor Path Settings + `*ForEditor` | done |

---

## A — Addressables 래퍼 + 라벨

**In**

- `AddressableAssetManager` — 비동기 로드, Dictionary 캐시, 릴리즈
- `AddressableLabels` — 종류 상수(`boot`, `JSON`, `Scriptable`, …); Demo 지역용 place 상수·문자열 헬퍼
- Demo: 키 또는 라벨로 로드 / 언로드

**Out**

- PathMeta, Spawn, Resources 이중 경로, Editor 윈도우

**Exit**

- [x] Play Mode Demo가 라벨 샘플 에셋을 캐시에 넣고 릴리즈함
- [x] README 설치 경로가 있음 (`Assets/AddressableLayout/Runtime`)

**Verify (Unity)**

1. `project/unity-addressable` 열기
2. **Tools → Addressable Layout → Demo → Register Boot Sample** (SO + `boot` 라벨 생성)
3. Play — Console: 샘플 로드 → `ReleaseAll done (milestone A)`

---

## B — Path Lookup + PathMeta

**In**

- A
- `PathManager` — `Load` / `Lookup` / `FindPrefabPath` (필요 시 형제 API)
- `Assets/Addressables/` PathMeta 스캔 (Resources는 F에서)
- PathMeta를 다시 쓰는 Editor·메뉴 Refresh (최소면 OK)

**Out**

- 라벨 preload를 Play 하드 불변조건으로 고정 (그건 C)
- Spawn, 이중 경로, Path Settings UI 전체 (E)

**Exit**

- [x] Demo에서 파일명 → Addressables 주소 round-trip
- [x] 중복 파일명 경고 동작이 문서화·구현됨

**Verify (Unity)**

1. **Tools → Addressable Layout → Demo → Register Boot Sample** (PathMeta도 갱신)
   — 또는 **Tools → Addressable Layout → Refresh Paths**
2. Play — Console: A의 ReleaseAll 뒤 PathMeta round-trip OK `(milestone B)`

---

## C — Preload + 동기 hit-only

**In**

- B
- `ResourcesManager` 로드 API — `LoadResource` / `LoadResourceAsync` / `LoadResourcesByLabelAsync`
- 부트 순서: `PathManager.Load` → 종류(및 선택 place) 라벨 preload → 동기 로드
- 불변조건: Play 동기 로드 = **캐시 hit만**
- Demo: `boot` preload + **place** 라벨 입장/퇴장 (샘플 지역 둘)

**Out**

- Spawn/Despawn (D), Resources 이중 스캔 (F), Editor ForEditor (E)
- typed 도메인 API

**Exit**

- [x] preload 전 동기 로드는 null, preload 후면 에셋
- [x] Demo에서 place “enter” 로드, “leave” 릴리즈
- [x] README 불변조건과 동작이 맞음

**Verify (Unity)**

1. **Tools → Addressable Layout → Demo → Register Boot Sample** (boot + DemoAreaA/B)
2. Play — Console: sync miss → boot preload → sync hit → place enter/leave `(milestone C)`

---

## F — Resources ↔ Addressables 이중 스캔

**In**

- C (이중 스캔을 preload UX 전에 넣어도 되나, C 이후를 권장)
- PathMeta가 `Resources/`와 `Assets/Addressables/`를 모두 스캔
- 동일 파일명 금지; **Resources 우선**
- Demo 또는 메모: Resources → Addressables로 샘플 하나 이관

**Out**

- 자동 일괄 마이그레이터 도구
- 도메인 파사드

**Exit**

- [x] Lookup이 Resources-only · Addressables-only를 해결하고, 충돌 규칙을 문서화함
- [x] README 이중 경로 불변조건 확인

**Verify (Unity)**

1. **Tools → Addressable Layout → Demo → Register Boot Sample** (Resources-only + 충돌 fixture도 생성) — 또는 **Refresh Paths**
2. Play — Console: Resources-only leaf Lookup · 충돌 Resources wins · Addressables-only는 `(milestone B)` · 이어 A/C
3. Refresh 충돌 경고: `Duplicate filename in PathMeta; Resources wins`

---

## D — Spawn / Despawn 파사드

**In**

- C (+ 이미 했다면 F)
- 프리팹 **이름**으로 `SpawnPrefab` / `Despawn` (Path → 로드 → Instantiate)
- 기본: Instantiate/Destroy (LeanPool은 선택·타이틀 소유)

**Out**

- `SpawnStage` / `SpawnMonster` / UI 캔버스 라우팅
- LeanPool 필수 의존성

**Exit**

- [x] Demo가 preload 후 named 프리팹을 spawn/despawn
- [x] Out of scope에 typed 게임 파사드가 계속 제외됨

**Verify (Unity)**

1. **Tools → Addressable Layout → Demo → Register Boot Sample** (boot SO + spawn 프리팹 + place + F fixture)
2. Play — Console: C preload 뒤, place enter/leave 전에 SpawnPrefab / Despawn `(milestone D)`
3. README Out of scope에 typed `Spawn*` 도메인 API가 계속 없음

---

## E — Editor Path Settings + `*ForEditor` (선택)

**In**

- D (또는 최소 C)
- Path Settings 윈도우 / 메뉴 — Refresh Paths
- `LoadResourceForEditor` / `LoadResourcesByLabelForEditor` (AssetDatabase; Play 캐시를 **채우지 않음**)
- Gotcha: 폴더는 있는데 라벨이 없으면 Edit는 OK일 수 있고, Play 라벨 로드는 실패할 수 있음

**Out**

- Sheets sync, Scriptable Find\* registration

**Exit**

- [x] Refresh가 디스크에서 PathMeta를 갱신함
- [x] Edit Mode 로드 경로가 Play hit-only와 구분되어 문서화됨

**Verify (Unity)**

1. **Tools → Addressable Layout → Path Settings** → **Refresh Paths** — Console / 창: PathMeta entries 갱신
2. (선택) **Tools → Addressable Layout → Refresh Paths** — 동일 공유 Refresh
3. **Tools → Addressable Layout → Smoke ForEditor (Edit Mode)** — Play 캐시를 채우지 않는 path / label 로드
4. README: Edit/`*ForEditor` vs Play 동기 hit-only · 폴더 OK / 라벨 없음 gotcha

---

## Done 이후

- README 불변조건을 SSOT로 유지하고, 위 상태표 행을 맞춰 두세요
- repo가 **game** 프로필로 바뀌기 전에는 Architecture / Plan 트리를 추가하지 마세요
- 타이틀 typed 파사드·밸런스 Facade는 게임 repo에 두세요
