# Addressable Layout

Unity Addressables 레이아웃이에요.

**파일명 → 경로 Lookup**, **종류(type) + 지역(place) 라벨**, **라벨 preload**와 **동기 로드 = 캐시 hit만**,
선택적 **Resources ↔ Addressables 이중 스캔**, 얇은 Spawn 파사드를 둬요.

클래스 골격은 Treasure/Core식 Resource 층을 따라, Resources 기반 타이틀(예: Dragon)이 typed 게임 파사드를 다시 쓰지 않고 이관할 수 있게 해요.

**포함** (목표 Done — [마일스톤](docs/milestones.md) 참고)

- **Runtime** — `PathManager` / PathMeta, `AddressableAssetManager`, `AddressableLabels`, `ResourcesManager` (`LoadResource` / `LoadResourceAsync` / `LoadResourcesByLabelAsync`, 동기 hit-only, 이중 경로, `SpawnPrefab` / `Despawn`)
- **Editor** — Path Settings 윈도우 · Refresh Paths, Edit Mode `ResourcesManagerForEditor` (`LoadResourceForEditor` / `LoadResourcesByLabelForEditor`, Play 캐시 미사용)
- **Demo** (선택) — 부트 preload, place 라벨 입장/퇴장, `SpawnPrefab` / `Despawn`, 이중 스캔 Lookup 놀이터 (출시 카탈로그 아님)

게임에 넣을 패키지는 **`Assets/AddressableLayout`**만 복사해요. `Assets/Demo`는 참고용이에요.

**마일스톤 A–D + F + E (이 repo):** Runtime·Editor는 `project/unity-addressable/Assets/AddressableLayout`에 있어요. Unity에서 **Tools → Addressable Layout → Demo → Register Boot Sample** 후 Play 하세요. PathMeta: **Path Settings** 또는 **Refresh Paths**. Edit Mode 스모크: **Smoke ForEditor (Edit Mode)**.

## 설치

`unity-addressable/.../Assets/AddressableLayout`를 프로젝트 `Assets/`로 통째 복사해요 (Runtime/Editor asmdef가 있으면 유지).

**Addressables** 패키지가 필요해요. 개발 중 Unity 프로젝트는 `project/unity-addressable/` 아래에 둘 수 있어요. 설치 단위는 여전히 **`Assets/AddressableLayout`**이에요. **Demo는 설치 대상이 아니에요.**

## Done (패키지 목표)

Dragon류 타이틀의 **Resources → Addressables** 이관 기준이 될 표면과, Treasure/Core에 가까운 타입:

| 조각 | 역할 |
| --- | --- |
| `AddressableAssetManager` | Addressables 로드 · Dictionary 캐시 · 릴리즈 |
| `AddressableLabels` | 종류 라벨(`boot`, `JSON`, `Scriptable`, …) + 지역 라벨(Area 이름) |
| `PathManager` | 파일명 → 주소 / Resources 상대경로 · PathMeta 갱신 |
| `ResourcesManager` | `LoadResource` / `LoadResourceAsync` / `LoadResourcesByLabelAsync` · 동기 = 캐시 hit만 · `SpawnPrefab` / `Despawn` |
| `ResourcesManagerForEditor` (Editor) | `LoadResourceForEditor` / `LoadResourcesByLabelForEditor` · AssetDatabase · **Play 캐시 미사용** |
| Path Settings (Editor) | **Tools → Addressable Layout → Path Settings** — Refresh Paths + 엔트리 수 |

**패키지에 없음:** typed 게임 파사드(`SpawnStage`, `SpawnMonster`, 팝업 라우팅), Json/SO 파싱 Facade, LeanPool(호스트 선택), 시트 Sync.

진행: [docs/milestones.md](docs/milestones.md).

## 불변조건

