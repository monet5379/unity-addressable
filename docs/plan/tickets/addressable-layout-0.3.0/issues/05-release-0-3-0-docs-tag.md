# 05: 0.3.0 릴리스 문서·semver·host parity

**What to build:** `com.monet5379.addressable-layout` 0.3.0을 문서·버전·tag로 ship 가능하게 한다. README·Architecture·CHANGELOG가 sync boot vs async Demo·load group을 설명하고, Dragon 호스트가 기대하는 공개 API 표면이 spec 또는 README에 prose로 정리된다. Git tag `v0.3.0`으로 pin 가능.

**Blocked by:** [04-verify-demo-regression](04-verify-demo-regression.md)

**Status:** done (tag·push는 커밋 후)

**GitHub:** [#6](https://github.com/monet5379/unity-addressable/issues/6) · **Parent:** [#1](https://github.com/monet5379/unity-addressable/issues/1) · [spec](../spec.md)

- [x] `package.json` version 0.3.0 · CHANGELOG [0.3.0] 항목
- [x] README 불변조건 + sync boot·load group API 요약
- [x] Architecture(BootFlow·AddressableLayout) 0.3.0 동기화
- [x] Host parity: LabelLoader·PreloadTypeLabels·ReleaseLabelRef alias 등 공개 API 목록(경로 덤프 없음)
- [ ] git tag `v0.3.0` (릴리스 커밋 후 · push는 별도)
- [x] Parent [#1](https://github.com/monet5379/unity-addressable/issues/1)에 완료 요약 댓글 (2026-10-28)

**Verify (docs ship gate):** `scripts/verify.ps1` — PrepareDemo · Edit **15/15** · Play **6/6** (2026-10-28 batchmode · Editor QA 동일)
