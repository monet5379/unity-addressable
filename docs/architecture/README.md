# architecture

Addressable Layout **현재 구조** Architecture 허브이다. 코드와 어긋나면 **코드가 이긴다**.

| 정본 | 문서 |
| --- | --- |
| 계약(불변조건) | [루트 README](../../README.md) |
| 용어 | [CONTEXT.md](../../CONTEXT.md) |
| 구조·캐시·handle·흐름 | 아래 Architecture |

레포 `docs/`는 **`architecture/`만** 유지한다 (`adr/` · `reference/` · `plan/` · `research/` 제거 — 내용은 README · CONTEXT · Architecture로 통합).

글쓰기·골격: [unity-studio-kit — Documentation (Architecture)](https://github.com/monet5379/unity-studio-kit/blob/main/docs/game/Documentation.md#architecture-작성-규약).

## 문서

| Architecture | 내용 |
| --- | --- |
| [Architecture_AddressableLayout](Architecture_AddressableLayout.md) | 패키지 경계 · 타입 · 의존 · Play API 요약 |
| [Architecture_BootFlow](Architecture_BootFlow.md) | PathMeta → preload → sync hit-only · place |
| [Architecture_Lookup](Architecture_Lookup.md) | PathMeta · 이중 스캔 · 이관 |
| [Architecture_RuntimeEditor](Architecture_RuntimeEditor.md) | Play 캐시 vs `*ForEditor` · 레이아웃 도구 |

## 코드 위치

| | 경로 |
| --- | --- |
| UPM 본체 | `project/unity-addressable/Packages/com.monet5379.addressable-layout/` |
| Dev fixture·Tests | `project/unity-addressable/Assets/AddressableLayoutDev/` (`Fixture/`, `Tests/Editor/`, `Tests/PlayMode/`) |
| Dev Demo 에셋 | `Assets/Addressables/Demo/` · `Assets/Resources/Demo/` (검증용, UPM 미포함) |

Play·Edit 확인 절차: [루트 README — 로컬 검증](../../README.md#로컬-검증-verify) · [패키지 README — Tests (레포 dev 전용)](../../project/unity-addressable/Packages/com.monet5379.addressable-layout/README.md#tests-레포-dev-전용).

Planning (GitHub): [0.3.0 #1](https://github.com/monet5379/unity-addressable/issues/1) ([#2](https://github.com/monet5379/unity-addressable/issues/2)–[#6](https://github.com/monet5379/unity-addressable/issues/6)) · [0.4.0 #7](https://github.com/monet5379/unity-addressable/issues/7) · [0.5.0 #8](https://github.com/monet5379/unity-addressable/issues/8) ([#9](https://github.com/monet5379/unity-addressable/issues/9)–[#10](https://github.com/monet5379/unity-addressable/issues/10)).
