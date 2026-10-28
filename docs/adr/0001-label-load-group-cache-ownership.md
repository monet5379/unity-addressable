---
status: accepted
---

# Label load group은 CacheOwnership 위에 두고 label ref-count로 해제한다

Project Dragon 임베드 `AddressableLayout`은 라벨 preload마다 `_labelToCacheKeys`(PrimaryKey 목록)와 `_labelEntries`(handle·`RefCount`)를 **캐시 객체 맵과 병행** 유지한다. UPM v0.2.0은 `CacheOwnership`(PrimaryKey owner-set, `ReleaseOwner`)과 Direct/label handle 분리로 Dragon fork의 단일 `_resourcesCache`를 대체했지만, **load group·동기 boot·라벨 ref-count·진행률 API**는 없어 타이틀이 fork를 유지한다.

0.3.0에서는 Dragon의 `AddressableLabelLoader` / `LoadRequest` / `LoadOperation` **공개 표면**을 패키지에 맞추되, `_labelToCacheKeys` 이식은 하지 않는다. 대신 (1) 라벨로 적재한 자산은 기존처럼 **라벨 문자열을 label owner**로 `CacheOwnership`에 등록하고, (2) 동일 라벨을 여러 load group id·단일 preload가 acquire할 때는 asset manager **label ref-count**로 handle·owner 해제 시점을 지연한다, (3) ref-count가 0이면 `_labelHandles`를 `Addressables.Release`하고 `ReleaseOwner(label)`한다.

## Considered Options

- **Dragon legacy 그대로 이식**: `_labelToCacheKeys` + `_labelEntries` + flat `_resourcesCache`를 UPM에 복사. load group 동작은 익숙하지만 v0.2 `CacheOwnership`·EditMode `CacheOwnershipTests`·공유 PrimaryKey owner-set 규칙과 **이중 진실**이 생긴다. 되돌리려면 캐시 전면 재작성과 Dragon 소비자 재검증이 필요하다.
- **CacheOwnership만, ref-count 없이 `ReleaseLabel`**: v0.2와 동일. 구현은 짧지만 load group A가 release해도 B가 같은 라벨을 쓰면 **조기 eviction**이 나와 Dragon 부트·place 중첩 preload와 맞지 않는다.
- **선택: CacheOwnership + label ref-count + LabelLoader (채택)**: PrimaryKey 수명은 owner-set 정본 유지. 라벨 단위 handle·in-flight는 `_labelHandles`와 (0.3.0) label ref-count. load group id는 owner가 아니라 **acquire/release 집계 키**다.

## Consequences

- **이점**: 캐시 의미가 Architecture·`CacheOwnershipTests` 한 줄기로 유지된다. 두 라벨이 같은 PrimaryKey를 공유해도 owner-set 규칙으로 sync hit-only가 그대로 설명된다. Dragon fork의 `_labelToCacheKeys` 스캔(`IsCacheKeyTracked`) 없이 공유 PrimaryKey eviction을 표현할 수 있다.
- **비용**: asset manager에 ref-count·load group 상태가 추가된다. `CopyLoadedLabels` 등 디버그 API는 ref-count와 asset count를 **노출**해야 한다. `ReleaseLabelRef` obsolete alias 한 릴리스 유지(spec).
- **되돌리기**: ref-count·LabelLoader를 제거하고 v0.2식 즉시 `ReleaseLabel`로 돌아가면, 호스트가 중첩 preload를 쓰는 순간 캐시 회귀가 난다. legacy map으로 되돌리면 `CacheOwnership` 계약 전체를 다시 맞춰야 하므로 **minor 내 revert 비용이 크다**.
- **Trade-off**: label owner는 여전히 **라벨 문자열** 하나다. load group id별 PrimaryKey subset release는 지원하지 않는다( Dragon legacy도 라벨 단위 cache key list였다). group별 partial eviction이 필요하면 ADR을 다시 연다.
- **검증**: `CacheOwnershipTests` 확장, LabelLoader Edit 테스트, `scripts/verify.ps1`(spec Testing Decisions).

## See also

- [CONTEXT.md](../../CONTEXT.md) — load group, label owner, label ref-count
- [addressable-layout-0.3.0 spec](../plan/tickets/addressable-layout-0.3.0/spec.md)
- [Architecture_AddressableLayout](../architecture/Architecture_AddressableLayout.md) — Play 캐시·handle 표
