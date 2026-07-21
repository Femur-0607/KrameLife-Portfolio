# CSV → ScriptableObject → GameDatabase

## 문제

아이템 데이터가 Inspector와 개별 ScriptableObject에 흩어지면 ID 중복, 누락된 필수값, 잘못된 아이콘 주소가 런타임에서 뒤늦게 발견됩니다. 여러 담당자가 인벤토리, 제작, 판매 기능을 동시에 만들 때 서로 다른 데이터를 참조할 위험도 있었습니다.

## 구조

```text
Google Sheets
  ↓ CSV 다운로드
ItemCsvParser
  ↓ 행 모델 변환
ItemSheetCsvMapper
  ↓ 공통 필드 매핑
ItemSheetValidator
  ↓ ID·필수값·분류·주소 검사
ItemSheetImportWindow
  ↓ 검증 성공 데이터만 생성/갱신
GameItemSO
  ↓ ID 기준 등록
GameDatabase
```

## 구현 선택

- `ItemSheetCsvUrlBuilder`가 시트별 CSV URL 구성을 담당합니다.
- 파싱, 매핑, 검증, 에셋 생성을 분리해 오류 위치를 좁혔습니다.
- `ItemSheetValidationError`로 행과 필드 단위 오류를 모아 일괄 표시합니다.
- `ItemIconAddressValidator`가 Addressables 아이콘 주소까지 임포트 전에 확인합니다.
- 런타임 시스템은 원본 CSV가 아니라 `GameDatabase`의 공통 ID 조회를 사용합니다.

## 효과와 한계

팀 기능이 같은 데이터 계약을 사용하고, 잘못된 데이터가 런타임까지 들어가기 전에 차단됩니다. 다만 이 공개 저장소는 외부 에셋과 실제 ScriptableObject 데이터가 제외된 코드 포트폴리오이므로 임포트 결과 자체는 포함하지 않습니다.

## 코드

- [`01.Core/Item`](../Assets/_Project/01.Scripts/01.Core/Item)
- [`GameDatabase.cs`](../Assets/_Project/01.Scripts/01.Core/Databases/GameDatabase.cs)
- [`ItemIconLoader.cs`](../Assets/_Project/01.Scripts/05.UI/Inventory/ItemIconLoader.cs)
