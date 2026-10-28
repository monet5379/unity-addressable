# 02: 단일 라벨 sync/async preload와 label ref-count release

**What to build:** 호스트가 한 Addressables 라벨을 sync(`PreloadLabelSync`, `ResourcesManager.PreloadTypeLabels`) 또는 async로 적재할 수 있고, 동일 라벨에 대한 acquire/release가 ref-count로 집계되어 0일 때만 label handle과 CacheOwnership owner가 해제된다. `ReleaseLabel`이 정본 API이고 `ReleaseLabelRef`는 obsolete bool alias다. `CountLabelLocationsAsync`, `CopyLoadedLabels`로 카운트·디버그 목록이 가능하다. hit-only sync `LoadResource`와 기존 Direct/label handle 규칙은 유지된다.

**Blocked by:** [01-adr-context-label-loader](01-adr-context-label-loader.md)

**Status:** done

**GitHub:** [#3](https://github.com/monet5379/unity-addressable/issues/3) · **Parent:** [#1](https://github.com/monet5379/unity-addressable/issues/1) · [spec](../spec.md)

- [x] sync preload 후 Addressables path에 대해 sync `LoadResource` hit — Play `LabelRefCountPlayModeTests.SyncPreload_ThenSyncLoad_HitsCache`
- [x] preload 전 sync load는 miss(null) (hit-only 불변) —同上 · Demo Milestone A
- [x] 같은 라벨 2회 acquire 후 1회 release 시 캐시 유지, ref 0에서만 evict — Play `TwoAcquires_OneRelease_StillHitsCache` · `SyncPreloadWhileAsyncInflight_OneAcquirePerCall`
- [x] `CacheOwnershipTests`(및 필요 시 추가 Edit 테스트) green — `scripts/verify.ps1` Edit 10/10 (`CacheOwnershipTests` + `AddressableAssetManagerRefCountTests`)
- [x] Demo PlayMode — [04](04-verify-demo-regression.md) 회귀 게이트 · `RunAllMilestones_WhenDemoRegistered_Succeeds` (2026-10-28 Editor · verify Play 6/6)
