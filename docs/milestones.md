# Milestones

Build order for **Addressable Layout**. Contract truth stays in the root [README](../README.md) (Invariants). This file is only **what to implement next** — not a Treasure-style Plan tree.

**Done (package goal):** **C + F**, with **D** recommended. **E** optional.

Class target (Treasure/Core-shaped): `AddressableAssetManager` · `AddressableLabels` · `PathManager` · `ResourcesManager` (+ Editor pieces in E).

```text
A → B → C → F → D → (E)
         └─────────── Done = C+F (+D)   ← F done; D next
```

| ID | Name | Status |
|----|------|--------|
| A | Addressables wrapper + labels | done |
| B | Path lookup + PathMeta | done |
| C | Preload + sync hit-only facade | done |
| F | Resources ↔ Addressables dual scan | done |
| D | Spawn / Despawn facade | pending |
| E | Editor Path Settings + `*ForEditor` | pending |

---

## A — Addressables wrapper + labels

**In**

- `AddressableAssetManager` — async load, Dictionary cache, release
- `AddressableLabels` — type constants (`boot`, `JSON`, `Scriptable`, …); place constants or string helpers for Demo areas
- Demo: load / unload by key or label

**Out**

- PathMeta, Spawn, Resources dual path, Editor window

**Exit**

- [x] Play Mode Demo loads a labeled sample asset into cache and releases it
- [x] README Install path exists (`Assets/AddressableLayout/Runtime`)

**Verify (Unity)**

1. Open `project/unity-addressable`
2. **Tools → Addressable Layout → Demo → Register Boot Sample** (creates SO + `boot` label)
3. Enter Play — Console: loaded sample → `ReleaseAll done (milestone A)`

---

## B — Path lookup + PathMeta

**In**

- A
- `PathManager` — `Load` / `Lookup` / `FindPrefabPath` (and siblings as needed)
- PathMeta scan of `Assets/Addressables/` (and later Resources in F)
- Editor or menu refresh that rewrites PathMeta (minimal OK)

**Out**

- Label preload policy as a hard Play invariant (that is C)
- Spawn, dual path, full Path Settings UI (E)

**Exit**

- [x] Filename → Addressables address round-trip in Demo
- [x] Duplicate filename warning behavior documented / implemented

**Verify (Unity)**

1. **Tools → Addressable Layout → Demo → Register Boot Sample** (also refreshes PathMeta)
   — or **Tools → Addressable Layout → Refresh Paths**
2. Enter Play — Console: PathMeta round-trip OK `(milestone B)` after A’s ReleaseAll

---

## C — Preload + sync hit-only

**In**

- B
- `ResourcesManager` load API — `LoadResource` / `LoadResourceAsync` / `LoadResourcesByLabelAsync`
- Boot order: `PathManager.Load` → type (and optional place) label preload → sync load
- Invariant: Play sync load = **cache hit only**
- Demo: `boot` preload + enter/leave a **place** label (two sample areas)

**Out**

- Spawn/Despawn (D), Resources dual scan (F), Editor ForEditor (E)
- Typed domain APIs

**Exit**

- [x] Sync load before preload returns null; after preload returns asset
- [x] Place label load on “enter”, release on “leave” in Demo
- [x] README Invariants match behavior

**Verify (Unity)**

1. **Tools → Addressable Layout → Demo → Register Boot Sample** (boot + DemoAreaA/B)
2. Enter Play — Console: sync miss → boot preload → sync hit → place enter/leave `(milestone C)`

---

## F — Resources ↔ Addressables dual scan

**In**

- C (or B if dual scan is implemented before full preload UX — prefer after C)
- PathMeta scans both `Resources/` and `Assets/Addressables/`
- Same filename forbidden; **Resources wins**
- Demo or note: migrate one sample from Resources → Addressables

**Out**

- Automatic bulk migrator tool
- Domain facades

**Exit**

- [x] Lookup resolves Resources-only, Addressables-only, and documents collision rule
- [x] README dual-path invariant checked

**Verify (Unity)**

1. **Tools → Addressable Layout → Demo → Register Boot Sample** (also creates Resources-only + collision fixtures) — or **Refresh Paths**
2. Enter Play — Console: Resources-only leaf Lookup · collision Resources wins · Addressables-only still `(milestone B)` · then A/C as before
3. Refresh warning on collision: `Duplicate filename in PathMeta; Resources wins`

---

## D — Spawn / Despawn facade

**In**

- C (+ F if already done)
- `SpawnPrefab` / `Despawn` by prefab **name** (Path → load → Instantiate)
- Default: Instantiate/Destroy (LeanPool optional / title-owned)

**Out**

- `SpawnStage` / `SpawnMonster` / UI canvas routing
- Required LeanPool dependency

**Exit**

- [ ] Demo spawns and despawns a named prefab after preload
- [ ] Out of scope still excludes typed game facades

---

## E — Editor Path Settings + `*ForEditor` (optional)

**In**

- D (or C minimum)
- Path Settings window / menu — Refresh Paths
- `LoadResourceForEditor` / `LoadResourcesByLabelForEditor` (AssetDatabase; does **not** fill Play cache)
- Gotcha: folder present but label missing → Edit OK, Play label load may fail

**Out**

- Sheets sync, Scriptable Find\* registration

**Exit**

- [ ] Refresh updates PathMeta from disk
- [ ] Edit Mode load path documented vs Play hit-only

---

## After Done

- Keep README Invariants as SSOT; tick rows in the status table above
- Do not add Architecture / Plan trees unless the repo switches to a **game** profile
- Title typed facades and balance Facades stay in the game repo
