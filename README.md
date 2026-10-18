# Addressable Layout

Unity Addressables용 **Resource 층** UPM 패키지(`com.monet5379.addressable-layout`)이다. 파일명 Lookup, 종류·지역 라벨 preload, Play 동기 Addressables **캐시 hit-only**, 선택적 Resources 이중 스캔, 얇은 `SpawnPrefab`을 제공한다. Treasure/Core식 골격으로 Resources 기반 타이틀이 typed 도메인 파사드 없이 Addressables로 이관할 수 있게 한다.

**구조·부트·Lookup·Runtime/Editor** 상세는 [Architecture](docs/architecture/README.md)가 정본이다. 아래 **불변조건**만 README 계약이다.

## 설치

**Package Manager → Install package from git URL:**

```text
https://github.com/monet5379/unity-addressable.git?path=/project/unity-addressable/Packages/com.monet5379.addressable-layout#v0.2.0
```

1. **`Assets/Addressables/`** 폴더를 만든다. (단수 `Addressable/` 금지)
2. **Tools → Addressable Layout → 레이아웃 도구** → **갱신** — `Assets/Resources/Data/PathMetaData.json` 생성
3. (선택) Demo — Samples import → **레이아웃 도구** → **부트 샘플 등록** → Test Runner **Play Mode** `DemoPlayModeTests`
4. (선택) Edit Mode — **ForEditor 스모크** · Edit Mode `CacheOwnershipTests`

`com.unity.addressables`는 패키지 의존성으로 설치된다. PM 설치·요구 사항은 [패키지 README](project/unity-addressable/Packages/com.monet5379.addressable-layout/README.md).

**이 repo 개발:** Unity 프로젝트는 `project/unity-addressable`, 패키지 소스는 `project/unity-addressable/Packages/com.monet5379.addressable-layout`이다.

### 로컬 검증 (verify)

Git·CI·Addressables **콘텐츠 빌드** 없이, 위 Test Runner 절차와 같은 순서를 batchmode로 한 번에 돌린다.

1. Edit Mode — `AddressableLayout.EditorTests` (`CacheOwnershipTests` · `PrefabInstanceProviderTests`)
2. Demo 준비 — `AddressableLayoutVerify.PrepareDemo` (`RegisterBootSample`, Fast Mode)
3. Play Mode — `AddressableLayout.PlayModeTests` (`DemoPlayModeTests`)

저장소 **루트**에서:

```powershell
.\scripts\verify.ps1
```

- Unity 경로: `ProjectSettings/ProjectVersion.txt` 버전의 Hub Editor. 다른 경로면 `UNITY_EXE` 환경 변수.
- 로그·테스트 결과: `logs/verify/` (gitignore). 단계 실패 시 중단.

Demo Sample이 `Assets/Samples/...`에 없으면 2단계가 실패한다 (소비자 프로젝트와 동일 전제).

## 불변조건

- **파일명은 전역 키** — PathMeta 중복 시 경고; Resources↔Addressables 동일 파일명은 **Resources wins** (Resources 스캔 선행).
- **`Assets/Addressables/`만** — 단수 `Addressable/` 콘텐츠 루트 금지.
- **종류 라벨 ≠ 지역 라벨** — 부트/UI/공유는 type; 지역 전용은 place (타이틀 Area id. `area1`/`area2` 금지). 공유 적/VFX는 place만 달지 말고 type(`boot`, `enemy_shared` 등)을 기본으로 한다.
- **동기 `LoadResource` (Play) — Addressables path = 캐시 hit만** — miss는 null. preload 또는 `LoadResourceAsync`. **Resources leaf**는 이관 중 `Resources.Load`.
- **Play 캐시 소유권** — `LoadResourceAsync` = Direct owner, 라벨 preload = 해당 라벨 owner, 동기 `LoadResource` = 빌림(소유 없음). PrimaryKey·alias는 **마지막 owner 해제 시에만** 캐시에서 제거; `Release` / `ReleaseLabel`은 멱등.
- **Edit `*ForEditor` ≠ Play sync** — Edit API는 Play `AddressableAssetManager` 캐시를 채우지 않는다.
- **Lookup 전 `PathManager.Load()`** — PathMeta → (선택) 라벨 preload → gameplay.
- **폴더 OK / 라벨 없음** — path `*ForEditor`는 될 수 있으나 라벨 미할당이면 라벨 로드는 실패 또는 빈 목록.
- **이중 스캔 시 Resources·Addressables 동일 파일명 금지** — 이관 후 Resources 쪽 삭제 + Refresh.
- **Spawn 파사드 ≠ 도메인 파사드** — `SpawnPrefab(프리팹 이름)`만; 스테이지/몬스터/UI 라우팅은 타이틀.
- **`SpawnPrefab` / `Despawn` provider** — `IPrefabInstanceProvider`로 인스턴스 생성·반환 교체 (기본 Instantiate/Destroy). **첫 `SpawnPrefab` 전** 등록; 살아 있는 인스턴스가 있을 때 provider 교체는 미정의. **`Despawn`은 `SpawnPrefab`으로 만든 인스턴스만.** `Release` / `ReleaseLabel`(`LeavePlace`) / `ReleaseAll` 전에 해당 prefab 인스턴스(풀 비활성 포함)는 호스트가 정리.
- **Demo ≠ 출시 카탈로그** — Sample만 놀이터.
- **Addressables 그룹 ≠ 라벨** — 패킹(용량)은 그룹, 런타임 필터는 라벨.

PathMeta path 형태·이관 절차·API 표면: [Architecture_Lookup](docs/architecture/Architecture_Lookup.md) · [Architecture_AddressableLayout](docs/architecture/Architecture_AddressableLayout.md).

## 이 패키지가 아닌 것

- 게임플레이·전투·스테이지·typed `Spawn*` 도메인 API
- Json/SO **파싱 · Find Facade** · Google Sheets → JSON
- 씬 **Release/Preload** 정책 전체 · Remote CDN·콘텐츠 업데이트 UX·암호화
- **풀링 필수화** — `IPrefabInstanceProvider`로 호스트가 구현 (예: LeanPool) · Resources → Addressables **자동 일괄 마이그레이터**
- `.meta` 수동 편집

## 문서

| | |
| --- | --- |
| [Architecture 허브](docs/architecture/README.md) | BootFlow · Lookup · RuntimeEditor |
| [unity-studio-kit](https://github.com/monet5379/unity-studio-kit) | personal 프로필 |
| [unity-save-layout](https://github.com/monet5379/unity-save-layout) | 형제 포트폴리오 |

## License

[MIT](LICENSE)
