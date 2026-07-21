# 저장과 씬 전환 구조

## Save/Load

`SaveManager`가 모든 기능의 구체 타입을 직접 참조하지 않도록 저장 참여 계약을 분리했습니다.

```text
SaveManager
  ├─ ISaveStorage → JsonSaveStorage
  └─ ISaveParticipant[]
       ├─ 도감 상태
       ├─ 플레이어 상태
       ├─ 상점 상태
       ├─ 시간 상태
       └─ 평판·관계·해금 상태
```

각 모듈은 `Capture(SaveData)`와 `Restore(SaveData)`만 구현합니다. 새 기능이 추가되어도 저장 관리자의 분기문을 늘리지 않고 같은 흐름에 참여할 수 있습니다.

## Scene Flow

공통 상태는 Persistent 씬에 유지하고 플레이 공간만 Additive 방식으로 교체합니다.

```text
Boot
  ↓
Persistent (GameContext, SaveManager, Player)
  ├─ ShopYard
  ├─ ShopInterior
  ├─ Village
  └─ Forest
```

`GameplaySceneId`와 `SceneSpawnId`를 사용해 문자열 입력을 줄였습니다. 전환 요청이 오면 현재 상태 저장, 기존 Gameplay 씬 해제, 대상 씬 로드, 활성 씬 변경, 상태 복원, Spawn Point 이동 순서로 처리합니다. `isTransitioning`으로 중복 요청도 막습니다.

## 코드

- [`SaveManager.cs`](../Assets/_Project/01.Scripts/01.Core/Save/SaveManager.cs)
- [`JsonSaveStorage.cs`](../Assets/_Project/01.Scripts/01.Core/Save/JsonSaveStorage.cs)
- [`SceneFlowManager.cs`](../Assets/_Project/01.Scripts/01.Core/SceneFlow/Persistent/SceneFlowManager.cs)
- [`GameContext.cs`](../Assets/_Project/01.Scripts/01.Core/SceneFlow/GameContext.cs)
