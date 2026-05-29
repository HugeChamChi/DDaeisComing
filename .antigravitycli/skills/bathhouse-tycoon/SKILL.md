---
name: bathhouse-tycoon
description: "Guidelines and prompt set for developing the Bathhouse Tycoon clone (때부자 모작) Unity game."
---

# Bathhouse Tycoon (때부자 모작) Development Guide

## Project Context
- **Genre**: Bathhouse Tycoon (때부자 게임 모작)
- **Platform**: Unity 2022 LTS, Mobile (iOS/Android), Portrait
- **Core Loop**: Customer Entry → Tub Assignment → Bathing → Payment → Money → Upgrade → Repeat
- **Coding Rules**: C#, MonoBehaviour based, ScriptableObject for data separation, all numerical values managed by SO.
- **Architecture**: MVC pattern oriented, Manager singletons minimized (GameManager, UIManager allowed).

## Development Prompts and Workflow
This project is built using a structured set of development prompts, strictly divided into 5 phases.
Please review the complete prompt set located at `Docs/GameDesign.md` in the project root (`D:\UnityGit\DDaeisComing\Docs\GameDesign.md`).

When instructed to work on the project, please ensure you do the following:
1. Confirm with the user which phase (0-5) or sub-phase is currently being implemented.
2. Read the corresponding section in `Docs/GameDesign.md`.
3. Generate the required C# Unity scripts based precisely on the instructions, constraints, and architecture outlined for that phase.
4. For any issue, keep the context in mind. For example, if there is a bug, remember that GameObject Pools must be used, or that `Update` loops are located in the Manager classes rather than the Instance classes.
5. Provide the code ensuring it strictly complies with the Phase instructions.

## Key Rules to Always Follow
- **Strict Data Separation**: Use ScriptableObjects for all numerical values. Do not hardcode values in MonoBehaviour components.
- **Pooling**: Always use `UnityEngine.Pool` for Customers and dynamic UI elements. No real-time `Instantiate` after initialization.
- **Event-Driven UI**: UI should only update by listening to events emitted by the managers. No polling.
- **Save System**: Ensure any manager data that needs saving implements an `ISaveable` interface for easy JSON serialization.
- Do not create singletons for Managers unless explicitly mentioned (e.g. GameManager).
