# 때부자 모작 — Unity 모바일 개발 설계 프롬프트 세트

> 사용법: 각 섹션의 프롬프트를 Claude Code / Codex / Cursor에 그대로 붙여넣어 사용하세요.  
> 순서대로 진행하되, 각 단계 완료 후 다음 단계로 넘어가세요.

---

## 0단계 | 프로젝트 컨텍스트 (모든 프롬프트 앞에 붙이는 공통 헤더)

```
[프로젝트 컨텍스트]
- 장르: 목욕탕 타이쿤 (때부자 게임 모작)
- 플랫폼: Unity 2022 LTS, 모바일 (iOS/Android), Portrait
- 핵심 루프: 손님 입장 → 욕조 배정 → 목욕 → 계산 → 돈 → 업그레이드 → 반복
- 코딩 규칙: C#, MonoBehaviour 기반, ScriptableObject로 데이터 분리, 모든 수치는 SO로 관리
- 아키텍처: MVC 패턴 지향, Manager 싱글톤은 최소화 (GameManager, UIManager만 허용)
- 이 컨텍스트를 항상 염두에 두고 코드를 작성해줘.
```

---

## 1단계 | 핵심 데이터 구조 설계

### 1-A. ScriptableObject 설계 프롬프트

```
[때부자 모작 — 데이터 설계]

아래 게임 데이터를 Unity ScriptableObject로 설계해줘.

## 필요한 SO 목록

### CustomerData (손님 데이터)
- 손님 타입 (일반/VIP/단골)
- 인내심 최대값 (float)
- 목욕 소요 시간 (float)
- 기본 지불 금액 (int)
- 팁 보너스 비율 (float, 0~1)
- 만족도별 팁 배율 표 (AnimationCurve로 표현)
- 스폰 가중치 (int, 희귀도 조정용)

### TubData (욕조 데이터)
- 욕조 이름
- 최대 수용 인원 (기본 1)
- 목욕 속도 배율 (float)
- 청결도 감소 속도 (float/min)
- 청소 소요 시간 (float)
- 구매 비용 (int)
- 업그레이드 레벨별 수치 변화 (배열)

### StageData (스테이지 데이터)
- 스테이지 번호
- 목표 매출 (int)
- 제한 시간 (float, 0이면 무제한)
- 손님 스폰 간격 (float)
- 스폰 가능한 손님 타입 목록
- 클리어 보상

## 요구사항
- 각 SO를 [CreateAssetMenu]로 에디터에서 생성 가능하게
- 유효성 검증용 OnValidate() 포함
- XML 주석으로 각 필드 설명 포함
```

### 1-B. 런타임 상태 모델 프롬프트

```
[때부자 모작 — 런타임 상태 모델]

ScriptableObject와 분리된 런타임 상태 클래스를 설계해줘.

## 필요한 클래스

### CustomerInstance (손님 인스턴스)
- 현재 인내심 (float, 실시간 감소)
- 현재 상태 enum: Arriving / Waiting / Bathing / Paying / Leaving
- 배정된 욕조 참조
- 만족도 점수 (float, 0~100)
- 목욕 진행도 (float, 0~1)

### TubInstance (욕조 인스턴스)
- 현재 상태 enum: Empty / Occupied / NeedsCleaning / BeingCleaned
- 현재 손님 참조
- 현재 청결도 (float, 0~100)
- 업그레이드 레벨 (int)

### GameState (전역 게임 상태)
- 현재 보유 금액 (int)
- 현재 스테이지
- 누적 매출
- 총 손님 처리 수
- 이탈 손님 수

## 요구사항
- 각 클래스에 이벤트(Action/UnityEvent) 포함 (상태 변화 시 발행)
- 세이브/로드를 위한 직렬화 가능한 DTO 구조체 별도 정의
```

---

## 2단계 | 핵심 게임 매니저 구현

### 2-A. CustomerManager 프롬프트

