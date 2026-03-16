# Framework — Unity 게임 프레임워크

## 기술 스택

- **Unity 6** (6000.0.70f1), URP 17
- **VContainer** — DI 컨테이너 (vendored: `Assets/VContainer/`)
- **UniTask** — async/await (git 패키지)
- **DOTween** — 트윈 애니메이션 (`UNITASK_DOTWEEN_SUPPORT` Standalone 정의)
- **Addressables** 2.9.1 — 에셋 로딩
- .NET Standard 2.1+ (`apiCompatibilityLevel: 6`)

## 프로젝트 구조

```
Assets/
├── 00.Setting/          # 프레임워크 코어
│   ├── Scripts/
│   │   ├── UI/          # UIManager, PopupView, ToastManager, UIAnimation
│   │   └── Scene/       # SceneLoader, LoadingScreen
│   └── Setting/         # RootScope, RootCanvas, RootManager, RootCanvasSetting
├── 01.Addressable/      # Addressable 에셋
├── 02.Game/             # 게임 로직
│   └── Scripts/
│       ├── Scene/       # 씬별 Scope + View (TestScene, Test2Scene)
│       ├── UI/Popup/    # 팝업 구현체 (TestPopup)
│       └── Addressable/ # Address 상수 (자동 생성)
├── VContainer/          # vendored VContainer 소스
└── Plugins/             # DOTween 등
```

## 아키텍처

### DI 계층 (VContainer)

```
RootScope (Singleton)          ← UIManager, ToastManager, SceneLoader, RootManager
  └── SceneScope (Scoped)      ← 씬별 View, 씬 전용 서비스
       예: TestScope, Test2Scope
```

- **RootScope**: `LifetimeScope` — 앱 전체 싱글톤 등록
- **SceneScope**: 씬별 `LifetimeScope` — `RegisterComponent(view)`로 View 등록
- 자식 스코프는 `VContainerSettings` fallback으로 RootScope에 자동 연결

### UI 시스템

- **UIManager** — Addressable 프리팹 로드 + DI 주입(`InjectGameObject`) + 팝업 스택 관리
- **PopupView** — 팝업 베이스. `IPopup` 라이프사이클: `OnShow()` → `OnEnter()` → `OnExit()` → `OnHide()`
- **ToastManager** — fire-and-forget 큐 기반 토스트
- **UIAnimation** — static 유틸. `FadeIn/Out`, `ScaleIn/Out`, `PopIn/Out` + CancellationToken
- **SceneLoader** — Additive 씬 전환. 로딩 화면 옵션

### 초기화 흐름

```
RootCanvasSetting.OnEnable() → Instance 설정 (PreloadedAssets)
  → RootScope.Configure() → 싱글톤 등록 + RootCanvas Instantiate
    → RootManager.Start() (IStartable) → IInitializable 순회 호출
      → RootCanvas.Start() → UIManager.Initialize(root), ToastManager.Initialize(toastRoot)
```

## 코드 컨벤션

- **네임스페이스**: `Framework` (전체 통일)
- **상수**: `UPPER_SNAKE_CASE` (`Address.TESTPOPUP_PREFAB`)
- **private 필드**: `_camelCase` (`_uiManager`, `_resolver`)
- **DI 주입**: 생성자 주입 우선, MonoBehaviour는 `[Inject]` 필드 주입
- **async**: UniTask 사용. `Task` 사용 금지. DOTween은 직접 `await tween` (`.WithCancellation(ct)`)
- **MonoBehaviour + DI**: `Awake()`에서 `[Inject]` 필드 접근 금지 (주입 미완료). `Start()` 이후 사용

## 커밋 규칙

사용자가 "커밋", "커밋해", "커밋 후 푸쉬"라고 말하면 아래 규칙대로 커밋한다.

### prefix

- `feat` — 새 기능, 새 파일 추가
- `fix` — 버그 수정, 오류 수정

- `build` — 빌드 시 버전 커밋 (빌드 전용)

이 3개만 사용. 다른 prefix 금지.

### 커밋 분류 (변경 내용을 3가지로 분리)

1. **변경된 코드** — `.cs` 스크립트 파일 변경/추가/삭제
2. **변경된 UI** — `.prefab`, `.unity`, `.asset`, `.controller`, `.anim` 등 Unity 에셋 변경
3. **추가된 이미지** — `.png`, `.jpg`, `.psd`, `.svg` 등 이미지/스프라이트 추가

각 분류에 해당하는 파일이 있을 때만 별도 커밋. 해당 없으면 생략.

### 커밋 메시지 형식

```
feat : 간결한 설명 (한국어)
```

- 콜론 앞뒤 공백 1칸
- 이전 커밋 메시지 스타일을 따름 (예: `feat : ui system`, `feat : popup open`)

### 빌드 커밋

빌드 시에는 모든 변경사항을 하나로 커밋하고 태그를 단다.

```bash
git add -A
git commit -m "build : 1.0.0"
git tag v1.0.0
```

### 예시

```bash
# 1. 코드 변경
git add Assets/**/*.cs
git commit -m "feat : ui manager 구현"

# 2. UI 에셋 변경
git add Assets/**/*.prefab Assets/**/*.unity Assets/**/*.asset
git commit -m "feat : popup prefab 추가"

# 3. 이미지 추가
git add Assets/**/*.png Assets/**/*.jpg
git commit -m "feat : 아이콘 이미지 추가"

# 4. 빌드
git add -A
git commit -m "build : 1.0.0"
git tag v1.0.0
```

## 주의사항

- `UIManager._resolver`는 RootScope 컨테이너 — Addressable 프리팹에는 RootScope 싱글톤만 주입 가능
- `InjectGameObject`는 확장 메서드 — `using VContainer.Unity;` 필요
- `PopupView.Awake()`는 private — 서브클래스는 `OnAwake()` 오버라이드 사용
- `Address` 클래스는 자동 생성 — 수동 편집 금지
- `RootCanvasSetting`은 PreloadedAssets에 등록되어야 함
