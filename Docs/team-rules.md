# 개발 규칙

Project TownLife의 Unity 6.3 LTS, 2D 중심 4인 팀 프로젝트용 개발 규칙이다.
게임 장르가 바뀌어도 유지되는 공통 규칙만 둔다.

## 1. 기본 원칙

### 필수
- 개발 기간은 팀 확정 후 정한다. 첫 주는 아이디어/기획서/에셋 탐색, 마지막 주는 폴리싱/문서 작업 중심으로 둔다.
- 핵심 재미 검증을 우선한다.
- 핵심 게임 로직은 C# 스크립트에 둔다.
- Unity Inspector, Prefab, Scene은 값 조정, 배치, 연출, UI 연결, 빠른 프로토타이핑에 사용한다.
- 팀 공유 문서는 Notion에 정리한다.
- GitHub 저장소는 `Femur-0607/ProjectTownLife`를 사용한다.
- 팀 개발 규칙은 이 문서를 기준으로 삼는다.

### 권장
- 기능 하나를 작게 만들고 바로 Play Mode에서 확인한다.
- 완벽한 구조보다 팀원이 이해하고 고칠 수 있는 구조를 우선한다.
- 발표와 제출 가능성을 항상 고려한다.

## 2. 폴더 구조

### 권장 루트 구조

```text
Assets/
  _Project/                     # 팀이 직접 만드는 게임 파일의 기준 폴더
    00.Scenes/                  # Unity Scene 파일
    01.Scripts/                 # C# 스크립트
      01.Core/                  # 게임 전체에서 쓰는 기본 타입, 공통 정의, 유틸
      02.Gameplay/              # 점수, 승패, 게임 흐름, 룰 같은 핵심 플레이 로직
      03.Characters/            # 플레이어, 적, NPC 등 캐릭터 관련 스크립트
      04.Interaction/           # 상호작용, 줍기, 버튼, 트리거, 조사 기능
      05.UI/                    # UI Controller, View, Presenter, UI 데이터 연결
      06.Data/                  # ScriptableObject, enum, 설정값, 밸런스 데이터 타입
      07.Systems/               # 저장/로드, 스폰, 상태 관리 등 여러 기능이 함께 쓰는 시스템
      08.Utils/                 # 작은 공용 헬퍼. 너무 커지면 적절한 폴더로 이동
    02.Prefabs/                 # 공용 Prefab
      01.Characters/
      02.Gameplay/
      03.UI/
      04.Effects/
    03.ScriptableObjects/       # 밸런스, 아이템, 설정 데이터 에셋
    04.Art/                     # Sprite, Texture, Tile, Animation Clip
      01.Sprite/
      02.Tile/
      03.AnimationClip/
      04.Texture/
    05.Audio/                   # 효과음, 배경음
      01.BGM/
      02.SFX/
    06.VFX/                     # 파티클, 셰이더, 2D 이펙트
      01.Particles/
      02.Shader/
      03.2D_Effect/
    07.UI/                      # UI 이미지, 폰트, UI 전용 에셋
      01.UI_Image/
      02.Font/
      03.UI_Asset/
    08.Prototypes/              # 임시 실험용 에셋. 최종 사용 전 정리 필요
    09.Developers/              # 개인 작업 공간. 공용 에셋은 여기에 오래 두지 않음
Docs/                           # 저장소에 함께 두는 팀 문서
  team-rules.md                 # 개발 규칙 문서
  ai-code-rules.md              # AI 코드 생성용 짧은 내부 규칙
```

### 필수
- 팀이 직접 만드는 에셋과 코드는 `Assets/_Project/` 아래에 둔다.
- C# 스크립트는 `Assets/_Project/01.Scripts/` 아래에 둔다.
- 임시 작업은 `Assets/_Project/08.Prototypes/` 또는 `Assets/_Project/09.Developers/<Name>/`에 둔다.
- 공용 Prefab, Scene, ScriptableObject 이동/삭제는 팀 채널에 먼저 알린다.
- Scene 파일은 동시에 여러 명이 수정하지 않는다.

### 권장
- 기능별로 `01.Scripts`, `02.Prefabs`, `04.Art` 하위 폴더 이름을 맞춘다.
- 인터페이스는 기능별 `Interfaces/` 폴더에 둔다.
  - 여러 기능이 공통으로 의존하는 약속은 `01.Core/Interfaces/`에 둔다.
  - 특정 기능 담당자가 소유하는 약속은 해당 기능 폴더의 `Interfaces/`에 둔다.
  - 예: `01.Core/Interfaces/ISaveService.cs`, `03.Characters/Interfaces/IRelationship.cs`, `07.Systems/Combat/Interfaces/IHealth.cs`.
