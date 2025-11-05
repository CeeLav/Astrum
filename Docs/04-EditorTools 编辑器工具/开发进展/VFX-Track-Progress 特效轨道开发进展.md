# 特效轨道开发进展

> 📊 **当前版本**: v0.0.0  
> 📅 **最后更新**: 2025-01-19  
> 👤 **负责人**: AI Assistant  
> 📖 **技术文档**: [特效轨道设计](../技能动作编辑器/VFX-Track-Design%20特效轨道设计.md)

## TL;DR（四象限）

- **状态/进度**：📝 设计完成，待开发
- **已达成**：技术方案设计完成，JSON格式统一，Logic/View层架构明确
- **风险/阻塞**：需重构现有触发帧数据结构（字符串→JSON），需手动转换表格数据
- **下一步**：开始Phase 1 - 数据结构重构

---

## 版本历史

### v0.0.0 - 设计阶段 (2025-01-19)
**状态**: 📝 策划案完成

**完成内容**:
- [x] 技术方案设计文档完成
- [x] 统一触发帧数据结构设计（JSON格式）
- [x] Logic层到View层通信架构设计
- [x] 编辑器预览架构设计
- [x] 数据序列化格式确定

**待完成**:
- [ ] Phase 1: 数据结构重构
- [ ] Phase 2: Logic层实现
- [ ] Phase 3: View层实现
- [ ] Phase 4: 编辑器集成

**预计工时**: 15-18小时

---

## 当前阶段

**阶段名称**: Phase 0 - 设计完成

**完成度**: 100%

**下一步计划**:
1. Phase 1: 数据结构重构（3-4小时）
   - 重构 TriggerFrameData 支持统一JSON格式
   - 更新 SkillActionEditorData 同步方法
   - 手动转换现有表格数据为JSON格式
2. Phase 2: Logic层实现（4-5小时）
   - 实现 VFXTriggerEventData 事件
   - 扩展 SkillExecutorCapability 处理VFX触发
   - 实现JSON解析逻辑
3. Phase 3: View层实现（4-5小时）
   - 实现 VFXManager
   - 实现特效加载和播放逻辑
   - 实现特效生命周期管理
4. Phase 4: 编辑器集成（4-4小时）
   - 实现 VFXPreviewManager
   - 集成到 AnimationPreviewModule
   - 实现编辑器预览功能

---

## 阶段划分

### Phase 0: 技术方案设计 ✅

**状态**: ✅ 完成  
**完成日期**: 2025-01-19

**完成内容**:
- ✅ 统一触发帧数据结构设计（SkillEffect/VFX/SFX统一JSON格式）
- ✅ Logic层到View层通信架构（事件驱动）
- ✅ VFXManager设计（View层，使用EntityView）
- ✅ 编辑器预览架构设计
- ✅ 数据序列化格式确定
- ✅ 关键决策记录（ADR）

**相关文档**:
- [特效轨道设计](../技能动作编辑器/VFX-Track-Design%20特效轨道设计.md)

---

### Phase 1: 数据结构重构 📝

**状态**: 📝 计划中  
**预计工时**: 3-4小时

**任务清单**:
- [ ] 重构 `TriggerFrameData` 类
  - [ ] 支持统一JSON格式
  - [ ] 添加 `Type` 字段（SkillEffect/VFX/SFX）
  - [ ] 添加 `TriggerType` 字段（SkillEffect内部）
  - [ ] 支持VFX字段（ResourcePath、PositionOffset等）
- [ ] 更新 `SkillActionEditorData`
  - [ ] 重构 `SyncFromTimelineEvents` 方法
  - [ ] 支持所有轨道类型（SkillEffect/VFX/SFX）
  - [ ] 实现 `BuildTimelineFromTriggerFrames` 方法
- [ ] 更新运行时解析
  - [ ] 更新 `SkillConfig.ParseTriggerFrames` 支持JSON格式
  - [ ] 移除旧字符串格式解析逻辑
- [ ] 手动转换表格数据
  - [ ] 将现有 CSV 中的 triggerFrames 字段从字符串格式转换为JSON格式
  - [ ] 验证数据转换正确性

**文件清单**:
- `AstrumProj/Assets/Script/Editor/RoleEditor/Data/SkillActionEditorData.cs` - 重构
- `AstrumProj/Assets/Script/AstrumLogic/Managers/SkillConfig.cs` - 扩展
- `AstrumProj/Assets/Script/AstrumLogic/SkillSystem/TriggerFrameInfo.cs` - 可能需要扩展

---

### Phase 2: Logic层实现 📝

**状态**: 📝 计划中  
**预计工时**: 4-5小时

