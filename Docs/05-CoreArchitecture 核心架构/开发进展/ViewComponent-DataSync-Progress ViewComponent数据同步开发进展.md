# ViewComponent 数据同步 - 开发进展

**项目**: ViewComponent 自动监听 BaseComponent 数据变化并同步机制  
**创建日期**: 2025-01-XX  
**最后更新**: 2025-01-XX  
**版本**: v1.0  
**技术方案**: [ViewComponent-DataSync-Design ViewComponent数据同步设计.md](../逻辑渲染分离/ViewComponent-DataSync-Design%20ViewComponent数据同步设计.md)

---

## 📋 目录

1. [开发状态总览](#开发状态总览)
2. [阶段划分](#阶段划分)
3. [详细任务清单](#详细任务清单)
4. [技术债务](#技术债务)
5. [测试计划](#测试计划)

---

## 开发状态总览

### 当前版本
- **版本号**: v1.0
- **状态**: 🟡 设计完成，开发中
- **功能完成度**: 0% (设计 100%，实现 0%)

### 阶段划分
- ✅ **Phase 0**: 技术方案设计 - **已完成**
  - ✅ 架构设计
  - ✅ 脏标记机制设计
  - ✅ 数据流设计
  - ✅ 文档编写
- ⏳ **Phase 1**: Entity 脏标记管理 - **待开发**
  - ⏳ 添加脏组件 ID 集合
  - ⏳ 实现 MarkComponentDirty 方法
  - ⏳ 实现 GetDirtyComponentIds 方法
  - ⏳ 实现 GetDirtyComponents 方法
  - ⏳ 实现 ClearDirtyComponents 方法
  - ⏳ 实现 GetComponentById 方法
- ⏳ **Phase 2**: Stage 查询处理 - **待开发**
  - ⏳ 在 Update 中添加 SyncDirtyComponents 调用
  - ⏳ 实现 SyncDirtyComponents 方法
- ⏳ **Phase 3**: ViewComponent 增强 - **待开发**
  - ⏳ 添加 GetWatchedComponentIds 方法
  - ⏳ 添加 SyncDataFromComponent 方法
- ⏳ **Phase 4**: EntityView 协调机制 - **待开发**
  - ⏳ 添加 ComponentId 到 ViewComponent 映射
  - ⏳ 实现注册/取消注册方法
  - ⏳ 实现 SyncDirtyComponents 方法
- ⏳ **Phase 5**: 现有 ViewComponent 迁移 - **待开发**
  - ⏳ 迁移 HealthViewComponent
  - ⏳ 迁移 HUDViewComponent
  - ⏳ 其他 ViewComponent 评估
- ⏳ **Phase 6**: 现有 BaseComponent 迁移 - **待开发**
  - ⏳ 在关键组件中添加脏标记调用
  - ⏳ 测试验证
- ⏳ **Phase 7**: 测试与优化 - **待开发**
  - ⏳ 单元测试
  - ⏳ 集成测试
  - ⏳ 性能测试

---

## 阶段划分

### Phase 0: 技术方案设计 ✅

**目标**: 完成技术方案设计和文档编写

**完成内容**:
- ✅ 分析当前 ViewComponent.OnSyncData 接口问题
- ✅ 设计脏标记机制
- ✅ 设计 Entity 统一管理方案
- ✅ 设计 Stage 查询处理方案
- ✅ 设计 ViewComponent 声明机制
- ✅ 设计 EntityView 协调机制
- ✅ 完成技术设计文档

**文档**:
- `ViewComponent-DataSync-Design ViewComponent数据同步设计.md`

---

### Phase 1: Entity 脏标记管理 ⏳

**目标**: 在 Entity 中实现脏标记管理功能

#### 1.1 添加脏组件 ID 集合

**文件**: `AstrumProj/Assets/Script/AstrumLogic/Core/Entity.cs`

**任务**:
- 添加 `HashSet<int> _dirtyComponentIds` 字段

**状态**: ⏳ 待开发

#### 1.2 实现 MarkComponentDirty 方法

**文件**: `AstrumProj/Assets/Script/AstrumLogic/Core/Entity.cs`

**任务**:
- 实现 `MarkComponentDirty(int componentId)` 方法
- 将 ComponentId 添加到脏组件集合

**状态**: ⏳ 待开发

#### 1.3 实现查询方法

**文件**: `AstrumProj/Assets/Script/AstrumLogic/Core/Entity.cs`

**任务**:
- 实现 `GetDirtyComponentIds()` 方法
- 实现 `GetComponentById(int componentId)` 方法
- 实现 `GetDirtyComponents()` 方法
- 实现 `ClearDirtyComponents()` 方法

**状态**: ⏳ 待开发

---

### Phase 2: Stage 查询处理 ⏳

**目标**: 在 Stage 中实现脏组件查询和处理

#### 2.1 在 Update 中添加调用

**文件**: `AstrumProj/Assets/Script/AstrumView/Core/Stage.cs`

**任务**:
- 在 `Update()` 方法中添加 `SyncDirtyComponents()` 调用

**状态**: ⏳ 待开发

#### 2.2 实现 SyncDirtyComponents 方法

**文件**: `AstrumProj/Assets/Script/AstrumView/Core/Stage.cs`

**任务**:
- 遍历所有 Entity，查询脏组件 ID
- 对于有脏组件的 Entity，通知对应的 EntityView
- 同步完成后清除脏标记

**状态**: ⏳ 待开发

---

### Phase 3: ViewComponent 增强 ⏳

**目标**: 在 ViewComponent 中添加监听声明和数据同步方法

#### 3.1 添加 GetWatchedComponentIds 方法

**文件**: `AstrumProj/Assets/Script/AstrumView/Components/ViewComponent.cs`

**任务**:
- 添加 `GetWatchedComponentIds()` 虚方法
- 默认返回 null

**状态**: ⏳ 待开发

#### 3.2 添加 SyncDataFromComponent 方法

**文件**: `AstrumProj/Assets/Script/AstrumView/Components/ViewComponent.cs`

**任务**:
- 添加 `SyncDataFromComponent(int componentId)` 虚方法
- 默认实现：根据 ComponentId 获取组件并调用 OnSyncData

**状态**: ⏳ 待开发

---

### Phase 4: EntityView 协调机制 ⏳

**目标**: 在 EntityView 中实现映射管理和同步协调

#### 4.1 添加映射字典

**文件**: `AstrumProj/Assets/Script/AstrumView/Core/EntityView.cs`

**任务**:
- 添加 `Dictionary<int, List<ViewComponent>> _componentIdToViewComponentsMap` 字段

**状态**: ⏳ 待开发

#### 4.2 实现注册/取消注册方法

**文件**: `AstrumProj/Assets/Script/AstrumView/Core/EntityView.cs`

**任务**:
- 实现 `RegisterViewComponentWatchedIds()` 方法
- 实现 `UnregisterViewComponentWatchedIds()` 方法
- 在 `AddViewComponent` 中调用注册方法
- 在 `RemoveViewComponent` 中调用取消注册方法

**状态**: ⏳ 待开发

#### 4.3 实现 SyncDirtyComponents 方法

**文件**: `AstrumProj/Assets/Script/AstrumView/Core/EntityView.cs`

**任务**:
- 实现 `SyncDirtyComponents(IReadOnlyCollection<int> dirtyComponentIds)` 方法
- 根据 ComponentId 查找对应的 ViewComponent
- 调用 ViewComponent 的同步方法

**状态**: ⏳ 待开发

---

### Phase 5: 现有 ViewComponent 迁移 ⏳

**目标**: 迁移现有 ViewComponent 使用新机制

#### 5.1 迁移 HealthViewComponent

**文件**: `AstrumProj/Assets/Script/AstrumView/Components/HealthViewComponent.cs`

**任务**:
- 在 `OnInitialize()` 中获取需要监听的组件 ComponentId
- 实现 `GetWatchedComponentIds()` 方法
- 实现 `SyncDataFromComponent(int componentId)` 方法
- 移除 `OnUpdate` 中的主动拉取逻辑（如果存在）

**状态**: ⏳ 待开发

#### 5.2 迁移 HUDViewComponent

**文件**: `AstrumProj/Assets/Script/AstrumView/Components/HUDViewComponent.cs`

**任务**:
- 在 `OnInitialize()` 中获取需要监听的组件 ComponentId
- 实现 `GetWatchedComponentIds()` 方法
- 实现 `SyncDataFromComponent(int componentId)` 方法
- 移除 `OnUpdate` 中的主动拉取逻辑

**状态**: ⏳ 待开发

#### 5.3 其他 ViewComponent 评估

**文件**: 其他 ViewComponent 文件

**任务**:
- 评估其他 ViewComponent 是否需要迁移
- 对于频繁变化的组件（如 TransViewComponent），保持现有主动拉取方式

**状态**: ⏳ 待开发

---

### Phase 6: 现有 BaseComponent 迁移 ⏳

**目标**: 在关键 BaseComponent 中添加脏标记调用

#### 6.1 迁移 DynamicStatsComponent

**文件**: `AstrumProj/Assets/Script/AstrumLogic/Components/DynamicStatsComponent.cs`

**任务**:
- 在 `Set()` 方法中，对于重要资源变化（如血量），调用 Entity.MarkComponentDirty
- 需要通过 EntityId 获取 Entity（具体方式待确定）

**状态**: ⏳ 待开发

#### 6.2 其他关键组件迁移

**文件**: 其他关键 BaseComponent 文件

**任务**:
- 识别需要脏标记的关键组件
- 在数据变化时调用 Entity.MarkComponentDirty

**状态**: ⏳ 待开发

---

### Phase 7: 测试与优化 ⏳

**目标**: 完成测试和性能优化

#### 7.1 单元测试

**任务**:
- 测试 Entity 脏标记管理
- 测试 Stage 查询处理
- 测试 ViewComponent 同步机制
- 测试 EntityView 协调机制

**状态**: ⏳ 待开发

#### 7.2 集成测试

**任务**:
- 测试完整的数据同步流程
- 测试多个 ViewComponent 监听同一组件
- 测试多个组件同时变脏的情况

**状态**: ⏳ 待开发

#### 7.3 性能测试

**任务**:
- 测试脏标记查询性能
- 测试批量同步性能
- 优化性能瓶颈

**状态**: ⏳ 待开发

---

## 详细任务清单

### Phase 1: Entity 脏标记管理

- [ ] 1.1 添加 `HashSet<int> _dirtyComponentIds` 字段
- [ ] 1.2 实现 `MarkComponentDirty(int componentId)` 方法
- [ ] 1.3 实现 `GetDirtyComponentIds()` 方法
- [ ] 1.4 实现 `GetComponentById(int componentId)` 方法
- [ ] 1.5 实现 `GetDirtyComponents()` 方法
- [ ] 1.6 实现 `ClearDirtyComponents()` 方法
- [ ] 1.7 在 `RemoveComponent` 中清理脏标记

### Phase 2: Stage 查询处理

- [ ] 2.1 在 `Update()` 方法中添加 `SyncDirtyComponents()` 调用
- [ ] 2.2 实现 `SyncDirtyComponents()` 私有方法
- [ ] 2.3 遍历所有 Entity，查询脏组件 ID
- [ ] 2.4 通知 EntityView 同步脏组件
- [ ] 2.5 清除 Entity 的脏标记

### Phase 3: ViewComponent 增强

- [ ] 3.1 添加 `GetWatchedComponentIds()` 虚方法（默认返回 null）
- [ ] 3.2 添加 `SyncDataFromComponent(int componentId)` 虚方法

### Phase 4: EntityView 协调机制

- [ ] 4.1 添加 `Dictionary<int, List<ViewComponent>> _componentIdToViewComponentsMap` 字段
- [ ] 4.2 实现 `RegisterViewComponentWatchedIds()` 方法
- [ ] 4.3 实现 `UnregisterViewComponentWatchedIds()` 方法
- [ ] 4.4 在 `AddViewComponent` 中调用注册方法
- [ ] 4.5 在 `RemoveViewComponent` 中调用取消注册方法
- [ ] 4.6 实现 `SyncDirtyComponents(IReadOnlyCollection<int> dirtyComponentIds)` 方法
- [ ] 4.7 在 `Destroy()` 中清理映射关系

### Phase 5: 现有 ViewComponent 迁移

- [ ] 5.1 迁移 HealthViewComponent
- [ ] 5.2 迁移 HUDViewComponent
- [ ] 5.3 评估其他 ViewComponent

### Phase 6: 现有 BaseComponent 迁移

- [ ] 6.1 迁移 DynamicStatsComponent
- [ ] 6.2 评估其他关键组件

### Phase 7: 测试与优化

- [ ] 7.1 编写单元测试
- [ ] 7.2 编写集成测试
- [ ] 7.3 性能测试和优化

---

## 技术债务

### 待解决

1. **BaseComponent 如何通过 EntityId 获取 Entity**
   - 问题：BaseComponent 需要通过 EntityId 获取 Entity 来调用 MarkComponentDirty
   - 方案：需要确定通过什么方式获取 Entity（World 管理器、Entity 管理器等）
   - 优先级：高

2. **ComponentId 的生成方式**
   - 问题：当前 BaseComponent.ComponentId 是静态的，需要确认是否正确
   - 方案：需要确认 ComponentId 是否为实例属性
   - 优先级：高

---

## 测试计划

### 单元测试

1. **Entity 脏标记管理测试**
   - 测试 MarkComponentDirty 方法
   - 测试 GetDirtyComponentIds 方法
   - 测试 GetDirtyComponents 方法
   - 测试 ClearDirtyComponents 方法
   - 测试 GetComponentById 方法

2. **ViewComponent 同步测试**
   - 测试 GetWatchedComponentIds 方法
   - 测试 SyncDataFromComponent 方法

3. **EntityView 协调测试**
   - 测试注册/取消注册方法
   - 测试 SyncDirtyComponents 方法

### 集成测试

1. **完整数据同步流程测试**
   - BaseComponent 数据变化 → Entity 记录脏标记 → Stage 查询 → EntityView 同步 → ViewComponent 更新

2. **多 ViewComponent 监听测试**
   - 多个 ViewComponent 监听同一组件
   - 同一 ViewComponent 监听多个组件

3. **批量同步测试**
   - 多个组件同时变脏
   - 多个 Entity 同时有脏组件

### 性能测试

1. **脏标记查询性能**
   - 测试大量 Entity 时的查询性能
   - 测试大量脏组件时的处理性能

2. **同步性能**
   - 测试同步调用的性能开销
   - 对比主动拉取和脏标记机制的性能差异

---

**返回**: [核心架构文档](../README.md)

