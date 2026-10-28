# 01: ADR·CONTEXT — load group과 label ref-count

**What to build:** Resource layer 용어(PathMeta, hit-only sync, type label vs place label, load group, label owner)가 repo `CONTEXT.md`에 정리되고, label load group을 CacheOwnership 위에 두고 label ref-count로 해제하는 결정이 ADR로 기록된다. 이후 구현 티켓이 같은 언어를 쓴다.

**Blocked by:** None (can start immediately)

**Status:** done

**GitHub:** [#2](https://github.com/monet5379/unity-addressable/issues/2) · **Parent:** [#1](https://github.com/monet5379/unity-addressable/issues/1) · [spec](../spec.md)

- [x] `CONTEXT.md`(repo 루트)에 패키지 도메인 용어가 glossary 형식으로 있다
- [x] ADR 1건: Dragon legacy label map 대신 CacheOwnership + label ref-count (되돌리기 비용·trade-off 명시)
- [x] Architecture README 또는 BootFlow/AddressableLayout에 ADR·CONTEXT 링크 1줄
