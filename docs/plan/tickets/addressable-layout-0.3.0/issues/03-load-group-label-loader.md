# 03: Load group (LabelLoader) sync·async end-to-end

**What to build:** `LoadRequest`, `LoadOperation`, `AddressableLabelLoader`로 groupId와 라벨 목록을 sync 또는 async 적재할 수 있다. Progress, Completed, 실패 라벨, `KeepResident`, `force` Release가 동작한다. `AddressableLabelLoader.Shared`는 `ResourcesManager.AssetManager`와 동일 캐시를 사용한다. sync 그룹은 Play에서 `RunSynchronously`로 완료될 수 있다. 두 groupId가 같은 라벨을 잡을 때 한쪽 release만으로 캐시가 남는다.

**Blocked by:** [02-single-label-preload-refcount](02-single-label-preload-refcount.md)

**Status:** ready-for-agent

**GitHub:** [#4](https://github.com/monet5379/unity-addressable/issues/4) · **Parent:** [#1](https://github.com/monet5379/unity-addressable/issues/1) · [spec](../spec.md)

- [ ] sync `LoadGroup`: 완료 후 해당 라벨 에셋 sync hit
- [ ] async `LoadGroup`: `CustomYieldInstruction` progress·완료
- [ ] `KeepResident`: non-force `Release(groupId)` 무시(경고)
- [ ] `force` Release: resident 그룹 해제
- [ ] Edit Mode: Demo 등록 전제(또는 동등 fixture) LabelLoader 시나리오 green
