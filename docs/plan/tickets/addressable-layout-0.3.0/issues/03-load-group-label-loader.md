# 03: Load group (LabelLoader) sync·async end-to-end

**What to build:** `LoadRequest`, `LoadOperation`, `AddressableLabelLoader`로 groupId와 라벨 목록을 sync 또는 async 적재할 수 있다. Progress, Completed, 실패 라벨, `KeepResident`, `force` Release가 동작한다. `AddressableLabelLoader.Shared`는 `ResourcesManager.AssetManager`와 동일 캐시를 사용한다. sync 그룹은 Play에서 `RunSynchronously`로 완료될 수 있다. 두 groupId가 같은 라벨을 잡을 때 한쪽 release만으로 캐시가 남는다. `LoadRequest.AssetType`으로 `(label, typeof(T))` handle을 단일 preload·`PreloadLabelSync<T>`와 맞출 수 있다.

**Blocked by:** [02-single-label-preload-refcount](02-single-label-preload-refcount.md)

**Status:** done

**GitHub:** [#4](https://github.com/monet5379/unity-addressable/issues/4) · **Parent:** [#1](https://github.com/monet5379/unity-addressable/issues/1) · [spec](../spec.md)

- [x] sync `LoadGroup`: 완료 후 해당 라벨 에셋 sync hit — Edit `AddressableLabelLoaderEditTests.SyncLoadGroup_ThenSyncLoad_HitsCache` (`AssetType = typeof(DemoBootSample)`)
- [x] async `LoadGroup`: `CustomYieldInstruction` progress·완료 — Play `LabelLoaderPlayModeTests.AsyncLoadGroup_CompletesWithProgress`
- [x] `KeepResident`: non-force `Release(groupId)` 무시(경고) — Edit `KeepResident_NonForceReleaseIgnored_ForceEvicts`
- [x] `force` Release: resident 그룹 해제 —同上
- [x] Edit Mode: Demo 등록 전제 LabelLoader 시나리오 green — `AddressableLabelLoaderEditTests`(5) · `scripts/verify.ps1` **PrepareDemo → Edit 15/15** (2026-10-28 CLI)

**추가 검증 (review follow-up):**

- [x] `LoadRequest.AssetType` + 단일 preload handle 공유 — Edit `SyncLoadGroup_ThenPreloadLabelSync_SharesLabelHandle` · [Architecture_BootFlow](../../../../architecture/Architecture_BootFlow.md) · [CONTEXT](../../../../../CONTEXT.md)
- [x] async 적재 중 `Release` — Play `AsyncLoadGroup_ReleaseWhileInflight_DropsAcquiredLabels`
- [x] 공유 라벨 partial group release — Edit `TwoGroupsShareLabel_OneRelease_KeepsCache` · Shared 캐시 — Edit `SharedLoader_UsesResourcesManagerAssetManager`
