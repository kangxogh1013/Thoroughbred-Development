# 질주마 목장 (HorseRanch) — Unity 자동 생성 패키지

## 설치
1. 이 `HorseRanch` 폴더를 Unity 프로젝트의 `Assets/` 아래에 그대로 넣습니다. → `Assets/HorseRanch/...`
2. 컴파일이 끝나면 상단 메뉴 **Tools > HorseRanch > 전체 자동 생성 (Build All)** 을 누릅니다.
3. 처음 한 번은 아래 두 창이 뜰 수 있습니다.
   - **TMP Importer**: [Import TMP Essentials] → 완료 후 메뉴를 한 번 더 실행
   - 한글 폰트는 `Fonts/` 에 에스코어 드림(SCDream4 본문, SCDream6 굵게)이 들어 있어 자동으로 적용됩니다.
4. ▶ Play

## 필요 환경
- Unity 6 또는 2022.3 LTS
- TextMeshPro (Unity 6은 uGUI에 포함, 2022는 com.unity.textmeshpro)
- Input System 패키지 권장 (Project Settings > Player > Active Input Handling = Input System Package 또는 Both)
  - 미설치 시 자동으로 레거시 StandaloneInputModule을 사용합니다.

## 메뉴 (Tools > HorseRanch)
| 메뉴 | 하는 일 |
|---|---|
| 전체 자동 생성 | 아래 1~4를 순서대로 실행 |
| 1. 데이터 에셋 생성 | `Data/` 에 ScriptableObject 생성 (이미 있으면 값 유지) |
| 2. 스프라이트 생성 | `Art/` 에 말·기수·결승선·둥근 UI 스프라이트 PNG 생성 |
| 3. 한글 폰트 설정 | `Fonts/` 의 ttf로 TMP 동적 폰트 에셋 생성 |
| 3-1. 한글 폰트 교체 | 기존 폰트 에셋을 지우고 `Fonts/` 의 폰트로 다시 만든 뒤 열린 씬에 적용 |
| 3-2. 열린 씬에 한글 폰트 적용 | 지금 열린 씬의 모든 TMP 텍스트(비활성 포함) 폰트를 교체 |
| 4. 씬 다시 생성 | `Scenes/HorseRanch.unity` 를 새로 만들고 모든 참조 자동 연결 |
| 씬 열고 플레이 | 씬을 열고 바로 Play |
| 데이터 값 초기화 | 밸런스·이름·보상을 기본값으로 덮어쓰기 |
| 세이브 데이터 삭제 | persistentDataPath의 세이브 파일 삭제 |

## 폴더 구조
```
Scripts/Core     게임 규칙 (순수 C#, MonoBehaviour 없음)
  RanchGame.cs         훈련·경주·마권·상점·교배·업적 전체 로직 + 이벤트
  RaceSimulator.cs     틱 단위 경주 계산
  ParimutuelMarket.cs  파리뮤추얼 배당 (NPC 가상 배팅 풀)
Scripts/Data     ScriptableObject (밸런스, 스탯, 대회, 아이템, 업적, 이름/털색/이벤트)
Scripts/Runtime  RanchGameRunner (게임 생성, JSON 저장/불러오기)
Scripts/UI       탭별 뷰, 팝업, 토스트, 경주 재생
Editor           Tools 메뉴, 데이터/스프라이트/폰트/씬 빌더
```

## 밸런스 조정
`Data/BalanceConfig` 를 선택하면 인스펙터에서 모든 수치를 바꿀 수 있습니다 (Play 중 변경은 다음 실행부터 반영).
대회 상금은 `Data/Races`, 아이템 가격은 `Data/Items`, 업적 보상은 `Data/Achievements`.

## 디버그
- `GameRunner` 오브젝트 → RanchGameRunner 컴포넌트 우클릭: 세이브 삭제 / 골드 +100,000 / 7일 건너뛰기 / 새 게임
- 단축키 (New Input System): 1~5 탭 전환, N 다음 날
- 경주 재생 오브젝트(Panel_Playback) 선택 시 씬 뷰에 레인 영역 Gizmo 표시

## 폰트 라이선스
`Fonts/` 의 에스코어 드림은 S-Core가 무료 배포하는 폰트입니다 (`S-Core_Dream_License.png` 참고). 폰트 파일을 수정하거나 판매할 수 없고, 배포 시 라이선스 안내를 함께 포함합니다.