```
[때부자 모작 — CustomerManager 구현]

손님의 전체 생명주기를 관리하는 CustomerManager를 구현해줘.

## 동작 명세

### 스폰 로직
- 현재 스테이지의 스폰 간격(StageData)에 따라 코루틴으로 주기적 스폰
- 대기 줄 최대 5명 (초과 시 스폰 중단, 대기열 빌 때 재개)
- 스폰 시 가중치 기반 랜덤으로 손님 타입 선택

### 상태 머신 (각 상태별 Update 로직)
- Arriving: 입장 애니메이션 재생 후 → Waiting 전환
- Waiting: 
  - 매 프레임 인내심 감소 (deltaTime * 인내심감소속도)
  - TubManager에 빈 욕조 요청
  - 빈 욕조 있으면 → Bathing 전환
  - 인내심 0 되면 → Leaving 전환 (이탈 처리)
- Bathing:
  - 진행도 증가 (deltaTime / 목욕소요시간)
  - 진행도 1.0 도달 시 → Paying 전환
  - 욕조 청결도도 함께 감소
- Paying:
  - 만족도 계산 (대기시간, 청결도 기반)
  - 금액 계산 후 GameState에 추가
  - → Leaving 전환
- Leaving: 퇴장 처리 후 오브젝트 풀 반환

### 이벤트
- OnCustomerSpawned(CustomerInstance)
- OnCustomerLeft(CustomerInstance, bool wasServed)
- OnPaymentReceived(int amount, int tip)

## 요구사항
- GameObject Pool 사용 (Instantiate 금지, UnityEngine.Pool 활용)
- 모든 수치는 CustomerData SO에서만 참조
- 테스트용 [ContextMenu] 메서드 포함 (강제 스폰, 강제 이탈)
```

### 2-B. TubManager 프롬프트

```
[때부자 모작 — TubManager 구현]

욕조 배정과 청결도 관리를 담당하는 TubManager를 구현해줘.

## 동작 명세

### 욕조 배정
- GetAvailableTub() → TubInstance? 반환
  - 상태가 Empty이고 청결도 > 20인 욕조 우선
  - 여러 개면 청결도 높은 순으로 반환
  - 없으면 null 반환

### 청결도 시스템
- 손님이 목욕 중일 때 TubData.청결도감소속도만큼 매 프레임 감소
- 청결도 0 도달 시 → NeedsCleaning 상태
- NeedsCleaning 욕조는 자동 청소 or 플레이어 탭으로 청소 시작
- 청소 완료 시 청결도 100으로 복원, → Empty 상태

### 이벤트
- OnTubStateChanged(TubInstance, TubState)
- OnTubNeedsCleaning(TubInstance)

## 요구사항
- 욕조 배치는 에디터에서 씬에 직접 배치 (런타임 생성 X)
- TubInstance 목록은 FindObjectsOfType으로 초기화
- 업그레이드 시 TubData 교체로 수치 변경 가능하게 설계
```

### 2-C. UpgradeManager 프롬프트

```
[때부자 모작 — UpgradeManager 구현]

업그레이드 구매와 적용을 관리하는 UpgradeManager를 구현해줘.

## 업그레이드 항목 (초기 구현 범위)
1. 욕조 수 증가 (새 욕조 활성화)
2. 욕조 품질 업그레이드 (TubData 레벨업 → 목욕속도 증가)
3. 청소부 고용 (NeedsCleaning 욕조 자동 청소 시작)
4. 대기 의자 추가 (대기 최대 인원 +2)

## 동작 명세
- CanPurchase(upgradeId) → bool (돈 충분 & 조건 충족 여부)
- Purchase(upgradeId) → 돈 차감 + 효과 적용 + 이벤트 발행
- 업그레이드 상태는 GameState에 저장
- 각 항목 최대 레벨 제한 (SO에서 정의)

## 요구사항
- 업그레이드 목록은 List<UpgradeData SO>로 관리
- UI와 직접 통신 금지 (이벤트로만 통신)
- 구매 조건 미충족 시 이유 반환 (돈 부족 / 레벨 제한 / 선행 업그레이드 필요)
```

---

## 3단계 | UI 구현

### 3-A. 메인 HUD 프롬프트

```
[때부자 모작 — 메인 HUD UI]

게임 플레이 중 상시 표시되는 HUD를 Unity UI (uGUI) 로 구현해줘.

## HUD 구성 요소

### 상단 바
- 현재 보유 금액 (TextMeshPro, 증가 시 팝업 애니메이션)
- 목표 매출 진행 바 (Slider)
- 남은 시간 (있는 스테이지만)

### 중앙 (씬 위 오버레이)
- 각 손님 머리 위: 인내심 게이지 (WorldSpace Canvas)
- 욕조 위: 진행 바 + 청결도 아이콘

### 하단 바
- 업그레이드 버튼 (잠금/해제 상태 표시)
- 빠른 청소 버튼 (NeedsCleaning 욕조 있을 때 강조)

## 요구사항
- Manager 이벤트 구독으로만 UI 갱신 (폴링 금지)
- 금액 변화 시 DOTween 없이 Unity 기본 Coroutine으로 카운트업 애니메이션
- 모든 텍스트는 TMP_Text
- 해상도 대응: CanvasScaler = Scale With Screen Size, 1080x1920 기준
```

### 3-B. 업그레이드 패널 프롬프트