- 인터페이스는 구현체보다 먼저 합의가 필요한 경우에만 만든다. 아직 사용처가 하나뿐이면 구체 클래스로 시작한다.
- 프로토타입이 끝난 에셋은 이름과 위치를 정리해서 공용 폴더로 옮긴다.

## 3. 네이밍 규칙

### 필수
- C# 클래스와 파일 이름은 일치시킨다.
  - 예: `PlayerController.cs` 안에는 `PlayerController` 클래스.
- 클래스, struct, enum, property, method는 PascalCase를 사용한다.
  - 예: `PlayerController`, `ItemData`, `GamePhase`, `ApplyDamage`.
- private 필드와 매개변수는 소문자 시작 camelCase를 사용한다.
  - 예: `moveSpeed`, `currentHealth`, `deltaTime`.
- Inspector에 노출할 필드는 `[SerializeField] private`를 기본으로 사용한다.
- 단, 코드로 자동 캐싱할 수 있는 런타임 참조는 Inspector에 노출하지 않고 일반 `private` 필드로 둔다.
  - 예: 같은 GameObject의 `GetComponent<T>()`, `Camera.main`, `FindAnyObjectByType<T>()`, `GameContext.Instance`에서 얻는 서비스.
  - 이런 필드는 `Awake`, `Start`, `Reset`, `OnValidate`, 또는 `CacheReferences()`에서 할당한다.
- bool 변수와 bool 매개변수는 의미가 바로 보이게 짓는다.
  - 예: `isDead`, `canMove`, `hasKey`.
- Prefab은 `PF_` 접두사를 사용한다.
- Scene은 `SC_` 접두사를 사용한다.
- ScriptableObject 에셋은 `SO_` 접두사를 사용한다.
- Sprite는 `SPR_`, Texture는 `TEX_`, Material은 `MAT_`를 사용한다.
- Animation Clip은 `AN_`, Animator Controller는 `AC_`를 사용한다.
- Persistent 씬에 상주하며 전역 상태나 흐름을 관리하는 클래스는 `Manager` 접미사를 사용한다.
  - 예: `GoldManager`, `TimeManager`, `SaveManager`, `ItemInventoryManager`, `MagicBookInventoryManager`, `StaminaManager`, `HealthManager`, `SceneFlowManager`.
  - 단, 개별 오브젝트 동작 Component에는 `Manager`를 붙이지 않는다. 예: `PlayerMovement`, `PlayerInteraction`, `CraftingStation`, `DisplayStand`, `MiningNode`.

### 권장
- 이름은 역할이 바로 보이게 짓는다.
- `NewBehaviourScript`, `TestObject`, `FinalFinal` 같은 이름을 공용 폴더에 두지 않는다.

## 4. C# 코드 규칙

### 필수
- 핵심 게임 규칙은 C#에 둔다.
- 필수 구조와 반복 로직은 C#에 두고, 테스트 값과 표현 연결은 Inspector/Prefab/Scene에서 조정한다.
- 게임 규칙, 상태 전이, 반복 로직, 점수/승패 흐름, 상호작용 판정, 저장/로드, 스폰, 재사용 Component는 C#에 둔다.
- 다른 코드가 의존하는 필수 Component는 C# 스크립트로 만든다.
  - 예: `Health`, `Inventory`, `InteractionHandler`, `Spawner`, `GameState`.
