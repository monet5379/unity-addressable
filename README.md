# Addressable Layout

Unity Addressables용 **Resource 층** UPM 패키지(`com.monet5379.addressable-layout`)이다. 파일명 Lookup, 종류·지역 라벨 preload, Play 동기 Addressables **캐시 hit-only**, 선택적 Resources 이중 스캔, 얇은 `SpawnPrefab`을 제공한다. Treasure/Core식 골격으로 Resources 기반 타이틀이 typed 도메인 파사드 없이 Addressables로 이관할 수 있게 한다.

**구조·부트·Lookup·Runtime/Editor** 상세는 [Architecture](docs/architecture/README.md)가 정본이다. 아래 **불변조건**만 README 계약이다.

## 설치

**Package Manager → Install package from git URL:**

```text
https://github.com/monet5379/unity-addressable.git?path=/project/unity-addressable/Packages/com.monet5379.addressable-layout#v0.1.0
```

1. **`Assets/Addressables/`** 폴더를 만든다. (단수 `Addressable/` 금지)
2. **Tools → Addressable Layout → Refresh Paths** — `Assets/Resources/Data/PathMetaData.json` 생성
3. (선택) Demo — Package Manager **Samples** import → **Tools → Addressable Layout → Demo → Register Boot Sample** → Play
4. (선택) Edit Mode — **Smoke ForEditor (Edit Mode)**

`com.unity.addressables`는 패키지 의존성으로 설치된다. PM 설치·요구 사항은 [패키지 README](project/unity-addressable/Packages/com.monet5379.addressable-layout/README.md).

**이 repo 개발:** `project/unity-addressable/Packages/com.monet5379.addressable-layout`

## 불변조건

- **파일명은 전역 키** — PathMeta 중복 시 경고; Resources↔Addressables 동일 파일명은 **Resources wins** (Resources 스캔 선행).
- **`Assets/Addressables/`만** — 단수 `Addressable/` 콘텐츠 루트 금지.
- **종류 라벨 ≠ 지역 라벨** — 부트/UI/공유는 type; 지역 전용은 place (타이틀 Area id. `area1`/`area2` 금지). 공유 적/VFX는 place만 달지 말고 type(`boot`, `enemy_shared` 등)을 기본으로 한다.
- **동기 `LoadResource` (Play) — Addressables path = 캐시 hit만** — miss는 null. preload 또는 `LoadResourceAsync`. **Resources leaf**는 이관 중 `Resources.Load`.
- **Edit `*ForEditor` ≠ Play sync** — Edit API는 Play `AddressableAssetManager` 캐시를 채우지 않는다.
- **Lookup 전 `PathManager.Load()`** — PathMeta → (선택) 라벨 preload → gameplay.
- **폴더 OK / 라벨 없음** — path `*ForEditor`는 될 수 있으나 라벨 미할당이면 라벨 로드는 실패 또는 빈 목록.
- **이중 스캔 시 Resources·Addressables 동일 파일명 금지** — 이관 후 Resources 쪽 삭제 + Refresh.
- **Spawn 파사드 ≠ 도메인 파사드** — `SpawnPrefab(프리팹 이름)`만; 스테이지/몬스터/UI 라우팅은 타이틀.
- **Demo ≠ 출시 카탈로그** — Sample만 놀이터.
- **Addressables 그룹 ≠ 라벨** — 패킹(용량)은 그룹, 런타임 필터는 라벨.

PathMeta path 형태·이관 절차·API 표면: [Architecture_Lookup](docs/architecture/Architecture_Lookup.md) · [Architecture_AddressableLayout](docs/architecture/Architecture_AddressableLayout.md).

## 이 패키지가 아닌 것

- 게임플레이·전투·스테이지·typed `Spawn*` 도메인 API
- Json/SO **파싱 · Find Facade** · Google Sheets → JSON
- 씬 **Release/Preload** 정책 전체 · Remote CDN·콘텐츠 업데이트 UX·암호화
- **LeanPool 필수** (호스트 선택) · Resources → Addressables **자동 일괄 마이그레이터**
- `.meta` 수동 편집

## 문서

| | |
| --- | --- |
| [Architecture 허브](docs/architecture/README.md) | BootFlow · Lookup · RuntimeEditor |
| [unity-studio-kit](https://github.com/monet5379/unity-studio-kit) | personal 프로필 |
| [unity-save-layout](https://github.com/monet5379/unity-save-layout) | 형제 포트폴리오 |

## License

[MIT](LICENSE)
