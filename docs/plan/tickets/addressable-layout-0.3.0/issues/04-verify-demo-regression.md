# 04: 회귀 게이트 — Demo Play + verify.ps1

**What to build:** 패키지 소비자 최소 경로가 end-to-end로 green이다. async Demo boot·place enter/leave, Edit Mode 소유권·provider 테스트, batchmode verify 스크립트가 한 번에 통과한다.

**Blocked by:** [02-single-label-preload-refcount](02-single-label-preload-refcount.md), [03-load-group-label-loader](03-load-group-label-loader.md)

**Status:** done

**GitHub:** [#5](https://github.com/monet5379/unity-addressable/issues/5) · **Parent:** [#1](https://github.com/monet5379/unity-addressable/issues/1) · [spec](../spec.md)

- [x] `DemoPlayModeTests` green (Demo import·부트 샘플 등록 전제) — Unity Test Runner Play · `RunAllMilestones_WhenDemoRegistered_Succeeds`
- [x] `CacheOwnershipTests` · `PrefabInstanceProviderTests` green — Unity Test Runner Edit
- [x] repo 루트 `scripts/verify.ps1` 전 단계 green — PrepareDemo · Edit 15/15 · Play 6/6 (2026-10-28 CLI · Editor QA 동일 green)
