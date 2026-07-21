# KrameLife - 데이터를 하나의 플레이 흐름으로 연결한 2D 경영 RPG

> 본 저장소는 4인 팀 프로젝트 `ProjectTownLife`가 **KrameLife(카르메 라이프)**라는 이름으로 완성된 뒤, 제가 담당한 코드와 협업 문서를 포트폴리오용으로 선별해 정리한 저장소입니다. 외부 에셋, 씬, 프리팹, 빌드 파일과 팀원 담당 코드는 포함하지 않습니다.

[![KrameLife 플레이 영상](GIF/KrameLife-preview.jpg)](https://youtu.be/ArTEC_peleQ)

▶︎ [플레이 영상 보기](https://youtu.be/ArTEC_peleQ) · ▶︎ [GitHub 프로필](https://github.com/Femur-0607)

## 프로젝트 정보

| 항목 | 내용 |
| --- | --- |
| 장르 | 상점 경영 + 제작 + 라이프 시뮬레이션 |
| 개발 기간 | 2026.06 ~ 2026.07 |
| 개발 형태 | 4인 팀 프로젝트 |
| 개발 환경 | Unity 6.3 LTS, C# |
| 담당 | 프로젝트 관리, Core 공통 토대, 데이터 파이프라인, 클라이언트 통합 |
| 주요 기술 | CSV, ScriptableObject, Addressables, Event Channel, Save/Load, Additive Scene Flow |

플레이어가 재료를 모으고 아이템을 제작한 뒤 가판대에서 판매하는 생활형 경영 RPG입니다. 저는 팀 기능이 서로 다른 데이터와 규칙을 사용하지 않도록 공통 ID, 데이터베이스, 저장, 씬 흐름을 설계하고 개별 기능을 메인 플레이 흐름에 통합했습니다.

## 담당 범위

### Framework - 여러 기능이 공유하는 구조

- Google Sheets CSV를 파싱·검증한 뒤 ScriptableObject와 `GameDatabase`로 연결하는 데이터 파이프라인
- 아이템 ID 중복, 필수값, 분류, 아이콘 Addressables 주소를 임포트 전에 검사하는 검증 도구
- `GameContext`를 통한 공통 런타임 의존성 접근과 이벤트 채널 기반 시스템 연결
- `ISaveParticipant` 단위로 각 모듈의 상태를 수집·복원하는 JSON 세이브/로드
- Persistent 씬을 유지하면서 Gameplay 씬과 Spawn ID를 전환하는 Additive Scene Flow
- 기능 범위, ID 규칙, 폴더 구조, 완료 기준과 검증 절차를 문서화하고 팀 기능을 통합

### Game Logic - 게임 규칙과 행동

- 아이템 획득, 인벤토리 표시, 제작 UI와 아이콘 로딩 흐름
- CSV 기반 대화 노드·선택지·조건·효과 실행
- 퀘스트 시작, 단계 완료, 보고 가능 상태와 UI 갱신
- 획득·제작 이벤트를 구독하는 도감 발견 기록과 저장 복원
- 가게 결과, 재화 HUD, 사운드 데이터와 재생 흐름

## 핵심 구현

### 1. CSV → ScriptableObject → GameDatabase

기획 데이터가 Inspector와 개별 ScriptableObject에 흩어지면 같은 아이템을 시스템마다 다르게 해석할 수 있습니다. CSV 행을 공통 모델로 변환하고, 검증을 통과한 데이터만 ScriptableObject로 생성한 뒤 `GameDatabase`에서 ID로 조회하도록 구성했습니다.

- [아이템 CSV 파싱과 검증](Assets/_Project/01.Scripts/01.Core/Item)
- [GameDatabase](Assets/_Project/01.Scripts/01.Core/Databases/GameDatabase.cs)
- [Addressables 아이콘 검증](Assets/_Project/01.Scripts/01.Core/Item/Editor/ItemIconAddressValidator.cs)

### 2. 모듈 단위 Save/Load

하나의 저장 클래스가 모든 시스템을 직접 알지 않도록 `ISaveParticipant` 계약을 두었습니다. 각 모듈이 자신의 상태를 `Capture`하고 `Restore`하므로 도감, 시간, 가게, 평판 같은 기능을 같은 저장 흐름에 추가할 수 있습니다.

- [SaveManager](Assets/_Project/01.Scripts/01.Core/Save/SaveManager.cs)
- [JsonSaveStorage](Assets/_Project/01.Scripts/01.Core/Save/JsonSaveStorage.cs)
- [CollectionSaveParticipant](Assets/_Project/01.Scripts/07.Systems/Collection/CollectionSaveParticipant.cs)

### 3. 데이터 기반 대화와 퀘스트

대사만 표시하는 기능이 아니라 조건 검사와 효과 실행을 데이터로 분리했습니다. 선택지에 따라 아이템·골드·평판·퀘스트 상태를 검사하거나 변경하고, 퀘스트 단계와 UI가 같은 데이터베이스를 사용합니다.

- [DialogueRunner](Assets/_Project/01.Scripts/07.Systems/Dialogue/DialogueRunner.cs)
- [DialogueSheetValidator](Assets/_Project/01.Scripts/07.Systems/Dialogue/DialogueSheetValidator.cs)
- [QuestManager](Assets/_Project/01.Scripts/07.Systems/Quests/QuestManager.cs)

### 4. Persistent Scene Flow

공통 매니저와 플레이어 상태는 Persistent 씬에 유지하고, 실제 플레이 공간만 Additive 방식으로 교체했습니다. enum 기반 Scene/Spawn ID를 사용해 문자열 오타와 잘못된 진입 위치를 줄이고, 전환 전후에 저장 상태를 연결했습니다.

- [SceneFlowManager](Assets/_Project/01.Scripts/01.Core/SceneFlow/Persistent/SceneFlowManager.cs)
- [GameContext](Assets/_Project/01.Scripts/01.Core/SceneFlow/GameContext.cs)

## 협업 방식

- 기능별 담당, 우선순위, 완료 기준과 통합 순서를 문서로 관리했습니다.
- 공통 ID와 데이터 계약을 먼저 맞춘 뒤 각 담당 기능이 같은 기준을 사용하도록 연결했습니다.
- 공용 Scene, Prefab, ScriptableObject 변경 규칙과 AI 보조 코드의 리뷰·검증 기준을 정리했습니다.
- 구현 완료를 개별 기능 동작이 아니라 메인 플레이 흐름에서의 연결과 검증으로 판단했습니다.

→ [기술 문서 인덱스](Docs/README.md) · [팀 개발 규칙](Docs/team-rules.md) · [아키텍처 개요](ARCHITECTURE.md)

## 저장소 구조

```text
Assets/_Project/01.Scripts/
├── 01.Core/          # 데이터, 저장, 씬 흐름, 이벤트 계약
├── 02.Gameplay/      # 아이템 획득과 게임 규칙 연결
├── 05.UI/            # 인벤토리, 제작, 대화, 퀘스트 UI
├── 07.Systems/       # 오디오, 도감, 대화, 퀘스트
└── 08.Utils/         # 공통 유틸리티
Docs/
├── README.md         # 기술 문서 인덱스
├── data-pipeline.md  # CSV/SO/DB 데이터 흐름
├── save-scene-flow.md # 저장과 씬 전환 구조
└── team-rules.md     # 팀 코드·Unity·협업 규칙
GIF/
└── KrameLife-preview.jpg # 플레이 영상 대표 이미지
ARCHITECTURE.md       # 시스템 책임과 의존 방향
```

## 공개 범위와 실행 안내

이 저장소는 구현 방식 검토를 위한 **코드 포트폴리오**입니다. 원본 프로젝트에 사용된 유료·외부 에셋과 팀원 담당 코드가 제외되어 Unity에서 그대로 실행되는 전체 프로젝트가 아닙니다. 실제 플레이 결과는 상단 영상을 확인해 주세요.

© 2026 Yang Ji-seop. Portfolio use only.