- `public` 필드는 기본적으로 사용하지 않는다. Inspector 노출은 `[SerializeField] private`를 사용한다.
- `[SerializeField] private`는 팀원이 Unity에서 조정, 연결, 확인해야 하는 값에만 사용한다.
- 런타임 캐시 필드는 직렬화하지 않는다. 외부에서 읽어야 하면 `public` 읽기 전용 프로퍼티로 연결한다.
- 기본 접근 제한자는 `private`로 둔다.
- 다른 스크립트가 상태를 읽거나 동작을 요청해야 할 때는 `public` 프로퍼티 또는 `public` 메서드를 사용한다.
- 외부에서 값을 바꿀 필요가 없으면 읽기 전용 프로퍼티를 우선한다.
- 상속받은 클래스에서 접근해야 하는 멤버만 `protected`를 사용하고, 동작을 바꿔야 하는 지점은 `protected virtual`을 사용한다.
- `GetComponent`는 반복 호출하지 말고 필요하면 `Awake`에서 캐싱한다.
- `FindObjectOfType`, `GameObject.Find`, 문자열 기반 경로 탐색은 꼭 필요할 때만 사용한다.
- `Update`는 꼭 필요할 때만 사용한다.
- 하드코딩된 Scene 이름, Resources 경로, 태그 문자열을 피한다.
- Unity Console에 새 에러나 경고를 만들지 않는다.
- 코드 주석은 기본적으로 한국어로 작성한다.
- 코드 흐름을 이해하는 데 필요한 한국어 주석을 남긴다.
- Inspector에 노출하는 `[SerializeField]` 필드는 팀원이 값을 조정할 때 의미를 알 수 있도록 한국어 `[Tooltip("...")]`을 함께 작성한다.
- Unity 생명주기 메서드와 이벤트 콜백처럼 역할이 명확한 이벤트 함수는 제외하고, 직접 작성한 함수에는 한국어 XML `/// <summary>` 설명을 작성한다.
- 다른 스크립트가 호출하거나 읽는 `public`, `protected` 프로퍼티는 한국어 XML `/// <summary>` 설명을 작성한다.
- 긴 스크립트는 기능 단위 `#region`으로 묶을 수 있다. 단, 작은 클래스에는 억지로 넣지 않는다.

### 권장
- 큰 클래스보다 역할이 분명한 작은 Component를 선호한다.
- ScriptableObject로 조정 가능한 값은 코드에 박아두지 않는다.
- 함수 이름은 행동을 드러내게 쓴다. 예: `ApplyDamage`, `StartInteraction`.
- 복잡한 계산, 상태 전이, Unity 생명주기 의존에는 짧은 한국어 주석을 단다.
- 코드와 같은 말을 반복하는 주석은 남기지 않는다.
- AI가 만든 코드는 팀원이 읽기 쉽게 의도, 흐름, 예외 조건이 보이게 작성한다.
- 짧고 의미가 바로 보이는 값은 변수 없이 바로 대입할 수 있다.
- 같은 값을 반복해서 쓰거나, 조건/계산의 의미가 중요하거나, 디버깅 중간값이 필요하거나, 한 줄이 길어지면 의미 있는 이름의 지역 변수로 뺀다.
- 지역 변수는 기본적으로 명시 타입을 사용한다.
- `var`는 오른쪽 코드만 봐도 타입이 바로 보이거나, 타입명이 너무 길어 반복하면 읽기 어려울 때만 사용한다.

### 지역 변수 예시

```csharp
currentHealth = Mathf.Max(currentHealth - damage, 0);
moveDirection = input.normalized;

float distanceToTarget = Vector2.Distance(transform.position, target.position);
bool canInteract = distanceToTarget <= interactRange && hasLineOfSight;

Vector2 direction = input.normalized;
Rigidbody2D body = GetComponent<Rigidbody2D>();
var item = new InventoryItem(itemData);
```

### 접근 제한자 예시

```csharp
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(currentHealth - damage, 0);
    }

    protected virtual void Die()
    {
        // 상속 클래스에서 사망 연출을 바꿀 수 있는 지점
    }
}
```

## 5. Unity Editor / Prefab 규칙

### 필수
- Inspector, Prefab, Scene은 아래 용도에 사용한다.
  - 수치 조정.
  - 이동속도, 체력, 점프력, 감지 거리, 쿨타임, 데미지 같은 테스트 튜닝값.
  - Sprite, Animation, Material, Audio, VFX, UI 연결.
  - Camera Offset, Sorting Layer, Collision Layer 같은 표현/배치 설정.
  - 레벨/Scene 배치.
  - 빠른 프로토타이핑.
- Prefab에는 팀원이 조정해야 하는 값만 노출한다.
- 복잡한 분기, 상태 전이, 반복 로직은 C# 이전 후보로 표시한다.
- 공용 Prefab 변경 전 변경 범위와 테스트 Scene을 확인한다.
- Scene에만 숨은 핵심 로직을 만들지 않는다.

### 권장
- Prefab Variant는 공통 구조가 확실할 때만 사용한다.
- 임시 프로토타입 Prefab은 `Proto_` 또는 `Temp_` 표시를 붙인다.
- UI 오브젝트는 계층 구조가 너무 깊어지면 역할별로 정리한다.

## 6. 2D / 아이템 데이터 규칙

