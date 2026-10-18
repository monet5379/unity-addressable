# 아키텍처

Addressable Layout 패키지의 **구조·데이터 흐름·컴포넌트 관계**를 적는 폴더이다.

**계약(불변조건) 정본**은 루트 [README](../../README.md)에 두고, 여기는 **왜 이렇게 나뉘는지**, **부트/로드/스폰 순서**, **Runtime ↔ Editor 경계** 같은 설계 설명을 둔다.

**제작 순서·Verify**는 [마일스톤](../milestones.md)을 본다.

## 문서

| 문서 | 내용 |
| --- | --- |
| [개요](overview.md) | 패키지 경계 · 클래스 역할 · 의존 방향 · Play API 요약 |
| [부트 흐름](boot-flow.md) | PathMeta → preload → sync hit-only · place enter/leave |
| [Lookup · PathMeta](lookup.md) | 이중 스캔 · 중복 규칙 · path 형태 · 이관 |
| [Runtime / Editor](runtime-editor.md) | Play 캐시 vs `*ForEditor` · PathMeta Refresh |

## 코드 위치 (참고)

패키지 본체: `project/unity-addressable/Assets/AddressableLayout/`  
Demo 검증: `project/unity-addressable/Assets/Demo/`
