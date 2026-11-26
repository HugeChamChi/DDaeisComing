---
system: unity-tools
title: Unity 명령·빌드·검증
keywords: unity, 유니티, 컴파일, compile, 빌드, build, ios, xcode, apk, android, batchmode, 에디터 메뉴, 콘솔 로그, editor.log
verified: 2026-11-26
commit: d08c723
---

**한 줄 요약**: 이 프로젝트는 **Unity 2022.3.62f3**이다. GaeGGUL의 Unity CLI·Pipeline 패키지·mcp-unity는 Unity 6 전용이라 여기서는 쓰지 않는다. 컴파일 확인은 열린 에디터의 Console 또는 batchmode 로그로 한다.

## 버전·패키지
| 파일 | 역할 |
|---|---|
| `ProjectSettings/ProjectVersion.txt` | 에디터 버전(2022.3.62f3) |
| `Packages/manifest.json` | URP 14, UGUI, TextMeshPro, UniTask(git), Test Framework 1.1.33 |
| `ProjectSettings/EditorBuildSettings.asset` | 빌드 씬: EditScene 하나 |

## 명령
| 파일 | 역할 |
|---|---|
| `Assets/Editor/iOSBuildScript.cs` | 메뉴 Build/Build iOS (Xcode), batchmode에서는 `-executeMethod iOSBuildScript.BuildIOS` |
| `Build_iOS_Locally.bat` | 로컬 iOS 빌드 배치 |
| `Assets/Editor/GenerateNPCAnimator.cs` | 메뉴 Tools/Generate NPC Animator |

### 컴파일 확인 (에디터를 닫은 상태)
같은 프로젝트를 연 에디터가 있으면 batchmode가 실패한다. 에디터가 열려 있으면 그 Console을 본다.

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -quit -projectPath E:\DDaeisComing -logFile E:\DDaeisComing\Logs\batch-compile.log
Select-String -Path E:\DDaeisComing\Logs\batch-compile.log -Pattern 'error CS|Compilation failed'
```

에디터 설치 경로는 PC마다 다르다. Unity Hub의 설치 목록에서 확인한다.

### 에디터 로그
| 파일 | 역할 |
|---|---|
| `%LOCALAPPDATA%/Unity/Editor/Editor.log` | 열린 에디터의 최근 로그(컴파일 오류·예외) |

## 불변 규칙·함정
- 워크트리 `E:/DDaeisComing-lab`(work/lab)도 같은 프로젝트다. 두 폴더를 동시에 에디터로 열 수는 있지만, 같은 폴더를 두 번 열 수는 없다.
- `.meta` 파일은 GUID 원본이다. 스크립트·에셋을 옮기거나 지울 때 `.meta`를 같이 다룬다. 커밋 훅이 짝이 안 맞는 `.meta`를 경고한다.
- `Library/`, `Temp/`, `Logs/`, `*.csproj`, `*.sln`은 git에서 제외된다.