- **파일명은 전역 키** — PathMeta는 중복 시 경고(같은 트리: `Duplicate filename in PathMeta; keeping first path`; Resources↔Addressables 충돌: `Resources wins`). Resources를 먼저 스캔하므로 Resources 경로가 유지돼요.
- **`Assets/Addressables/`만** — 단수 `Addressable/` 콘텐츠 루트 금지.
- **종류 라벨 ≠ 지역 라벨** — 부트/UI/공유는 type, 지역 전용은 place(타이틀 Area id. `area1`/`area2` 금지).
- **동기 `LoadResource` (Play) — Addressables 경로 = 캐시 hit만** — miss는 null. 라벨 preload 또는 `LoadResourceAsync`. miss를 동기 Addressables 조회로 막지 않아요. **Resources leaf**(`Assets/` 접두 없음)는 이관 중 `Resources.Load`를 써요.
- **Edit `*ForEditor` ≠ Play sync** — `LoadResourceForEditor` / `LoadResourcesByLabelForEditor`는 AssetDatabase(및 Addressable settings 라벨)만 봐요. `AddressableAssetManager` Play 캐시를 채우지 않아요.
- **Lookup 전 `PathManager.Load()`** — 부트: PathMeta → 라벨 preload → 게임플레이 로드/스폰.
- **폴더 OK / 라벨 없음** — 경로 `*ForEditor`는 성공할 수 있으나, 라벨 미할당이면 Edit 라벨 로드·Play 라벨 로드 모두 실패(또는 빈 목록)할 수 있어요.
- **이중 스캔 시 Resources·Addressables 동일 파일명 금지** — Resources 우선 (`Duplicate filename in PathMeta; Resources wins`).
- **Spawn 파사드 ≠ 도메인 파사드** — 패키지 `SpawnPrefab`은 프리팹 이름; 스테이지/캐릭터/UI 라우팅은 타이틀.
- **Demo ≠ 출시 카탈로그** — 샘플 라벨·에셋은 놀이터만.

### 이중 스캔 경로 형태

| 출처 | PathMeta 값 | `IsAddressablePath` |
| --- | --- | --- |
| `Assets/Addressables/...` | `Assets/...` 주소 | `true` |
| `Assets/Resources/...` | Resources.Load leaf(확장자 없음), 예: `Demo/Foo` | `false` |

**한 파일 이관:** `Assets/Resources/` → `Assets/Addressables/`로 옮기고 Resources 쪽을 지운 뒤 **Path Settings / Refresh Paths**하면 Lookup이 `Assets/...`가 돼요. 일괄 마이그레이터는 이 패키지에 없어요.

## 이 패키지가 아닌 것

- 게임플레이·전투·스테이지·typed `Spawn*` 도메인 API
- Json/SO **파싱 · Find Facade**
- Google Sheets → JSON 파이프라인
- 라벨 입장/퇴장을 넘는 씬 **Release/Preload** 정책 전체
- **LeanPool 필수** (호스트가 풀링 가능; 패키지 기본은 Instantiate/Destroy일 수 있음)
- Remote CDN 운영·콘텐츠 업데이트 UX·암호화
- Resources → Addressables **자동 일괄 마이그레이터**
- `.meta` 수동 편집

## 라벨 (종류 + 지역)

| 축 | 예 | 로드 시점 |
| --- | --- | --- |
| **종류** | `boot`, `JSON`, `Scriptable`, `ui`, `player` | 앱 부트 / 파이프라인 |
| **지역** | `GallatinForest`, `Dawn`, … (타이틀 `AreaNames`) | Area 입장; 퇴장 시 릴리즈 |

공유 적/VFX: place 하나만 달지 말고 공유 type 라벨(`enemy_shared` 또는 `boot`)을 기본으로 해요.

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
  Editor/                         # Path Settings · Refresh Paths · *ForEditor
Assets/Demo/                      # 놀이터만 — 출시 복사 금지
Assets/Resources/Data/
  PathMetaData.json               # Path Settings / Refresh Paths로 생성
```

## 관련

- [마일스톤](docs/milestones.md) — 제작 순서 A → Done (이력·Verify)
- [unity-studio-kit](https://github.com/monet5379/unity-studio-kit) — personal 프로필 (README + 불변조건)
- 형제 포트폴리오: [unity-save-layout](https://github.com/monet5379/unity-save-layout)

## License

[MIT](LICENSE)