### 필수
- 2D 게임 오브젝트는 SpriteRenderer, Collider2D, Rigidbody2D 기준으로 설계한다.
- 3D 물리 컴포넌트와 2D 물리 컴포넌트를 섞지 않는다.
- 레이어, 태그, Sorting Layer는 팀에서 이름을 정하고 공유한다.
- 아이템 시스템을 만들 때는 기본적으로 `ItemType`, `ItemData`, `ItemDataSO` 조합으로 정의한다.
- `ItemType`은 아이템의 큰 분류만 나타낸다.
  - 예: `Consumable`, `Key`, `Equipment`, `Quest`, `Material`, `Weapon`.
- `ItemData`는 아이템 ID, 표시 이름, 설명, 아이콘, 타입, 수량 제한 같은 기본 데이터를 담는다.
- `ItemDataSO`는 Inspector에서 조정할 수 있는 아이템 데이터 원본으로 사용한다.
- 월드에 배치되는 아이템은 `ItemPickup` 같은 Component가 `ItemDataSO`를 참조하게 한다.
- 인벤토리 보유, 추가, 제거, 사용 같은 상태 변경 로직은 `Inventory` 같은 C# Component에 둔다.
- 아이템 효과, 사용 조건, 획득 판정처럼 반복되는 게임 로직은 Scene 이벤트가 아니라 C#에 둔다.

### 권장
- 새 아이템 종류가 생길 때마다 C# 클래스를 무작정 늘리지 말고, 먼저 `ItemType`과 `ItemDataSO`로 표현 가능한지 검토한다.
- 아이템별 수치와 표시 데이터는 ScriptableObject로 조정하고, 핵심 사용 로직만 C# 함수나 Component로 분리한다.

## 7. 입력 / 멀티플레이 범위

### 필수
- 입력 방식은 Unity Input System 패키지 기반 New Input System을 기본으로 사용한다.
- `Assets/InputSystem_Actions.inputactions`를 팀 공용 입력 액션 기준으로 관리한다.
- 입력 액션 이름, 액션 맵, 바인딩 변경은 팀 채널에 공유한다.
- 멀티플레이는 기본 범위에 포함하지 않는다.
- 멀티플레이 기능, 네트워크 패키지, 관련 Project Settings 변경은 팀장 확인 후 진행한다.

## 8. Git / Git LFS 규칙

### 필수
- `main`은 제출/발표 가능한 안정 버전으로 유지한다.
- `dev`는 기능 통합 브랜치로 사용한다.
- 작업은 `dev`에서 기능 브랜치를 만들어 진행하고, 완료 후 `dev`로 합친다.
- `main`에는 직접 커밋하지 않는다.
- 기능 브랜치 이름은 `type/feature-name` 형식을 사용한다.
- type은 `feat`, `fix`, `docs`, `chore`, `test`, `refactor` 중 하나를 사용한다.
- feature-name은 영어 소문자와 하이픈을 사용한다.
- 대형 이미지, 사운드, 영상, 모델, Unity 바이너리 에셋은 필요하면 Git LFS로 관리한다.
- `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`는 커밋하지 않는다.
- Unity `.meta` 파일은 에셋과 함께 커밋한다.
- 같은 Scene이나 큰 Prefab 변경은 작업 전 팀 채널에 공유한다.
- 충돌이 나면 혼자 억지로 해결하지 말고 팀 채널에 공유한다.

### 권장
- 기능 브랜치 이름 예시.

```text
feat/movement
feat/core
feat/combat
feat/interaction
feat/trade
fix/hud-null-access
docs/update-team-rules
```

- 커밋 제목은 `type: short summary` 형식을 사용한다.
- 커밋 본문에는 필요하면 수정 파일과 변경 내용을 짧게 적는다.
- 한 커밋에는 한 가지 목적의 변경만 담는다.
- 코드 변경과 대형 에셋 변경은 가능하면 커밋을 분리한다.
- 커밋 메시지 예시.

```text
feat: add player movement input

수정 파일:
- Assets/_Project/01.Scripts/03.Characters/PlayerMovement.cs
- Assets/InputSystem_Actions.inputactions

변경 내용:
- 이동 입력을 New Input System 기준으로 연결
- Rigidbody2D 이동 처리 추가
```

## 9. 작업 절차

### 필수
1. Notion 작업 보드에서 작업 카드를 만든다.
2. 작업 범위와 완료 기준을 적는다.
3. 브랜치를 만든다.
4. 필요한 파일만 수정한다.
5. Unity Console 에러 확인과 Play Mode 검증을 한다.
6. 작업 카드에 변경 내용과 검증 결과를 적는다.
7. 리뷰를 요청한다.

