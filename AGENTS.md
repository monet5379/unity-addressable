# Addressable Layout — Agent Guide

```text
profile: personal
```

공개 UPM 패키지(`com.monet5379.addressable-layout`) repo다. Cursor는 **이 repo + unity-studio-kit** 멀티 루트로 연다 (`unity-addressable.code-workspace`).

| Kit | 경로 |
|-----|------|
| 공통 | `unity-studio-kit/docs/common/` · `.cursor/rules/common/` |
| 글쓰기 | `unity-studio-kit/docs/common/WritingGuide.md` |
| 프로필 | `unity-studio-kit/docs/personal/` · `.cursor/rules/personal/` |

## 프로젝트 개요

- **목적:** Unity Addressables용 Resource 층 UPM — Lookup, 라벨 preload, Play 캐시·Spawn 파사드
- **Git 루트:** 이 저장소 최상단
- **Unity:** `project/unity-addressable/` (에셋·Test Runner·Addressables 설정)

## 레이아웃

```text
unity-addressable/
├── AGENTS.md
├── README.md
├── scripts/verify.ps1
├── docs/architecture/
└── project/unity-addressable/
    ├── Packages/com.monet5379.addressable-layout/   ← 패키지 소스 (정본)
    └── Assets/                                      ← 샘플·Demo·Addressables 콘텐츠
```

## 포니테일 (Build)

- 구현·리팩터·버그 수정 등 **코드 변경** 작업은 채팅에서 **`/ponytail`** (또는 `/ponytail full`)로 켠다.
- 스킬 경로: `.cursor/skills/ponytail/` — [DietrichGebert/ponytail](https://github.com/DietrichGebert/ponytail) (MIT). 고지: [`NOTICE`](NOTICE).
- Kit common 스코프·최소 diff와 함께 적용한다 (`unity-studio-kit/docs/common/AgentWorkflow.md` §포니테일).

## 에이전트가 지킬 것

- Kit **common** + **personal** (README 불변조건 + `docs/architecture/`; Plan·Optimization·ADR는 요청 시만).
- `.meta` 생성·수정·삭제 금지. `ThirdParty/` · `Plugins/` 직접 수정 금지.
- 패키지 구현은 `Packages/com.monet5379.addressable-layout/`만; Demo·Sample은 출시 필수 경로로 취급하지 않는다.
- C# 변경 후 관련 **asmdef 컴파일** 확인. 통합 검증은 repo 루트 `.\scripts\verify.ps1` (README §로컬 검증).
- 커밋은 사용자 **명시 요청** 시에만. Kit CommitMessages (`unity-studio-kit/docs/common/CommitMessages.md`) — `type(scope): 한글 제목` + **본문(why) 필수**

## 이 repo만의 메모

- Kit와 다른 규약: —
- 네임스페이스: `AddressableLayout.*` (Runtime `AddressableLayout.Resource` 등)
- 의존: `com.unity.addressables` (패키지 manifest)
- 불변조건·Out of scope: [README.md](README.md)

### 커밋 (이 repo)

Kit CommitMessages를 따르고, 아래만 이 repo에서 정한다.

| 항목 | 값 |
|------|-----|
| 언어 | 한글 |
| scope (정본) | `package`, `demo`, `docs`, `ci`, `agent` |
