# Addressable Layout

[English](README.md) | **한국어**

Unity Addressables 레이아웃이에요. 

**파일명 → 경로 Lookup**, **종류(type) + 지역(place) 라벨**, **라벨 preload**와 **동기 로드 = 캐시 hit만**, 
선택적 **Resources ↔ Addressables 이중 스캔**, 얇은 Spawn 파사드를 둡니다. 

클래스 골격은 Treasure/Core식 Resource 층을 따라, Resources 기반 타이틀(예: Dragon)이 typed 게임 파사드를 다시 쓰지 않고 이관할 수 있게 해요.

**포함** (목표 Done — [마일스톤](docs/milestones.md) 참고)

- **Runtime** — `PathManager` / PathMeta, `AddressableAssetManager`, `AddressableLabels`, `ResourcesManager` (이름 로드, 라벨 preload, 동기 hit-only, 이중 경로, 도메인 API 없는 Spawn/Despawn)
- **Editor** (선택 마일스톤) — Path Settings 갱신, Edit Mode `*ForEditor` 로드
- **Demo** (선택) — 부트 preload, place 라벨 입장/퇴장, 로드·스폰 놀이터 (출시 카탈로그 아님)

게임에 넣을 패키지는 **`Assets/AddressableLayout`**만 복사해요. `Assets/Demo`는 참고용이에요.

**마일스톤 A–C (이 repo):** Runtime은 `project/unity-addressable/Assets/AddressableLayout`에 있어요 (`ResourcesManager`·PathManager·Addressables). Unity에서 **Tools → Addressable Layout → Demo → Register Boot Sample** (boot + place 샘플 + PathMeta 갱신) 후 Play 하세요. PathMeta만: **Tools → Addressable Layout → Refresh Paths**.

## 설치

`unity-addressable/.../Assets/AddressableLayout`를 프로젝트 `Assets/`로 통째 복사해요 (Runtime/Editor asmdef가 있으면 유지).

**Addressables** 패키지가 필요해요. 개발 중 Unity 프로젝트는 `project/unity-addressable/` 아래에 둘 수 있어요. 설치 단위는 여전히 **`Assets/AddressableLayout`**이에요. **Demo는 설치 대상이 아니에요.**

## Done (패키지 목표)

Dragon류 타이틀의 **Resources → Addressables** 이관 기준이 될 표면과, Treasure/Core에 가까운 타입:


| 조각                        | 역할                                                               |
| ------------------------- | ---------------------------------------------------------------- |
| `AddressableAssetManager` | Addressables 로드 · Dictionary 캐시 · 릴리즈                            |
| `AddressableLabels`       | 종류 라벨(`boot`, `JSON`, `Scriptable`, …) + 지역 라벨(Area 이름)          |
| `PathManager`             | 파일명 → 주소 / Resources 상대경로 · PathMeta 갱신                          |
| `ResourcesManager`        | `Load` / `LoadAsync` / 라벨 preload · 동기 = 캐시 hit만 · Spawn/Despawn |


**패키지에 없음:** typed 게임 파사드(`SpawnStage`, `SpawnMonster`, 팝업 라우팅), Json/SO 파싱 Facade, LeanPool(호스트 선택), 시트 Sync.

진행: [docs/milestones.md](docs/milestones.md).

## 불변조건

- **파일명은 전역 키** — PathMeta는 중복 시 경고(`Duplicate filename in PathMeta; keeping first path`), 첫 path만 등록.
- `Assets/Addressables/`**만** — 단수 `Addressable/` 콘텐츠 루트 금지.
- **종류 라벨 ≠ 지역 라벨** — 부트/UI/공유는 type, 지역 전용은 place(타이틀 Area id. `area1`/`area2` 금지).
- **동기** `Load`**(Play) = 캐시 hit만** — miss는 null. 라벨 preload 또는 `LoadAsync`. miss를 동기 Addressables 조회로 막지 않음.
- **Lookup 전** `PathManager.Load()` — 부트: PathMeta → 라벨 preload → 게임플레이 로드/스폰.
- **폴더 OK / 라벨 없음** — Edit/`*ForEditor`는 성공할 수 있으나, 라벨 미할당이면 Play 라벨 로드는 실패할 수 있음.
- **이중 스캔 시 Resources·Addressables 동일 파일명 금지** — Resources 우선.
- **Spawn 파사드 ≠ 도메인 파사드** — 패키지 Spawn은 프리팹 이름; 스테이지/캐릭터/UI 라우팅은 타이틀.
- **Demo ≠ 출시 카탈로그** — 샘플 라벨·에셋은 놀이터만.

## 이 패키지가 아닌 것

- 게임플레이·전투·스테이지·typed `Spawn*` 도메인 API
- Json/SO **파싱 · Find Facade**
- Google Sheets → JSON 파이프라인
- 라벨 입장/퇴장을 넘는 씬 **Release/Preload** 정책 전체
- **LeanPool 필수** (호스트가 풀링 가능; 패키지 기본은 Instantiate/Destroy일 수 있음)
- Remote CDN 운영·콘텐츠 업데이트 UX·암호화
- `.meta` 수동 편집

## 라벨 (종류 + 지역)


| 축      | 예                                             | 로드 시점             |
| ------ | --------------------------------------------- | ----------------- |
| **종류** | `boot`, `JSON`, `Scriptable`, `ui`, `player`  | 앱 부트 / 파이프라인      |
| **지역** | `GallatinForest`, `Dawn`, … (타이틀 `AreaNames`) | Area 입장; 퇴장 시 릴리즈 |


공유 적/VFX: place 하나만 달지 말고 공유 type 라벨(`enemy_shared` 또는 `boot`)을 기본으로.

그룹(Local/Remote 패킹)과 라벨은 별개예요. 용량은 그룹, 런타임 필터는 라벨.

## 레이아웃 (콘텐츠)

권장 타이틀 콘텐츠 루트:

```text
Assets/Addressables/
  JSON/
  Scriptable/
  Prefabs/
  ...
Assets/Resources/                 # 이중 경로 이관 중 선택
  Data/PathMetaData.json          # 또는 패키지가 정한 PathMeta 위치
```

패키지 코드:

```text
Assets/AddressableLayout/
  Runtime/
  Editor/                         # Refresh Paths (B); Path Settings UI는 E
Assets/Demo/                      # 놀이터만 — 출시 복사 금지
Assets/Resources/Data/
  PathMetaData.json               # Refresh Paths로 생성
```

## 관련

- [마일스톤](docs/milestones.md) — 제작 순서 A → Done
- [unity-studio-kit](https://github.com/monet5379/unity-studio-kit) — personal 프로필 (README + 불변조건)
- 형제 포트폴리오: [unity-save-layout](https://github.com/monet5379/unity-save-layout)

## License

[MIT](LICENSE)

영문 산문은 AI 보조일 수 있어요. 표현이 어긋나면 [한국어 README](README.ko.md) 또는 코드를 우선하세요.