### 작업 카드 템플릿

```markdown
## 목표
-

## 범위
- 수정할 파일/기능:
- 수정하지 않을 것:

## 완료 기준
-

## 검증
- [ ] Unity Console 에러 없음.
- [ ] Play Mode 실행.
- [ ] 대상 Scene에서 기능 확인.
- [ ] UI 깨짐 없음.

## 위험
-
```

## 10. 코드 리뷰 규칙

### 필수
- 리뷰는 버그, 충돌 위험, 책임 분리, 검증 누락을 먼저 본다.
- AI가 만든 코드도 사람이 만든 코드와 같은 기준으로 본다.
- 리뷰 요청에는 수정 파일, 테스트 방법, 남은 위험을 적는다.
- 리뷰 받은 사람은 반영 여부를 댓글로 남긴다.

### 권장
- 스타일만 지적하기보다 팀 규칙 링크를 함께 남긴다.
- 짧은 팀 프로젝트에서는 “나중에 완벽하게”보다 “지금 제출 가능한가”를 같이 본다.

## 11. AI 사용 규칙

### 필수
- AI에게 작업을 맡기기 전 관련 파일과 현재 구조를 알려준다.
- AI 결과를 그대로 신뢰하지 않는다.
- Unity Console, Play Mode, 대상 Scene 확인 중 필요한 검증을 직접 한다.
- 외부 패키지 추가, 대규모 구조 변경, 공용 부모 클래스 변경은 팀장 확인 후 진행한다.
- AI가 만든 코드의 책임 위치를 작업 카드에 적는다.
- AI가 만든 코드는 흐름을 볼 수 있는 한국어 주석을 포함해야 한다.
- AI가 긴 스크립트를 만들 때는 기능 단위 `#region` 사용 여부를 검토한다.
- AI 결과물은 가독성, 네이밍, 책임 범위를 리뷰에서 반드시 확인한다.

### 권장
- 한 번에 큰 시스템을 요청하지 말고 작은 단위로 요청한다.
- AI에게 “기존 스타일 유지”, “수정 범위 제한”, “검증 방법 포함”을 명시한다.

### AI 작업 요청 프롬프트 템플릿

```text
목표:
-

현재 구조:
- 관련 파일:
- 관련 클래스/Prefab/Scene:

제약:
- Unity 6.3 LTS, 2D 중심.
- 핵심 로직은 C#에 둔다.
- Inspector/Prefab/Scene은 값 조정, 표현, UI 연결, 프로토타입에만 사용한다.
- 외부 패키지 추가 금지.
- Unity Input System 패키지 기반 New Input System 사용.
- 멀티플레이 구현 금지. 필요하면 팀장 확인 먼저.
- 요청 범위 밖 리팩터링 금지.

원하는 변경:
-

완료 기준:
-

검증 방법:
-

출력 형식:
- 변경 요약.
- 수정 파일 목록.
- 검증 결과.
- 남은 위험.
```

## 12. 빌드와 검증

### 필수
- C# 변경 후 Unity Console에 컴파일 에러가 없어야 한다.
- Gameplay 변경 후 Play Mode에서 최소 1회 확인한다.
- UI 변경 후 주요 해상도에서 깨짐을 확인한다.
- 새 에셋이나 Prefab은 참조 끊김이 없는지 확인한다.
- 2D 기능 변경 후 Collider2D, Rigidbody2D, Sorting Layer 설정을 확인한다.

### 권장
- 중요한 기능은 테스트 Scene을 따로 둔다.
- 버그 수정은 재현 절차로 다시 확인한다.

## 13. 범위 축소 규칙

### 필수
- 남은 기간 안에 완성 가능성이 낮으면 기능을 줄인다.
- 핵심 루프와 발표 가능한 플레이 흐름을 우선한다.
- 아래 상황이면 범위 축소 회의를 연다.
  - 2일 이상 핵심 기능이 작동하지 않음.
  - Scene/Prefab/에셋 충돌이 반복됨.
  - 팀원 1명에게 작업이 과도하게 몰림.
  - 발표용 빌드가 불안정함.

### 권장
- 기능을 줄일 때는 삭제보다 “최소 버전”을 먼저 정한다.
- 멋진 부가 요소보다 조작감, 피드백, 목표 전달을 우선한다.