**任务清单**:
- [ ] 创建 `VFXTriggerEventData` 事件类
  - [ ] 位置：`AstrumProj/Assets/Script/CommonBase/Events/` 或 `AstrumView/Events/`
  - [ ] 包含所有VFX参数（ResourcePath、PositionOffset等）
- [ ] 扩展 `SkillExecutorCapability`
  - [ ] 实现 `ProcessVFXTriggers` 方法
  - [ ] 解析VFX触发帧数据
  - [ ] 发布 `VFXTriggerEventData` 事件
- [ ] 实现JSON解析逻辑
  - [ ] 解析统一JSON格式的triggerFrames
  - [ ] 按类型分发处理（SkillEffect/VFX/SFX）
  - [ ] 支持单帧和多帧范围

**文件清单**:
- `AstrumProj/Assets/Script/CommonBase/Events/VFXTriggerEventData.cs` - 新建
- `AstrumProj/Assets/Script/AstrumLogic/Capabilities/SkillExecutorCapability.cs` - 扩展

---

### Phase 3: View层实现 📝

**状态**: 📝 计划中  
**预计工时**: 4-5小时

**任务清单**:
- [ ] 创建 `VFXManager`
  - [ ] 位置：`AstrumProj/Assets/Script/AstrumView/Managers/VFXManager.cs`
  - [ ] 继承 `Singleton<VFXManager>`
  - [ ] 订阅 `VFXTriggerEventData` 事件
- [ ] 实现特效加载和播放
  - [ ] 实现资源加载逻辑（Prefab）
  - [ ] 实现特效实例化
  - [ ] 实现参数设置（位置、旋转、缩放等）
  - [ ] 实现绑定到EntityView
- [ ] 实现特效生命周期管理
  - [ ] 实现特效更新逻辑（跟随角色）
  - [ ] 实现特效停止和清理
  - [ ] 管理活跃特效字典

**文件清单**:
- `AstrumProj/Assets/Script/AstrumView/Managers/VFXManager.cs` - 新建

---

### Phase 4: 编辑器集成 📝

**状态**: 📝 计划中  
**预计工时**: 4小时

**任务清单**:
- [ ] 创建 `VFXPreviewManager`
  - [ ] 位置：`AstrumProj/Assets/Script/Editor/RoleEditor/Modules/VFXPreviewManager.cs`
  - [ ] 管理编辑器中的特效预览
- [ ] 扩展 `AnimationPreviewModule`
  - [ ] 集成 `VFXPreviewManager`
  - [ ] 监听时间轴播放头位置
  - [ ] 检测VFX事件触发
  - [ ] 实现特效预览播放
- [ ] 实现编辑器预览功能
  - [ ] 加载特效资源
  - [ ] 实例化特效
  - [ ] 设置参数
  - [ ] 清理已结束的特效

**文件清单**:
- `AstrumProj/Assets/Script/Editor/RoleEditor/Modules/VFXPreviewManager.cs` - 新建
- `AstrumProj/Assets/Script/Editor/RoleEditor/Modules/AnimationPreviewModule.cs` - 扩展

---

## 依赖系统状态

### ✅ 已就绪的依赖

- **技能动作编辑器**: ✅ 基础框架完成
  - ✅ TimelineEditorModule
  - ✅ VFXTrackRenderer（已有）
  - ✅ AnimationPreviewModule
- **事件系统**: ✅ 完成
  - ✅ EventSystem（CommonBase）
  - ✅ 事件订阅/发布机制
- **EntityView系统**: ✅ 完成
  - ✅ EntityView基类
  - ✅ Stage管理
  - ✅ EntityViewFactory

---

## 技术债务

无

---

## 验收标准

### Phase 1 验收
- [ ] 能够解析JSON格式的triggerFrames
- [ ] 表格数据已转换为JSON格式
- [ ] 编辑器能够正确同步时间轴事件到triggerFrames

### Phase 2 验收
- [ ] Logic层能够正确解析VFX触发帧
- [ ] 能够发布VFXTriggerEventData事件
- [ ] 事件数据包含所有必要参数

### Phase 3 验收
- [ ] View层能够接收VFX事件
- [ ] 能够正确加载和播放特效
- [ ] 特效能够正确绑定到EntityView
- [ ] 特效生命周期管理正确

### Phase 4 验收
- [ ] 编辑器能够预览特效
- [ ] 特效预览与时间轴同步
- [ ] 特效参数能够正确应用

---

*文档版本：v0.0.0*  
*创建时间：2025-01-19*  
*最后更新：2025-01-19*  
*状态：设计完成，待开发*

