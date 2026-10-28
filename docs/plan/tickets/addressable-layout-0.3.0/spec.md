# Spec: Addressable Layout 0.3.0 (Dragon host parity)

Status: ready-for-agent

GitHub: [#1](https://github.com/monet5379/unity-addressable/issues/1)

## Implementation tickets

| # | Local | GitHub | Blocked by |
|---|--------|--------|------------|
| 01 | [issues/01-adr-context-label-loader.md](issues/01-adr-context-label-loader.md) | [#2](https://github.com/monet5379/unity-addressable/issues/2) | — |
| 02 | [issues/02-single-label-preload-refcount.md](issues/02-single-label-preload-refcount.md) | [#3](https://github.com/monet5379/unity-addressable/issues/3) | #2 |
| 03 | [issues/03-load-group-label-loader.md](issues/03-load-group-label-loader.md) | [#4](https://github.com/monet5379/unity-addressable/issues/4) | #3 |
| 04 | [issues/04-verify-demo-regression.md](issues/04-verify-demo-regression.md) | [#5](https://github.com/monet5379/unity-addressable/issues/5) | #3, #4 |
| 05 | [issues/05-release-0-3-0-docs-tag.md](issues/05-release-0-3-0-docs-tag.md) | [#6](https://github.com/monet5379/unity-addressable/issues/6) | #5 |

Frontier: **#2** (unblocked).

## Problem Statement

`com.monet5379.addressable-layout` v0.2.0은 PathMeta Lookup, CacheOwnership 기반 캐시, async 라벨 preload, Demo 검증은 갖추지만, 실제 타이틀(Project Dragon)이 임베드 Layout에서 쓰는 **라벨 load group**, **동기 boot preload**, **라벨 ref-count 해제**, **적재 진행률·카운트** API가 없다. 그 결과 Dragon은 패키지 UPM을 소비하지 못하고 `Assets/AddressableLayout` fork를 유지한다.

호스트는 Resource layer만 UPM으로 받고, 게임 앱·씬·typed spawn은 타이틀에 두기로 했다. **이번 cut은 unity-addressable repo만** 수정하며, Dragon manifest 전환·임베드 제거는 별 work item이다.

## Solution

0.3.0 minor에서 Dragon 임베드와 **공개 API·캐시 의미**를 맞춘다. 내부 구현은 v0.2.0의 CacheOwnership·Direct/label handle 모델 위에 Dragon의 LabelLoader·sync preload를 **통합**한다(legacy `_labelToCacheKeys` 이식이 아님). Demo Sample과 `verify.ps1` 파이프라인은 **유지**하고, async boot 마일스톤 회귀를 막는다. README·Architecture·ADR·CONTEXT 용어를 갱신하고 git tag `v0.3.0`으로 pin 가능하게 한다.

## User Stories

1. As a **host integrator**, I want **Load group by id and label list (sync or async)**, so that boot and place preload can share one mechanical API without a game app in the package.
2. As a **host integrator**, I want **LoadOperation as CustomYieldInstruction with progress and completion**, so that loading UI can await the same object the loader returns.
3. As a **host integrator**, I want **KeepResident groups to ignore non-forced Release**, so that boot assets stay cached for the session.
4. As a **host integrator**, I want **forced Release on resident groups**, so that retry/fallback paths can tear down boot state.
5. As a **host integrator**, I want **PreloadTypeLabels with optional fail-on-empty in dev**, so that boot can block until type labels are cached (sync hit-only gameplay).
6. As a **host integrator**, I want **PreloadLabelSync and PreloadLabelAsync on the asset manager facade**, so that single-label and boot-pipeline loaders can choose mode.
7. As a **host integrator**, I want **CountLabelLocationsAsync before label load**, so that loading screens can show asset counts per label.
8. As a **host integrator**, I want **label release with reference counting when multiple groups acquire the same label**, so that one group release does not evict cache still needed by another.
9. As a **host integrator**, I want **ReleaseLabel as the canonical release API**, so that documentation and new code have one name.
10. As a **host integrator**, I want **ReleaseLabelRef as an obsolete alias returning bool**, so that existing host code compiles for one release while renaming.
11. As a **host integrator**, I want **CopyLoadedLabels for debug overlays**, so that developer tools can list loaded labels without reaching into private maps.
12. As a **host integrator**, I want **AddressableLabelLoader.Shared wired to ResourcesManager.AssetManager**, so that a single cache instance is used everywhere.
13. As a **host integrator**, I want **RunSynchronously for sync group mode in Play**, so that boot can complete before first gameplay frame when configured.
14. As a **package maintainer**, I want **AddressableLabels to stay Demo and generic type constants only**, so that ship-specific place/enemy labels never ship in UPM.
15. As a **package maintainer**, I want **Demo PlayMode tests to keep validating async boot and place enter/leave**, so that minimal consumers still have a green path without sync boot.
16. As a **package maintainer**, I want **ForEditor smoke to target Demo assets only**, so that Editor validation does not depend on a host catalog.
17. As a **package maintainer**, I want **CacheOwnershipTests extended for label ref and shared PrimaryKey across label owners**, so that cache regressions fail in Edit Mode without a host project.
18. As a **package maintainer**, I want **CHANGELOG and Architecture docs aligned with 0.3.0**, so that future readers understand sync vs async boot and load groups.
19. As a **package maintainer**, I want **verify.ps1 green after changes**, so that CI/batchmode reproduces Demo + Edit tests.
20. As a **host integrator (future Dragon ticket)**, I want **documented public surface matching TSResourcesManager expectations**, so that consuming Dragon can drop embedded Layout with minimal API edits.
21. As a **host integrator**, I want **hit-only sync LoadResource unchanged**, so that gameplay contracts from README remain true after 0.3.0.
22. As a **host integrator**, I want **IPrefabInstanceProvider unchanged**, so that pooling registration in the host still applies to SpawnPrefab.
23. As a **host integrator**, I want **EnterPlaceAsync / LeavePlace unchanged**, so that place preload demos and simple hosts keep working.
24. As a **package maintainer**, I want **no cohort migrate or SliceSmoke menus in UPM**, so that Dragon Slice tooling stays in the title repo.
25. As a **package maintainer**, I want **an ADR for label load group on CacheOwnership**, so that the ref-count vs ReleaseOwner trade-off is recorded.

## Implementation Decisions

- **Scope boundary**: Only `com.monet5379.addressable-layout` and its tests, Demo sample, package docs, and repo-level plan/ADR. No Dragon repo changes in this cut.
- **Versioning**: semver **0.3.0** minor; breaking cache behavior avoided; obsolete alias for one release instead of silent rename.
- **Public API additions**: `AddressableLabelLoader`, `LoadRequest`, `LoadOperation`, `LabelLoadMode` remain **public**; hosts may wrap them (e.g. TSAssetLoadOperation).
- **Sync boot**: `ResourcesManager.PreloadTypeLabels` calls into asset manager **PreloadLabelSync** (WaitForCompletion semantics); Demo verification **does not** switch to sync boot.
- **Release semantics**: Implement **label reference counting** at the asset manager layer; when count reaches zero, release label handles and `CacheOwnership.ReleaseOwner(label)`. `ReleaseLabel` is void or bool per existing facade consistency; `ReleaseLabelRef` obsolete wrapper returns whether eviction occurred.
- **Load group runner**: Async groups use a small runner component or equivalent already used in Dragon port; sync groups use **RunSynchronously** on the asset manager.
- **Loaded labels debug**: `CopyLoadedLabels` exposes label, ref count, asset count tuples for tooling.
- **AddressableLabels**: Keep `boot`, Demo place constants, and documented generic type names only; no title Area/Enemy enums.
- **Editor**: No new Dragon-only migrate menus; layout tool and PathMeta refresh unchanged in responsibility.
- **ADR**: Record decision to merge Dragon label loader onto CacheOwnership rather than retaining parallel `_labelToCacheKeys` maps.

## Testing Decisions

**What makes a good test here**: Assert **observable behavior** at package seams—cache hit/miss after preload/release, group progress completion, resident group ignore release, shared label survives partial group release—not private dictionary shapes.

**Primary seam (prefer one integration path)**:

- **`scripts/verify.ps1`** end-to-end: Edit Mode package tests, Demo PrepareDemo, Play Mode `DemoPlayModeTests`. This is the **highest** acceptance seam for 0.3.0 ship.

**Supporting seams (Edit Mode, no host catalog)**:

- **`CacheOwnershipTests`**: extend for multi-owner labels and release idempotency (prior art in same asmdef).
- **New or extended Edit tests** for `AddressableLabelLoader`: sync group, `KeepResident`, `force` release, label ref-count when two group ids share a label—using **Demo-registered labels** or minimal Addressables settings in the sample Unity project (same prior art as `DemoPlayModeTests` prerequisites).

**Explicitly not in 0.3.0 gate**:

- Dragon Category `AddressablesEdit` or PathMeta ~4k keys (host consumer ticket).

**Seam expectation (grill consensus)**: User accepted verifying 0.3.0 with **package tests + verify.ps1 + Demo Play** only; Dragon EditMode suite validates after consume ticket.

## Out of Scope

- Dragon `Packages/manifest.json`, removing `Assets/AddressableLayout`, cohort migrate menus, SliceSmoke, ship `AddressableLabels`.
- GameApp, GameBootPipeline, GameLoadingScene, GameLoadingPreloadOrchestrator, typed `ResourcesManager` spawn facade.
- Remote catalog, content update automation, new Demo milestones beyond regression.
- Second UPM for app/scene flow; meta-package for multi-UPM install.
- Mandatory LeanPool provider in package (host registers `IPrefabInstanceProvider`).

## Further Notes

- Conversation grill **Q1–Q6** accepted recommended answers (0.3.0, public LabelLoader, sync boot API, ReleaseLabel + obsolete ReleaseLabelRef, package-only test gate, Demo-only labels).
- Dragon consumer should pin `com.monet5379.addressable-layout#v0.3.0` after tag; parity checklist is the TSResourcesManager / LabelLoader public surface agreed in planning chat.
- Resource layer ubiquitous language: PathMeta, hit-only sync, type label vs place label, load group, label owner—capture in repo `CONTEXT.md` when implementing.
