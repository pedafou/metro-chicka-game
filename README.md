# Metro Chicka

**Chicka를 펼쳐 길을 만들고, 작은 열차에서 다음 Chicka가 내리는 3D 싱글플레이 프로토타입.**

Unity **6000.3.23f1** · **Unfold Study A01** · 한국어 · Windows / macOS 빌드 경로 제공.

이 버전은 핵심 조작·연쇄·겹 순서를 비교하기 위한 테스트 빌드입니다. 유료 완성판이나 이전 프로토타입의 후속 릴리스가 아닙니다.

![실제 3D 플레이어의 연쇄 펼침](docs/images/chain-moving.png)

검증: 규칙 테스트 **22개**, 씬 통합 테스트 **3개** 통과. macOS 그래픽 배치 플레이어에서 드래그·연쇄·하차·되돌리기 검증을 통과했습니다. Windows는 빌드와 패키지 검증까지 완료했고 실기기 실행은 남아 있습니다. [검증 기록](docs/VALIDATION.md)

## 설계 조건

- 이름은 Metro Chicka로 유지합니다.
- 마트료시카의 중첩과 펼침을 핵심 플레이에 포함합니다.
- 반복 자체가 즐거운 조작, 크게 이어지는 결과, 화면에서 읽을 수 있는 다음 기회를 설계합니다.
- 기존 게임과의 차별성은 실제 조작·판단 구조를 기준으로 검증합니다.

## 플레이

1. **첫 펼침:** Chicka를 드래그해 방향을 잡고 놓습니다. 클릭 선택 후 `Q/E`와 `Space`로도 조작할 수 있습니다.
2. **이어지는 운행:** 같은 배치에서 연쇄 ON/OFF를 비교합니다. `Z`로 행동 전체를 되돌릴 수 있습니다.
3. **겹의 순서:** 직선·굽은 길·교량·분기를 비교합니다. 아래쪽에 바깥부터 사용할 겹이 표시됩니다.

길과 신호가 이어지면 다른 Chicka도 한 겹 펼쳐집니다. 열차는 자신이 나온 길을 따라 이동한 뒤 내부 Chicka를 내려주고 사라집니다. 기존 길에는 신호만 이어집니다. 각 Chicka는 한 연쇄에 한 번만 펼쳐지고, 겹은 보충되지 않습니다. 하차한 Chicka의 표시 크기는 동일하게 유지합니다.

`1/2/3` 장면 선택 · `Q/E` 방향 회전 · `Space` 펼치기 · `Z` 되돌리기 · `R` 다시 시작 · `H` 도움말 · `Esc` 취소/도움말 닫기. 동작 줄이기·소리 설정은 화면 아래에서 바꿀 수 있습니다. 이번 테스트 빌드는 저장 없이 장면을 즉시 재시작합니다.

화면 비율은 **16:10 또는 16:9**를 권장합니다. 고정된 사선 카메라를 사용하는 실제 3D 장면입니다.

테스트 순서와 피드백 항목은 [플레이테스트 안내](docs/PLAYTEST.md)를 참고하세요. 자동 테스트는 재미·독창성·구매 의향을 입증하지 않습니다.

## Unity에서 실행

Unity Hub에 이 폴더를 추가하고 **6000.3.23f1**으로 엽니다. `Assets/MetroChicka/Scenes/UnfoldStudy.unity`를 열어 Play를 누릅니다. 씬 준비 및 빌드 메뉴는 **Metro Chicka**에 있습니다.

```bash
UNITY_EDITOR='/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity'
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -executeMethod MetroChicka.Editor.BuildPrototype.Prepare -quit -logFile /tmp/metro-study-prepare.log
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/metro-study-editmode.xml -logFile /tmp/metro-study-editmode.log
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults /tmp/metro-study-playmode.xml -logFile /tmp/metro-study-playmode.log
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -executeMethod MetroChicka.Editor.BuildPrototype.Mac -quit -logFile /tmp/metro-study-mac.log
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -buildTarget Win64 -executeMethod MetroChicka.Editor.BuildPrototype.Windows -quit -logFile /tmp/metro-study-windows.log
```

결과는 `Builds/Study/macOS/Metro Chicka.app`과 `Builds/Study/Windows/Metro Chicka.exe`에 생성됩니다.

## 개발 플레이어 자동 검증

개발 빌드는 `-metro-study-smoke /tmp/metro-study-smoke` 옵션으로 3D 드래그, UI 레이캐스트, 연쇄 ON/OFF, 열차 소멸, 되돌리기와 경계를 확인하고 PNG 캡처 및 `result.txt`를 남깁니다. 정상 실행에서는 자동 조작하지 않습니다. `-batchmode`를 함께 사용하면 창을 조작하지 않고 카메라 렌더로 검증할 수 있습니다. 캡처에는 그래픽 장치가 필요하므로 이때는 `-nographics`를 사용하지 않습니다.

## 구조와 범위

- `Core/Board.cs`: Unity 의존성이 없는 위치·겹·신호·사전 예측 규칙.
- `Runtime/Prototype.cs`: 3D 입력, 길과 열차·하차 연출, 한국어 화면.
- `Runtime/ToyFactory.cs`: 직접 생성한 입체 마트료시카·열차·레일 메시.
- `Tests`: 규칙 및 씬 통합 테스트.

메인 캠페인, 장기 성장, 세션 저장, 게임패드, 모바일, 온라인과 유료 결제는 구현 범위 밖입니다. 캐릭터·레일은 코드로 만든 임시 아트입니다. Noto Sans KR 글꼴은 [OFL](Assets/MetroChicka/Resources/Fonts/OFL.txt)에 따라 포함합니다.

이전 프로토타입의 코드, 디자인과 로드맵을 새 구현의 전제로 사용하지 않습니다.
