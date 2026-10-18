# architecture

Addressable Layout **현재 구조** Architecture 허브이다. 계약(불변조건) 정본은 [루트 README](../../README.md)이고, 코드와 어긋나면 **코드가 이긴다**.

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
| Demo Sample | `Samples~/Demo/` (Package Manager Samples import) |
| Tests | `Tests/Editor/` · `Tests/PlayMode/` (`CacheOwnershipTests`, `DemoPlayModeTests`) |

Play·Edit 확인 절차: [루트 README — 설치](../../README.md#설치) · [패키지 README — Tests](../../project/unity-addressable/Packages/com.monet5379.addressable-layout/README.md#tests).
