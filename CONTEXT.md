# Addressable Layout (com.monet5379.addressable-layout)

Unity Addressables용 **Resource 층** UPM 패키지 도메인 어휘다. Play·캐시 **실행 계약** 정본은 [루트 README](README.md)와 [docs/architecture/](docs/architecture/README.md) Architecture다. 이 파일과 `docs/adr/`는 에이전트·0.3.0 구현 티켓이 같은 말을 쓰기 위한 용어·결정 레이어다.

## Language

### Lookup

**PathMeta**:
Editor **레이아웃 도구** 갱신으로 만든 `Assets/Resources/Data/PathMetaData.json`과, Play에서 `PathManager`가 메모리에 올린 **파일명 → 경로** Lookup. Addressables path(`Assets/Addressables/...`)와 Resources leaf를 한 키로 해석한다.
_Avoid_: Addressables catalog만으로 파일명 lookup, Resources-only PathMeta

**Dual scan**:
PathMeta 갱신 시 `Assets/Resources`와 `Assets/Addressables`를 함께 스캔하는 Editor 규칙. 상세는 [Architecture_Lookup](docs/architecture/Architecture_Lookup.md).
_Avoid_: Addressables-only scan

### Labels and boot

**Type label**:
Asset **종류** 단위 Addressables 라벨(`boot`, `JSON`, `Scriptable`, `ui` 등). 부트·Json/SO preload·Call-site preload에 쓰인다. 패키지 `AddressableLabels`는 Demo·제네릭 상수만 노출한다.
_Avoid_: ship place 라벨, Area/Enemy enum을 UPM에 넣기

**Place label**:
**지역·씬 단위** preload용 라벨. `EnterPlaceAsync` / `LeavePlace`가 acquire·`ReleaseLabel`하는 대상. type label과 **혼용하지 않는다**.
_Avoid_: type label, boot label

**Boot preload**:
`PathManager.Load()` 이후 gameplay sync 전에 type label(예: `boot`)을 캐시에 올리는 단계. Demo는 async preload; 0.3.0부터 호스트는 sync boot API도 선택할 수 있다([spec](docs/plan/tickets/addressable-layout-0.3.0/spec.md)).
_Avoid_: place preload, sync Addressables fetch on gameplay miss

### Load groups (0.3.0)

**Load group**:
`AddressableLabelLoader`가 `LoadRequest.Id`와 라벨 목록으로 한 번에 적재·해제하는 **논리 그룹**. sync(`LabelLoadMode.Sync`) 또는 async runner로 실행한다. `LoadRequest.AssetType`은 라벨 handle `(label, typeof(T))`와 맞출 타입(단일 `PreloadLabelSync<T>`·`LoadResourcesByLabelAsync<T>`와 **같은 T** 권장). `KeepResident`면 일반 `Release`는 무시하고, `force` release만 세션 resident를 깬다.
_Avoid_: Addressables **Group** 설정 이름과 혼동, 단일 `LoadResourcesByLabelAsync` 호출만 load group이라 부르기

**Label ref-count**:
동일 Addressables **라벨 문자열**에 대해 load group·단일 preload acquire가 겹칠 때 asset manager가 유지하는 **참조 횟수**. 0이 되면 label handle을 `Addressables.Release`하고 `CacheOwnership.ReleaseOwner(label)`로 label owner를 제거한다.
_Avoid_: PrimaryKey owner-set과 동일 개념으로 취급, ref-count 없이 첫 `ReleaseLabel`만으로 공유 라벨 eviction

### Cache and Play load

**Sync hit-only**:
Play에서 Addressables 경로의 동기 `LoadResource` / `LoadResourceByName`은 **이미 캐시에 있는 경우만** 객체를 반환하고, miss면 `null`인 계약.
_Avoid_: sync `LoadAsset`, WaitForCompletion gameplay fetch

**PrimaryKey**:
Addressables location의 canonical 캐시 키(대개 GUID). alias(요청 주소·InternalId)는 `CacheOwnership`이 PrimaryKey로 해석한다.
_Avoid_: 파일명 키를 PrimaryKey와 동일시

**CacheOwnership**:
PrimaryKey당 객체 하나와 **owner-set**을 관리하는 내부 캐시. Addressables handle은 `AddressableAssetManager`가 `_directHandles` / `_labelHandles`로 따로 들고 있다.
_Avoid_: `_resourcesCache` 단일 맵, Dragon fork의 `_labelToCacheKeys` 병행 모델(0.3.0에서 통합 — [ADR 0001](docs/adr/0001-label-load-group-cache-ownership.md))

**Direct owner**:
`CacheOwnership.Direct` 상수 owner. `LoadResourceAsync`가 PrimaryKey에 붙이는 소유권. `Release(key)`는 Direct owner와 `_directHandles`만 건드린다.
_Avoid_: 라벨 문자열 owner

**Label owner**:
`LoadResourcesByLabelAsync`·label preload가 PrimaryKey에 붙이는 **Addressables 라벨 문자열** owner. `ReleaseLabel` / ref-count 0 시 `ReleaseOwner(label)`로 해당 라벨이 캐시에 기여한 PrimaryKey들에서 label owner만 제거한다. Direct owner나 다른 라벨 owner가 남으면 sync hit은 유지된다.
_Avoid_: load group id를 CacheOwnership owner로 쓰기(owner는 **라벨**; group id는 load group·ref-count 집계용)

**Label handle**:
`(label, typeof(T))` 키로 `AddressableAssetManager`가 보관하는 `LoadAssetsAsync` handle. ref-count 0일 때 해제 대상이다.
_Avoid_: Direct handle과 혼동

### Facade

**ResourcesManager**:
Play 파사드. Path 분기, sync hit-only, Spawn/Despawn, place enter/leave, (0.3.0) boot·label preload 진입점. 게임은 보통 이 타입만 노출한다.
_Avoid_: 타이틀 typed spawn 파사드를 패키지에 넣기

**AddressableAssetManager**:
Addressables 로드, `CacheOwnership`, handle 맵, label ref-count(0.3.0). `ResourcesManager.AssetManager`로 공유 인스턴스에 접근한다.
_Avoid_: 호스트마다 별도 캐시 인스턴스
