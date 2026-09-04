# Addressable Layout

**English** | [한국어](README.ko.md)

A Unity Addressables layout: **filename → path lookup**, **type + place labels**, load/cache with **label preload** and **sync hit-only**, optional **Resources ↔ Addressables dual scan**, and a thin spawn facade. Class shape follows a Treasure/Core-style Resource layer so a Resources-based title (e.g. Dragon) can migrate without rewriting typed game facades.

**Includes** (target Done — see [milestones](docs/milestones.md))

- **Runtime** — `PathManager` / PathMeta, `AddressableAssetManager`, `AddressableLabels`, `ResourcesManager` (load by name, label preload, sync cache-hit only; dual path; Spawn/Despawn without domain APIs)
- **Editor** (optional milestone) — Path Settings refresh, Edit Mode `*ForEditor` loads
- **Demo** (optional) — boot preload, place-label enter/leave, dual-scan Lookup playground (not a shipping catalog)

Copy only **`Assets/AddressableLayout`** into a game. `Assets/Demo` is reference-only.

**Milestones A–C + F (in repo):** Runtime is under `project/unity-addressable/Assets/AddressableLayout` (`ResourcesManager` + PathManager dual scan + Addressables). After opening that Unity project, run **Tools → Addressable Layout → Demo → Register Boot Sample** (boot + place + Resources/collision samples + PathMeta), then Play. Refresh alone: **Tools → Addressable Layout → Refresh Paths**.

## Install

Copy `unity-addressable/.../Assets/AddressableLayout` into your project `Assets/` (keep Runtime/Editor asmdefs if present).

Requires the **Addressables** package. This repo’s Unity project may live under `project/unity-addressable/` during development; the install unit is still **`Assets/AddressableLayout`**. **Demo is not part of the install.**

## Done (package goal)

Enough surface to baseline a **Resources → Addressables** migration for a Dragon-like title, with Treasure/Core-like types:

| Piece | Role |
|-------|------|
| `AddressableAssetManager` | Addressables load · Dictionary cache · release |
| `AddressableLabels` | Type labels (`boot`, `JSON`, `Scriptable`, …) + place labels (area names) |
| `PathManager` | Filename → address / Resources-relative path; PathMeta refresh |
| `ResourcesManager` | `Load` / `LoadAsync` / label preload · sync = cache hit only · Spawn/Despawn |

**Not in package:** typed game facades (`SpawnStage`, `SpawnMonster`, popup routing), Json/SO parse Facades, LeanPool (optional host), sheet sync.

Progress: [docs/milestones.md](docs/milestones.md).

## Invariants

- **Filename is a global key** — PathMeta warns on duplicates (`Duplicate filename in PathMeta; keeping first path` within one tree; `Resources wins` when Addressables collides with Resources); first registered path wins (Resources scanned first).
- **`Assets/Addressables/` only** — do not use a singular `Addressable/` content root.
- **Type label ≠ place label** — boot/UI/shared use type labels; area-scoped content uses place labels matching title area ids (not `area1`/`area2`).
- **Sync `Load` (Play) for Addressables paths = cache hit only** — miss returns null. Preload by label or use `LoadAsync`. Do not block the main thread with sync Addressables fetch on miss. **Resources leaf** paths (no `Assets/` prefix) use `Resources.Load` during dual-path migration.
- **`PathManager.Load()` before Lookup** — boot order: PathMeta → label preload → gameplay load/spawn.
- **Folder OK / label missing** — Edit/`*ForEditor` may succeed while Play label load fails if the asset is not labeled.
- **Resources and Addressables must not share the same filename** when dual scan is on — Resources wins (`Duplicate filename in PathMeta; Resources wins`).
- **Spawn facade ≠ domain facade** — package Spawn takes a prefab name; stage/character/UI routing stays title-owned.
- **Demo ≠ shipping catalog** — sample labels and assets are playground only.

### Dual-scan path shapes

| Source | PathMeta value | `IsAddressablePath` |
|--------|----------------|---------------------|
| `Assets/Addressables/...` | `Assets/...` address | `true` |
| `Assets/Resources/...` | Resources.Load leaf (no extension), e.g. `Demo/Foo` | `false` |

**One-file migrate:** move the asset from `Assets/Resources/` to `Assets/Addressables/`, delete the Resources copy, then **Refresh Paths** — Lookup becomes an `Assets/...` address. No bulk migrator in this package.

## Out of scope

- Gameplay, combat, stages, or typed `Spawn*` domain APIs
- Json/SO **parse · Find\* Facades** (balance / Scriptable registries)
- Google Sheets → JSON pipeline
- Scene transition **Release/Preload** policy beyond label enter/leave helpers
- Required **LeanPool** (host may pool; package default may Instantiate/Destroy)
- Remote CDN ops, content update UX, encryption
- Automatic bulk Resources → Addressables migrator
- `.meta` hand-editing

## Labels (type + place)

| Axis | Examples | When to load |
|------|----------|--------------|
| **Type** | `boot`, `JSON`, `Scriptable`, `ui`, `player` | App boot / pipeline |
| **Place** | `GallatinForest`, `Dawn`, … (title `AreaNames`) | Area enter; release on leave |

Shared enemies/VFX: prefer a shared type label (e.g. `enemy_shared` or `boot`), not a single place label only.

Groups (Local/Remote packing) are independent of labels. Use groups for download size; use labels for runtime filters.

## Layout (content)

Recommended title content root:

```text
Assets/Addressables/
  JSON/
  Scriptable/
  Prefabs/
  ...
Assets/Resources/                 # optional during dual-path migration
  Data/PathMetaData.json          # or package-agreed PathMeta location
```

Package code:

```text
Assets/AddressableLayout/
  Runtime/
  Editor/                         # Refresh Paths (B/F); Path Settings UI later (E)
Assets/Demo/                      # playground only — do not copy to ship
Assets/Resources/Data/
  PathMetaData.json               # generated by Refresh Paths
```

## Related

- [Milestones](docs/milestones.md) — build order A → Done
- [unity-studio-kit](https://github.com/monet5379/unity-studio-kit) — personal profile (README + Invariants)
- Sibling portfolio: [unity-save-layout](https://github.com/monet5379/unity-save-layout)

## License

[MIT](LICENSE)

English prose may be AI-assisted. If wording conflicts, prefer the [Korean README](README.ko.md) or the code.
