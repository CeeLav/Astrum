# 技能位移系统 - 开发进展

**项目**: 技能位移系统（Root Motion + 视觉跟随）  
**创建日期**: 2025-01-16  
**最后更新**: 2025-01-16  
**版本**: v0.0.0 (准备开发)

---

## 📋 目录

1. [开发状态总览](#开发状态总览)
2. [依赖系统状态](#依赖系统状态)
3. [开发计划](#开发计划)
4. [待完成功能](#待完成功能)
5. [文件清单](#文件清单)
6. [技术债务](#技术债务)

---

## 开发状态总览

### 当前版本
- **版本号**: v0.0.0 (准备开发)
- **编译状态**: ⏳ 未开始
- **测试状态**: ⏳ 未开始
- **功能完成度**: 0% (方案设计完成，待实现)

### 阶段划分
- ⏳ **Phase 0**: 依赖系统准备 - **待确认**
  - ⏳ 动作系统（ActionComponent、ActionCapability）
  - ⏳ 配置系统（ActionConfig、SkillActionTable）
  - ⏳ 编辑器工具基础
- ⏳ **Phase 1**: 编辑器端 - 根节点位移提取 - **待开发**
  - ⏳ AnimationRootMotionExtractor 服务
  - ⏳ 数据序列化（数组格式）
  - ⏳ 编辑器窗口集成
  - ⏳ CSV 存储
- ⏳ **Phase 2**: 运行时 - SkillDisplacementCapability - **待开发**
  - ⏳ Capability 核心实现
  - ⏳ 数据加载（ActionConfig）
  - ⏳ 位移应用逻辑
  - ⏳ MemoryPack 注册
- ⏳ **Phase 3**: 视觉层 - 视觉跟随 - **待开发**
  - ⏳ TransViewComponent 扩展
  - ⏳ 逻辑帧插值机制
  - ⏳ AnimationViewComponent 集成
  - ⏳ 调试可视化
- ⏳ **Phase 4**: 集成测试与优化 - **待开发**
  - ⏳ 端到端测试
  - ⏳ 性能测试
  - ⏳ 边界情况测试

---

## 依赖系统状态

### ✅ 已就绪的依赖

#### 1. 动作系统
- **状态**: ✅ 完成
- **完成度**: 高
- **关键组件**:
  - ✅ `ActionComponent` - 动作状态管理
  - ✅ `ActionCapability` - 动作执行能力
  - ✅ `ActionInfo` / `SkillActionInfo` - 动作信息
  - ✅ `ActionConfig` - 配置管理器

**可用功能**:
- ✅ 动作切换和状态管理
- ✅ 帧索引追踪
- ✅ 技能动作识别（`SkillActionInfo`）
- ✅ 动作配置加载

**影响评估**: ✅ **完全就绪，可直接使用**

#### 2. 配置系统
- **状态**: ✅ 就绪
- **完成度**: 高
- **关键表**:
  - ✅ `SkillActionTable` - 技能动作配置表
  - ✅ `ActionTable` - 动作基础表
  - ✅ `TableConfig` - 配置管理器

**可用功能**:
- ✅ CSV 读取和解析
- ✅ 配置数据加载
- ✅ 字符串字段支持

**影响评估**: ✅ **就绪，需要扩展 SkillActionTable 添加 `root_motion_data` 字段**

#### 3. 实体组件系统
- **状态**: ✅ 完成
- **完成度**: 高
- **关键组件**:
  - ✅ `TransComponent` - 位置和旋转组件（定点数）
  - ✅ `Entity` - 实体基础类
  - ✅ `Capability` - 能力基类

**可用功能**:
- ✅ 组件查询和管理
- ✅ 定点数位置更新
- ✅ 物理世界同步接口

**影响评估**: ✅ **完全就绪**

#### 4. 视图系统
- **状态**: ✅ 完成
- **完成度**: 中
- **关键组件**:
  - ✅ `EntityView` - 视图实体
  - ✅ `TransViewComponent` - 移动视图组件
  - ✅ `AnimationViewComponent` - 动画视图组件

**可用功能**:
- ✅ 视觉位置更新
- ✅ 动画播放
- ✅ Animator 访问

**待扩展功能**:
- ⏳ `TransViewComponent` 视觉跟随逻辑
- ⏳ `AnimationViewComponent.GetAnimator()` 方法

**影响评估**: 🟡 **需要扩展以支持视觉跟随**

#### 5. 物理系统
- **状态**: ✅ 完成
- **完成度**: 高
- **关键功能**:
  - ✅ `HitSystem.UpdateEntityPosition()` - 物理世界同步

**影响评估**: ✅ **完全就绪**

### ⏳ 待准备的依赖

#### 1. 编辑器工具扩展
- **状态**: ⏳ 需要开发
- **需求**: 
  - 根节点位移提取服务
  - 编辑器窗口集成
  - 数据预览和可视化
- **优先级**: 🔴 高 - Phase 1 核心功能

#### 2. SkillActionTable 字段扩展
- **状态**: ⏳ 需要添加
- **需求**: 
  - 添加 `root_motion_data` 字段（string 类型）
  - 更新 CSV 表结构
  - 更新数据映射类
- **优先级**: 🔴 高 - Phase 1 阻塞项

#### 3. AnimationRootMotionData 运行时结构
- **状态**: ⏳ 需要创建
- **需求**: 
  - 运行时数据结构（定点数）
  - MemoryPack 序列化支持
- **优先级**: 🔴 高 - Phase 2 需要

---

## 开发计划

### Phase 1: 编辑器端 - 根节点位移提取（预计 3-4 天）

#### 目标
实现编辑器端的动画根节点位移数据提取、序列化和存储功能。

#### 待开发内容

##### 1.1 数据结构定义
- [ ] **编辑器端数据结构**（`AnimationRootMotionData.cs`）
  - [ ] `AnimationRootMotionData` 类（Serializable）
  - [ ] `RootMotionFrameData` 类（Serializable）
  - [ ] `RelativePosition` / `RelativeRotation` 字段（浮点数）
  - [ ] `DeltaPosition` / `DeltaRotation` 字段（浮点数）
  - [ ] `HasMotion` 属性

- [ ] **运行时数据结构**（`AnimationRootMotionData.cs` - Runtime 命名空间）
  - [ ] `AnimationRootMotionData` 类（MemoryPackable）
  - [ ] `RootMotionFrameData` 类（MemoryPackable）
  - [ ] `RelativePosition` / `RelativeRotation` 字段（定点数 TSVector/TSQuaternion）
  - [ ] `DeltaPosition` / `DeltaRotation` 字段（定点数）
  - [ ] `HasMotion` 属性

##### 1.2 提取服务实现
- [ ] **AnimationRootMotionExtractor.cs**
  - [ ] `ExtractRootMotion()` - 从 AnimationClip 提取位移数据
  - [ ] 20 FPS 采样逻辑（50ms/帧）
  - [ ] 局部空间位移和旋转计算
  - [ ] 增量位移计算（DeltaPosition/DeltaRotation）
  - [ ] 临时 GameObject 创建和清理
  - [ ] `SerializeToArrayString()` - 序列化为数组字符串（整型*1000）
  - [ ] `DeserializeFromArrayString()` - 从数组字符串反序列化
  - [ ] `ConvertToRuntime()` - 浮点数转定点数（备选）
  - [ ] `ConvertToRuntimeFromArrayString()` - 整型直接转定点数（推荐）

##### 1.3 编辑器集成
- [ ] **SkillActionEditorData.cs 扩展**
  - [ ] 添加 `RootMotionData` 字段（编辑器端数据）
  - [ ] 添加 `RootMotionDataString` 字段（用于保存到 CSV）

- [ ] **SkillActionEditorWindow.cs 修改**
  - [ ] `LoadAnimationForAction()` 方法集成提取逻辑
  - [ ] 自动提取根节点位移数据
  - [ ] 序列化并保存到 `RootMotionDataString`
  - [ ] 日志输出数据大小和帧数

##### 1.4 配置表扩展
- [ ] **SkillActionTableData.cs 扩展**
  - [ ] 添加 `RootMotionData` 字段（string 类型）
  - [ ] 更新 `GetTableConfig()` 方法

- [ ] **CSV 表结构更新**
  - [ ] 在 `#SkillActionTable.csv` 中添加 `root_motion_data` 列
  - [ ] 更新表头信息

- [ ] **SkillActionDataWriter.cs 修改**
  - [ ] 保存时写入 `root_motion_data` 字段
  - [ ] 从 `RootMotionDataString` 读取数据

##### 1.5 数据预览（可选）
- [ ] **编辑器预览功能**
  - [ ] 位移轨迹可视化（Gizmos）
  - [ ] 网格对齐显示
  - [ ] 速度曲线显示

#### 验收标准
- [ ] 可以提取动画的根节点位移数据
- [ ] 数据正确序列化为数组格式（整型*1000）
- [ ] 数据可以保存到 CSV 配置表
- [ ] 数据可以从 CSV 正确读取并反序列化
- [ ] 编辑器窗口可以自动提取并显示数据信息
- [ ] 单元测试覆盖核心提取逻辑

**参考文档**: [动画根节点位移提取方案](../04-EditorTools%20编辑器工具/技能动作编辑器/动画根节点位移提取方案.md)

---

### Phase 2: 运行时 - SkillDisplacementCapability（预计 2-3 天）

#### 目标
实现运行时技能位移能力，自动应用根节点位移数据到实体位置。

#### 待开发内容

##### 2.1 配置加载
- [ ] **ActionConfig.cs 修改**
  - [ ] 在 `GetAction()` 方法中添加 RootMotionData 加载
  - [ ] 使用 `AnimationRootMotionExtractor.ConvertToRuntimeFromArrayString()`
  - [ ] 从 `SkillActionTable.RootMotionData` 字段读取
  - [ ] 错误处理和日志记录
  - [ ] 将数据填充到 `SkillActionInfo.RootMotionData`

##### 2.2 Capability 实现
- [ ] **SkillDisplacementCapability.cs**
  - [ ] 类定义和 MemoryPackable 特性
  - [ ] 构造函数（Priority = 150）
  - [ ] `Initialize()` 方法
  - [ ] `Tick()` 方法
    - [ ] 获取 `ActionComponent`
    - [ ] 检查当前动作是否为 `SkillActionInfo`
    - [ ] 检查是否有 `RootMotionData`
    - [ ] 调用 `ApplyRootMotion()`
  - [ ] `ApplyRootMotion()` 方法
    - [ ] 帧索引有效性检查
    - [ ] 读取当前帧的位移数据
    - [ ] 获取 `TransComponent`
    - [ ] 局部空间转世界空间（`TransformDeltaToWorld()`）
    - [ ] 更新 `TransComponent.Position`
    - [ ] 更新 `TransComponent.Rotation`（如果包含旋转）
    - [ ] 物理世界同步（`HitSystem.UpdateEntityPosition()`）
  - [ ] `TransformDeltaToWorld()` 方法
  - [ ] `CanExecute()` 方法
  - [ ] `IsDisplacementActive()` 方法（运行时获取）
  - [ ] `GetCurrentSkillActionId()` 方法（运行时获取）

##### 2.3 MemoryPack 注册
- [ ] **Capability.MemoryPack.cs 修改**
  - [ ] 添加 `[MemoryPackUnion(8, typeof(SkillDisplacementCapability))]`

##### 2.4 Archetype 集成
- [ ] **角色 Archetype 修改**
  - [ ] 在 `RoleArchetype` 或相关 Archetype 中添加 `SkillDisplacementCapability`
  - [ ] 确保在 `ActionCapability` 之后初始化

#### 验收标准
- [ ] 可以正确加载根节点位移数据（从配置表）
- [ ] 每逻辑帧自动检查并应用位移
- [ ] 位移方向正确（局部空间转世界空间）
- [ ] 旋转正确应用（如果动画包含旋转）
- [ ] 物理世界正确同步
- [ ] 动作切换时自动停止应用位移
- [ ] 单元测试覆盖核心逻辑
- [ ] 编译通过，无运行时错误

**参考文档**: [SkillDisplacementCapability 技能位移能力](../移动-位移/SkillDisplacementCapability%20技能位移能力.md)

---

### Phase 3: 视觉层 - 视觉跟随（预计 3-4 天）

#### 目标
实现视觉层的平滑跟随机制，使技能动画在视觉上流畅自然，避免长期误差累积。

#### 待开发内容

##### 3.1 TransViewComponent 扩展
- [ ] **TransViewComponent.cs 修改**
  - [ ] 添加视觉跟随相关字段
    - [ ] `motionBlendWeight` - 动画Root感强度（0.5f）
    - [ ] `maxVisualOffset` - 最大视觉偏移（0.5f）
    - [ ] `enableVisualFollow` - 是否启用视觉跟随（true）
  - [ ] 添加 `VisualSyncData` 结构体
    - [ ] `lastLogicPos` - 上一逻辑帧位置
    - [ ] `previousLogicPos` - 上上一逻辑帧位置
    - [ ] `visualOffset` - 视觉偏移
    - [ ] `timeSinceLastLogicUpdate` - 累积时间
  - [ ] 修改 `OnUpdate()` 方法
    - [ ] 添加视觉跟随模式判断
    - [ ] 调用 `UpdateVisualFollow()` 或 `UpdateSmoothMovement()`
  - [ ] 实现 `UpdateVisualFollow()` 方法
    - [ ] 获取当前逻辑位置（定点数转浮点数）
    - [ ] 获取动画位移（`animator.deltaPosition`）
    - [ ] 检测逻辑跳变
    - [ ] 逻辑帧插值计算
    - [ ] 累积动画偏移
    - [ ] 误差钳制
    - [ ] 应用最终视觉位置
  - [ ] 保留 `UpdateSmoothMovement()` 方法（兼容性）
  - [ ] 实现 `GetAnimator()` 方法
  - [ ] 修改 `OnInitialize()` 方法
    - [ ] 初始化 `VisualSyncData` 字段
  - [ ] 添加必要的命名空间引用
    - [ ] `using Astrum.LogicCore.FrameSync;`

##### 3.2 AnimationViewComponent 扩展
- [ ] **AnimationViewComponent.cs 修改**
  - [ ] 添加 `_animator` 私有字段
  - [ ] 在 `OnInitialize()` 中获取 Animator 引用
  - [ ] 确保 `applyRootMotion = false`
  - [ ] 实现 `GetAnimator()` 公共方法

##### 3.3 调试可视化（可选）
- [ ] **Gizmos 绘制**（Editor Only）
  - [ ] 绘制逻辑位置（绿色球体）
  - [ ] 绘制视觉位置（黄色球体）
  - [ ] 绘制偏移向量（红色线段）
  - [ ] 在 `OnDrawGizmos()` 中实现

##### 3.4 参数调优
- [ ] **Inspector 参数调整**
  - [ ] 测试不同的 `motionBlendWeight` 值
  - [ ] 测试不同的 `maxVisualOffset` 值
  - [ ] 观察视觉跟随效果

#### 验收标准
- [ ] 视觉位置平滑跟随逻辑位置
- [ ] 逻辑帧跳变时视觉平滑过渡（无突变）
- [ ] 动画位移感正确表现（通过 `motionBlendWeight` 控制）
- [ ] 误差自动消除（不会长期漂移）
- [ ] 调试可视化正常工作
- [ ] 性能开销可接受（每帧几个向量运算）
- [ ] 编译通过，无运行时错误

**参考文档**: [技能动画视觉跟随方案](../移动-位移/技能动画视觉跟随方案.md)

---

### Phase 4: 集成测试与优化（预计 2-3 天）

#### 目标
进行端到端测试，验证完整流程，优化性能和修复问题。

#### 待开发内容

##### 4.1 端到端测试
- [ ] **编辑器到运行时完整流程测试**
  - [ ] 编辑器提取位移数据
  - [ ] 数据保存到 CSV
  - [ ] 运行时加载数据
  - [ ] Capability 应用位移
  - [ ] 视觉层平滑跟随
  - [ ] 验证最终效果

##### 4.2 功能测试
- [ ] **位移正确性测试**
  - [ ] 位移方向和距离正确
  - [ ] 旋转正确应用
  - [ ] 多帧连续位移正确
  - [ ] 动作切换时位移停止

- [ ] **视觉跟随测试**
  - [ ] 逻辑帧跳变时视觉平滑
  - [ ] 动画位移感正确
  - [ ] 误差不会累积
  - [ ] 不同帧率下表现一致

##### 4.3 边界情况测试
- [ ] **边界情况处理**
  - [ ] 没有位移数据的技能动作
  - [ ] 位移数据为空
  - [ ] 帧索引越界
  - [ ] 位移数据格式错误
  - [ ] Animator 不存在
  - [ ] 动作快速切换

##### 4.4 性能测试
- [ ] **性能评估**
  - [ ] Capability Tick 开销
  - [ ] 视觉跟随计算开销
  - [ ] 内存占用（位移数据）
  - [ ] 数据加载时间

##### 4.5 优化
- [ ] **性能优化**（如需要）
  - [ ] Animator 引用缓存
  - [ ] 向量计算优化
  - [ ] 数据访问优化

- [ ] **代码优化**
  - [ ] 代码审查
  - [ ] 注释完善
  - [ ] 错误处理完善

#### 验收标准
- [ ] 所有测试用例通过
- [ ] 性能满足要求
- [ ] 边界情况处理正确
- [ ] 代码质量达标
- [ ] 文档完善

---

## 待完成功能

### 🔴 高优先级（阻塞开发）

1. **SkillActionTable 字段扩展**
   - 添加 `root_motion_data` 字段
   - 更新数据映射类
   - 更新 CSV 表结构
   - **预计时间**: 1 小时

2. **AnimationRootMotionExtractor 实现**
   - 核心提取逻辑
   - 序列化/反序列化
   - **预计时间**: 4-6 小时

3. **SkillDisplacementCapability 实现**
   - 核心位移应用逻辑
   - 配置加载集成
   - **预计时间**: 4-6 小时

4. **TransViewComponent 视觉跟随**
   - 逻辑帧插值实现
   - Animator 集成
   - **预计时间**: 4-6 小时

### 🟡 中优先级（重要功能）

1. **编辑器窗口集成**
   - 自动提取触发
   - 数据预览
   - **预计时间**: 2-3 小时

2. **调试可视化**
   - Gizmos 绘制
   - 性能监控
   - **预计时间**: 2-3 小时

3. **单元测试**
   - 核心逻辑测试
   - 边界情况测试
   - **预计时间**: 4-6 小时

### 🟢 低优先级（优化功能）

1. **位移轨迹可视化**
   - 编辑器预览窗口
   - 网格对齐显示
   - **预计时间**: 3-4 小时

2. **高级功能扩展**
   - 位移限制
   - 位移混合
   - 插值优化
   - **预计时间**: 待定

---

## 文件清单

### 编辑器端文件

#### 待创建文件
- [ ] `Assets/Script/Editor/RoleEditor/Services/AnimationRootMotionExtractor.cs`
- [ ] `Assets/Script/Editor/RoleEditor/Data/AnimationRootMotionData.cs` (编辑器端)

#### 待修改文件
- [ ] `Assets/Script/Editor/RoleEditor/Data/SkillActionEditorData.cs`
- [ ] `Assets/Script/Editor/RoleEditor/Windows/SkillActionEditorWindow.cs`
- [ ] `Assets/Script/Editor/RoleEditor/Persistence/Mappings/SkillActionTableData.cs`
- [ ] `Assets/Script/Editor/RoleEditor/Persistence/SkillActionDataWriter.cs`

### 运行时文件

#### 待创建文件
- [ ] `Assets/Script/AstrumLogic/Capabilities/SkillDisplacementCapability.cs`
- [ ] `Assets/Script/AstrumLogic/SkillSystem/AnimationRootMotionData.cs` (运行时)

#### 待修改文件
- [ ] `Assets/Script/AstrumLogic/Capabilities/Capability.MemoryPack.cs`
- [ ] `Assets/Script/AstrumLogic/Managers/ActionConfig.cs`
- [ ] `Assets/Script/AstrumLogic/Archetypes/RoleArchetype.cs` (或相关 Archetype)

### 视图层文件

#### 待修改文件
- [ ] `Assets/Script/AstrumView/Components/TransViewComponent.cs`
- [ ] `Assets/Script/AstrumView/Components/AnimationViewComponent.cs`

### 配置表文件

#### 待修改文件
- [ ] `AstrumConfig/Tables/Datas/Skill/#SkillActionTable.csv`
  - 添加 `root_motion_data` 列

---

## 技术债务

### 已知问题

1. **无**
   - 方案设计阶段，暂无已知问题

### 待优化项

1. **性能优化**
   - 评估位移数据加载时间
   - 优化数组字符串解析性能
   - 评估视觉跟随计算开销

2. **功能扩展**
   - 支持位移插值（如果需要）
   - 支持位移混合（技能位移+普通移动）
   - 支持位移限制（防止异常位移）

3. **工具完善**
   - 编辑器位移轨迹可视化
   - 编辑器参数调整工具
   - 调试工具完善

---

## 相关文档

### 核心文档
- [SkillDisplacementCapability 技能位移能力](../移动-位移/SkillDisplacementCapability%20技能位移能力.md) - Capability 完整实现方案
- [技能动画视觉跟随方案](../移动-位移/技能动画视觉跟随方案.md) - 视觉层平滑跟随方案
- [动画根节点位移提取方案](../../04-EditorTools%20编辑器工具/技能动作编辑器/动画根节点位移提取方案.md) - 编辑器端数据提取方案

### 依赖文档
- [ECC 系统说明](../../05-CoreArchitecture%20核心架构/ECC/ECC-System%20ECC结构说明.md) - 实体-组件-能力架构
- [动作系统策划案](../技能系统/Action-System%20动作系统.md) - 动作系统架构

---

**返回**: [开发进展总览](./README.md) | [战斗系统](../README.md) | [文档中心](../../../README.md)

