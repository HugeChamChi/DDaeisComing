---
system: map-editor
title: 맵·그리드·씬 편집 도구
keywords: 맵, map, 그리드, grid, 타일, tile, 맵 에디터, map editor, 씬 빌더, scenefacilitybuilder, 배치, 구조물 배치, 정렬, sorting, depth, 스폰 위치, editscene, 맵 확장
verified: 2026-11-26
commit: d08c723
---

**한 줄 요약**: EditScene에서 `SceneFacilityBuilder`가 그리드 타일(걷기 가능 여부)을 들고, 씬에 놓은 시설마다 `SceneFacilityInfo`가 그리드 좌표·FacilityData를 가진다. 실행하면 `SceneTestRunner`가 이것을 `MapData` → `CustomGridMap`으로 바꿔 길찾기와 시설 등록에 쓴다.

## 흐름
1. 편집(에디터): `SceneFacilityBuilderEditor`(인스펙터)와 `BathhouseMapEditorWindow`(메뉴 Bathhouse/Map Editor Window)로 타일 칠하기, 맵 확장·축소·이동(`SceneFacilityBuilder.ExpandMapCustom`, `SceneFacilityBuilder.InsertGrid` 등), 시설 배치.
2. 크기·노드 크기·스폰 위치의 원본은 `MapConfig`(SO). 크기를 바꾸면 `SceneFacilityBuilder.ResizeTilesToMatchConfig`로 타일 배열을 맞춘다.
3. 실행: `SceneTestRunner` → `CustomGridMap` 생성 → 씬의 `SceneFacilityInfo` 전부 `FacilityManager.RegisterSceneFacility` → `NPCSpawner.Initialize`.
4. InGame 씬은 `CustomGridTestRunner`가 JSON 맵(`DefaultMap.json`)으로 같은 일을 한다(현재 빌드 씬 아님).
5. 정렬: 탑다운 2D라 `DynamicDepthSorter`가 y좌표로 sortingOrder를 계속 갱신한다. `SceneFacilityInfo.UpdateSortingOrder`, 메뉴 `Tools/Fix All Sprite Sorting`, `Tools/Setup 2D Sorting`.

## 진입 파일
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/EditorTools/SceneFacilityBuilder.cs` | 그리드 타일 데이터와 맵 크기 조작(런타임 컴포넌트) |
| `Assets/Scripts/EditorTools/SceneFacilityInfo.cs` | 씬 시설의 그리드 좌표·FacilityData·정렬 |
| `Assets/Editor/SceneFacilityBuilderEditor.cs` | 빌더 인스펙터(타일 칠하기, 크기 조작 버튼) |
| `Assets/Editor/BathhouseMapEditorWindow.cs` | 맵 편집 창 |
| `Assets/Editor/EditSceneSetup.cs` | 메뉴 Tools/Bathhouse/Create Edit Scene |
| `Assets/Scripts/Managers/SceneTestRunner.cs` | EditScene 실행 런처(그리드 생성·시설 등록·스폰 연결) |
| `Assets/Scripts/Managers/CustomGridTestRunner.cs` | InGame 씬 JSON 맵 런처 |
| `Assets/Scripts/Managers/TilemapTestRunner.cs` | Tilemap 기반 실험 런처(씬 미사용) |
| `Assets/Scripts/Grid/CustomGridMap.cs` | MapData → 노드 그리드 |
| `Assets/Scripts/Grid/TilemapGridMap.cs` | Tilemap → 노드 그리드(실험) |
| `Assets/Scripts/EditorTools/RuntimeMapEditor.cs` | 런타임 맵 저장/불러오기(실험) |
| `Assets/Scripts/EditorTools/DynamicDepthSorter.cs` | y기반 동적 정렬 |
| `Assets/Editor/ApplySortingFix.cs` | 스프라이트 정렬 일괄 수정 메뉴 |
| `Assets/Editor/Setup2DSorting.cs` | 탑다운 정렬 설정 메뉴 |
| `Assets/WorkSpace/HSD/Scripts/Editor/SceneNavigatorWindow.cs` | 씬 이동 창(메뉴 경로가 GaeGGUL에서 넘어온 이름) |

## 데이터
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Data/MapConfig.cs` | 그리드 크기·노드 크기·스폰 좌표 |
| `Assets/Data/MapConfig.asset` | 위 값 |
| `Assets/Scripts/Data/MapData.cs` | 직렬화용 맵(타일·시설 배치) |
| `Assets/Resources/DefaultMap.json` | InGame 씬 기본 맵 |
| `Assets/Scenes/EditScene.unity` | 실제 게임 씬(빌드 씬 유일) |
| `Assets/Scenes/InGame.unity` | JSON 맵 테스트 씬 |

## 불변 규칙·함정
- 맵 크기를 바꾸는 작업(확장·축소·삽입)은 타일 배열과 씬 시설의 `gridX/gridY`를 함께 옮긴다. 한쪽만 바꾸면 시설이 길 위에 놓이거나 길이 막힌다.
- EditScene은 여러 사람이 동시에 고치기 쉬운 큰 YAML 파일이다. 수정 전에 작업판에서 잡혀 있는지 확인한다.
- 스폰 좌표가 맵 밖이면 스포너가 경고 후 (0,0,0)을 쓴다.

## 검증
| 파일 | 역할 |
|---|---|
| `Assets/Scenes/EditScene.unity` | Play → Console `[SceneTestRunner] 총 N개의 구조물` 로그와 손님 이동 확인 |
