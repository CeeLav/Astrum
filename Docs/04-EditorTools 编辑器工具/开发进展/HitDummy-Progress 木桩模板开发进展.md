# 木桩模板开发进展

> 📊 **当前版本**: v0.1.0  
> 📅 **最后更新**: 2025-11-07  
> 👤 **负责人**: Lavender Dev Tools  
> 📖 **技术文档**: [木桩模板系统设计](../技能动作编辑器/Skill-HitDummy-Design%20木桩模板设计.md)

## TL;DR（四象限）

- **状态/进度**：📝 策划案完成，待实现
- **已达成**：完成模板架构与交互设计；定义模板、木桩交互数据结构；明确时间轴桥接流程
- **风险/阻塞**：木桩与击退/特效联动依赖 TriggerFrameData 击退参数；需评估现有碰撞数据是否足够计算命中方向
- **下一步**：进入 Phase 1，落地模板数据资产与编辑器 UI；搭建 HitDummyPreviewController 骨架

---

## 版本历史

### v0.1.0 - 设计阶段 (2025-11-07)
**状态**: 📝 策划案完成

**完成内容**:
- [x] 木桩模板系统设计文档发布
- [x] 明确模板集合 ScriptableObject 结构
- [x] 定义 HitDummyPreviewController、InteractionEvaluator 职责
- [x] 梳理击退与特效交互流程
- [x] 列出目标改动文件及依赖

**待完成**:
- [ ] Phase 1: 模板基础设施
- [ ] Phase 2: 预览集成
- [ ] Phase 3: 交互评估器
- [ ] Phase 4: 调试工具与打磨

**预计工时**: 12-15 小时

---

## 当前阶段

**阶段名称**: Phase 0 - 设计完成

**完成度**: 100%

**下一步计划**:
1. Phase 1: 模板基础设施（约 3 小时）
   - 创建模板集合资产及加载服务
   - 编辑器配置面板接入模板选择
   - 提供模板增删改 UI（Odin + ReorderableList）
2. Phase 2: 预览集成（约 4 小时）
   - 实现 HitDummyPreviewController
   - 在 AnimationPreviewModule 中实例化/刷新木桩
   - 提供木桩 Gizmos 与状态显示
3. Phase 3: 交互评估器（约 4 小时）
   - 实现 HitDummyInteractionEvaluator
   - 解析 TriggerFrameData 的击退/特效数据
   - 调用木桩 Marker 接口应用击退与命中特效
4. Phase 4: 调试工具与打磨（约 2-3 小时）
   - 命中详情面板、击退数值展示
   - 木桩状态重置、模板导入导出
   - 联合测试与文档补充

---

## 阶段划分

### Phase 0: 设计 ✅

**状态**: ✅ 完成  
**完成日期**: 2025-11-07

**完成内容**:
- ✅ 发布木桩模板系统设计文档
- ✅ 明确模板/木桩数据结构与默认值
- ✅ 设计 HitDummyPreviewController + InteractionEvaluator 协作流程
- ✅ 定义与 Timeline/VFXPreviewManager 的联动接口

**相关文档**:
- [木桩模板系统设计](../技能动作编辑器/Skill-HitDummy-Design%20木桩模板设计.md)

---

### Phase 1: 模板基础设施 📝

**状态**: 📝 计划中  
**预计工时**: 3 小时

**任务清单**:
- [ ] 新建 `SkillHitDummyTemplateCollection` ScriptableObject
- [ ] 实现 `SkillHitDummyTemplateService` 读写逻辑
- [ ] 在配置面板新增模板选择与编辑入口
- [ ] 提供模板增删改、Placement 列表编辑

**文件清单**:
- `Assets/Script/Editor/RoleEditor/Data/SkillHitDummyTemplateCollection.cs`
- `Assets/Script/Editor/RoleEditor/Services/SkillHitDummyTemplateService.cs`
- `Assets/Script/Editor/RoleEditor/Modules/SkillActionConfigModule.cs`

---

### Phase 2: 预览集成 📝

**状态**: 📝 计划中  
**预计工时**: 4 小时

**任务清单**:
- [ ] 新增 `HitDummyPreviewController`，管理木桩实例
- [ ] 在 `AnimationPreviewModule` 中注入并驱动控制器
- [ ] 支持模板切换、跟随锚点、LockY 设置
- [ ] 绘制木桩 Gizmos / 命中高亮

**文件清单**:
- `Assets/Script/Editor/RoleEditor/Services/HitDummyPreviewController.cs`
- `Assets/Script/Editor/RoleEditor/Modules/AnimationPreviewModule.cs`

---

### Phase 3: 交互评估器 📝

**状态**: 📝 计划中  
**预计工时**: 4 小时

**任务清单**:
- [ ] 实现 `HitDummyInteractionEvaluator`
- [ ] 解析击退/特效相关 TriggerFrameData
- [ ] 与 `HitDummyTargetMarker`、`VFXPreviewManager` 对接
- [ ] 输出命中详情数据给 UI

**文件清单**:
- `Assets/Script/Editor/RoleEditor/Services/HitDummyInteractionEvaluator.cs`
- `Assets/Script/Editor/RoleEditor/Timeline` 相关解析扩展

---

### Phase 4: 调试工具与打磨 📝

**状态**: 📝 计划中  
**预计工时**: 2-3 小时

**任务清单**:
- [ ] 命中详情 UI（击退距离、方向、命中特效）
- [ ] 木桩状态重置、命中记录清理
- [ ] 模板导入/导出与共享提示
- [ ] 联合测试与文档整理

**文件清单**:
- `Assets/Script/Editor/RoleEditor/Windows/SkillActionEditorWindow.cs`
- `Assets/Script/Editor/RoleEditor/Modules/SkillActionConfigModule.cs`
- 其他辅助 UI/Service 类

---

## 依赖系统状态

### ✅ 已就绪
- 技能动作编辑器主框架（Timeline/预览模块）
- TriggerFrameData JSON 结构（已规划，复用现有字段）
- VFXPreviewManager、CollisionShapePreview 基础能力

### ⚠️ 待确认
- TriggerFrameData 是否已有击退距离/方向字段；若缺少需扩展
- 表格中击退参数与命中特效配置是否统一

---

## TODO List

- [ ] 完成 Phase 1 模板基础设施实现
- [ ] 完成 Phase 2 预览集成与木桩实例刷新
- [ ] 实现 Phase 3 交互评估器命中计算与击退逻辑
- [ ] 打磨 Phase 4 调试工具及导入导出流程
- [ ] 更新技术文档与使用指南，覆盖木桩调试流程

---

*文档版本：v0.1.0*  
*创建时间：2025-11-07*  
*最后更新：2025-11-07*  
*状态：设计完成，待开发*