```
[때부자 모작 — 업그레이드 패널]

업그레이드 구매 UI 패널을 구현해줘.

## 구성
- ScrollView 내에 업그레이드 카드 목록 (동적 생성)
- 각 카드: 아이콘 + 이름 + 설명 + 현재 레벨 + 비용 + 구매 버튼
- 구매 불가 상태: 버튼 비활성화 + 이유 툴팁 표시

## 동작
- 패널 열릴 때 UpgradeManager에서 전체 목록 가져와 카드 생성
- 구매 성공 시 해당 카드만 갱신 (전체 재생성 X)
- 돈 부족으로 구매 불가인 카드는 흐리게 표시

## 요구사항
- 카드 프리팹 1개 + 데이터 주입 방식 (UpgradeCardUI.Setup(UpgradeData))
- 오브젝트 풀로 카드 관리
```

---

## 4단계 | 세이브/로드 시스템

### 4-A. 세이브 시스템 프롬프트

```
[때부자 모작 — 세이브/로드 시스템]

모바일에 적합한 세이브 시스템을 구현해줘.

## 저장 데이터
- 현재 보유 금액
- 업그레이드 레벨 목록
- 현재 스테이지 번호
- 총 플레이 통계 (손님 수, 총 매출 등)

## 기술 스펙
- 저장 포맷: JSON (JsonUtility)
- 저장 위치: Application.persistentDataPath
- 저장 타이밍: 스테이지 클리어 / 업그레이드 구매 / 앱 백그라운드 전환 시
- 자동 저장 주기: 30초 (코루틴)

## 요구사항
- ISaveable 인터페이스 정의 → 각 Manager가 구현
- SaveManager가 ISaveable 목록 순회하며 일괄 저장/로드
- 세이브 파일 버전 관리 (버전 불일치 시 초기화)
- 에디터용 [ContextMenu]: "Save Now", "Load Now", "Delete Save"
```

---

## 5단계 | 통합 & 밸런싱

### 5-A. GameManager (통합) 프롬프트

```
[때부자 모작 — GameManager 통합]

모든 Manager를 조율하는 GameManager를 구현해줘.

## 책임
- 게임 상태 머신: MainMenu / Playing / Paused / StageClear / GameOver
- 스테이지 시작/종료 흐름 제어
- 각 Manager 초기화 순서 보장 (Awake 실행 순서 문제 방지)

## 스테이지 클리어 조건 체크
- 매출 목표 달성 → StageClear 상태 전환
- 제한 시간 초과 & 미달성 → GameOver 전환

## 요구사항
- 싱글톤 패턴 (DontDestroyOnLoad)
- 씬 전환은 Additive Load 방식
- 각 상태 진입/탈출 이벤트 발행
```

### 5-B. 밸런스 튜닝 가이드 (AI에게 조정 요청용)

```
[때부자 모작 — 밸런스 조정 요청]

현재 ScriptableObject 수치를 기반으로 밸런스를 검토하고 조정해줘.

## 현재 수치 (예시, 실제 값으로 교체)
- 손님 인내심: 60초
- 목욕 소요 시간: 30초
- 욕조 1개 기본 청결도 감소: 10/분
- 스폰 간격: 15초
- 스테이지 1 목표 매출: 5000원

## 검토 요청
1. 욕조 1개일 때 스테이지 1 클리어 가능한가? (수치 시뮬레이션)
2. 손님 이탈률이 20~30%가 되도록 인내심 수치 제안
3. 업그레이드 구매 타이밍이 3~5분 플레이 후가 되도록 가격 제안
4. 난이도 곡선: 스테이지 1→5 수치 점진 상승 테이블 제시

## 출력 형식
- 현재 수치 문제점 분석
- 조정 제안 (수치 테이블)
- 조정 근거 설명
```

---

## 부록 A | 클래스 다이어그램 요청 프롬프트

```
[때부자 모작 — 아키텍처 확인]

지금까지 구현한 클래스들의 의존 관계를 정리해줘.

## 출력 형식
- Mermaid classDiagram 형식
- 의존 방향 화살표 포함
- 이벤트로 연결된 관계는 점선으로 표시
- 각 클래스의 핵심 메서드 3개 이내만 표시
```

## 부록 B | 디버그/테스트 프롬프트

```
[때부자 모작 — 디버그 도구]

개발 중 테스트를 위한 인게임 디버그 패널을 구현해줘.

## 기능
- 화면 우상단 토글 버튼 (에디터 + 개발 빌드에서만 표시)
- 현재 CustomerInstance 상태 목록 실시간 표시
- 현재 TubInstance 상태 목록 실시간 표시
- 버튼: 돈 +1000 / 강제 스폰 / 전체 청소 / 스테이지 클리어
- 프레임레이트, 오브젝트 풀 사용량 표시

## 요구사항
- #if UNITY_EDITOR || DEVELOPMENT_BUILD 로 조건부 컴파일
- IMGUI 또는 간단한 uGUI (별도 캔버스)
```
