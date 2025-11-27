# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# / .NET 9.0 (服务器), Unity 2022.3 LTS (客户端)  
**Primary Dependencies**: Unity Engine, TrueSync FP Math, BEPU Physics v1, MemoryPack, Protocol Buffers  
**Storage**: 配置数据使用 Luban CSV 框架，运行时状态使用内存快照  
**Testing**: NUnit (单元测试), 确定性测试框架, 回滚测试框架, 性能测试工具  
**Target Platform**: Windows 10/11, macOS, Linux (客户端和服务器)  
**Project Type**: Unity 游戏客户端 + .NET 服务器 (帧同步架构)  
**Performance Goals**: 60 FPS 稳定运行，单帧执行时间 < 16.67ms，网络延迟 < 100ms  
**Constraints**: 确定性执行（必须使用定点数），禁止浮点数游戏逻辑，禁止非确定性操作，状态必须可序列化  
**Scale/Scope**: 多人游戏（2-8 玩家），ECC 架构，技能系统，物理碰撞检测

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### 确定性执行检查
- [ ] 所有数值计算是否使用定点数（FP Math）而非浮点数？
- [ ] 是否避免使用系统时间、随机数等非确定性操作？
- [ ] 所有逻辑是否基于帧数而非真实时间？

### 性能优化检查
- [ ] 单帧执行时间是否在预算范围内（目标 60 FPS）？
- [ ] 是否避免在游戏循环中进行耗时操作（IO、网络、复杂序列化）？
- [ ] 是否使用对象池管理频繁创建销毁的对象？

### ECC架构检查
- [ ] Entity 是否仅作为标识符容器？
- [ ] Component 是否仅包含纯数据，无逻辑代码？
- [ ] Capability 是否包含所有逻辑实现？
- [ ] 状态变更是否通过明确接口，而非直接修改 Component？

### 网络同步检查
- [ ] 所有游戏状态是否可序列化（使用 MemoryPack）？
- [ ] 是否实现状态回滚机制？
- [ ] 网络消息是否包含帧号，确保有序处理？

### 测试与验证检查
- [ ] 是否编写确定性单元测试？
- [ ] 是否实现回滚测试？
- [ ] 是否进行性能测试验证单帧执行时间？

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
