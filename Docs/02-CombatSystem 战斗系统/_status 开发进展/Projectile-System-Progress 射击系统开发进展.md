# 射击系统（Projectile System）- 开发进展

**项目**: 射击系统 / 抛射物系统（Action → Projectile → Raycast）  
**创建日期**: 2025-11-10  
**最后更新**: 2025-11-10  
**版本**: v0.1.0 （设计完成，开发待启动）

---

## 📋 目录

1. [开发状态总览](#开发状态总览)  
2. [依赖系统状态](#依赖系统状态)  
3. [开发计划](#开发计划)  
4. [待完成功能](#待完成功能)  
5. [文件清单](#文件清单)  
6. [技术债务](#技术债务)  
7. [更新日志](#更新日志)  
8. [相关文档](#相关文档)

---

## 开发状态总览

### 当前版本
- **版本号**: v0.1.0（设计完成，编码尚未开始）
- **编译状态**: ❌ 未实现
- **测试状态**: ❌ 未开始
- **功能完成度**: 15%（技术方案与流程设计完成）

### 阶段划分
- ✅ **Phase 0**: 技术方案与依赖梳理
  - 射击系统技术文档 (`Projectile-Shooting-System 技术设计.md`) 完成
  - 多阶段动作、弹道实体、射线碰撞、表现层同步方案确认
  - 依赖系统（Action / Skill / View / Physics）梳理完毕
- ⏳ **Phase 1**: 核心运行时（逻辑层）实现
  - ProjectileComponent / ProjectileCapability / Raycast 命中流程
  - ProjectileSpawnCapability 事件驱动体系
- ⏳ **Phase 2**: 表现层集成
  - ProjectileViewComponent（表现层追赶逻辑）
  - SocketRefs / ViewBridge 集成
- ⏳ **Phase 3**: 触发帧与配置联调
  - TriggerFrameInfo 扩展验证
  - SkillEffectTable / ProjectileTable 实际配置
- ⏳ **Phase 4**: 测试与优化
  - 端到端功能测试
  - 性能与对象池回收验证
  - 可视化 / 调试工具补充

---

## 依赖系统状态

### ✅ 已就绪的依赖

| 系统 | 状态 | 说明 |
|------|------|------|
| Action / Skill 系统 | ✅ 完成 | 支持多阶段动作、触发帧、SkillEffect 管线 |
| 技能效果运行时 | ✅ 完成 | SkillEffectManager、DamageCalculator 已可复用 |
| 物理系统 | ✅ 完成 | PhysicsWorld/HitManager 支持 Raycast、实体查询 |
| 视图系统基础 | ✅ 完成 | EntityView、ViewComponent 架构稳定，可挂载新视图组件 |

### ⏳ 待准备的依赖

| 事项 | 状态 | 说明 |
|------|------|------|
| SocketRefs 组件 | ⏳ 待实现 | 模型绑点缓存，需要在 View 端新增脚本（MonoBehaviour） |
| ViewBridge 扩展 | ⏳ 待实现 | 需支持根据实体/Socket 获取世界坐标，向 ViewComponent 注入初始位置 |
| ProjectileTable 配置 | ⏳ 待设计 | 需要定义 ProjectileDefinition 表结构、字段说明、生成流程 |
| Raycast Hit 数据结构 | ⏳ 待实现 | 需要标准化射线命中的返回结构（包含 EntityId、命中点等） |

---

## 开发计划

### Phase 1：核心运行时实现（预计 3~4 天）
- ProjectileComponent（SkillEffectIds 列表、PierceCount、LastPosition 等）
- ProjectileCapability（轨迹更新、射线碰撞、效果触发）
- ProjectileSpawnCapability（事件监听、实体创建、运行时初始化）
- 技能触发帧 → 事件请求流程（SkillExecutorCapability → ProjectileSpawnRequestEvent）

### Phase 2：表现层与 Socket 集成（预计 2~3 天）
- SocketRefs MonoBehaviour（Prefab 绑点缓存）
- ViewBridge 扩展（获取 Socket 世界坐标、缓存 EntityView）
- ProjectileViewComponent（表现层追赶逻辑、命中特效播放、对象池 reset）
- View/Logic 同步策略验证（初始偏移、追赶速度、强制同步阈值）

### Phase 3：配置与联调（预计 2 天）
- ProjectileDefinition / ProjectileConfigManager 实现
- SkillEffectTable、TriggerFrameInfo（SocketName、AdditionalEffectIds）实测
- 实现多轨迹类型（Linear / Parabola / Homing）配置示例

### Phase 4：测试与优化（预计 2 天）
- 端到端逻辑测试（多段技能、穿透、追踪等）
- 表现层测试（初始偏移、Socket 失效回退、对象池回收）
- 性能压测（大量弹道并发）
- 调试工具（日志、Gizmos、射线路径可视化）

---

## 待完成功能

### 🔴 高优先级
1. ProjectileComponent / Capability 逻辑层基础实现  
2. ProjectileSpawnCapability（事件驱动抛射物生成）  
3. 射线碰撞流程（Raycast → 过滤 → TriggerSkillEffect）  
4. SocketRefs + ViewBridge 集成（表现层出射位置）

### 🟡 中优先级
1. ProjectileViewComponent（插值追赶逻辑、命中特效）  
2. 轨迹系统（Linear / Parabola / Homing）完整实现  
3. ProjectileTable / SkillEffect 配置工具链  
4. 对象池支持（ProjectilePool / ProjectileManager）

### 🟢 低优先级
1. 调试可视化（射线路径、Gizmos、日志过滤）  
2. 表现层高级特效（拖尾材质切换、动态光效）  
3. 服务器端安全校验（未来多人同步时使用）

---

## 文件清单

### 计划新增文件
- `Assets/Script/AstrumLogic/Components/ProjectileComponent.cs`
- `Assets/Script/AstrumLogic/Capabilities/ProjectileCapability.cs`
- `Assets/Script/AstrumLogic/Capabilities/ProjectileSpawnCapability.cs`
- `Assets/Script/AstrumLogic/SkillSystem/ProjectileDefinition.cs`
- `Assets/Script/AstrumLogic/SkillSystem/ProjectileConfigManager.cs`
- `Assets/Script/AstrumLogic/SkillSystem/ProjectileSpawnRequestEvent.cs`
- `Assets/Script/AstrumView/Components/ProjectileViewComponent.cs`
- `Assets/Script/AstrumView/MonoBehaviours/SocketRefs.cs`

### 计划修改文件
- `Assets/Script/AstrumLogic/Capabilities/SkillExecutorCapability.cs`（触发帧 → 事件请求）
- `Assets/Script/AstrumLogic/SkillSystem/TriggerFrameInfo.cs`（SocketName / EffectIds 支持）
- `Assets/Script/AstrumLogic/Archetypes/Builtins/CombatArchetype.cs`（注册新的能力组件）
- `Assets/Script/AstrumView/Core/ViewBridge.cs`（支持实体视图映射与 Socket 查询）
- 配置表：`AstrumConfig/Tables/Datas/Skill/#SkillActionTable.csv`、`ProjectileTable.csv`（待创建）

---

## 技术债务

| 类型 | 描述 | 状态 |
|------|------|------|
| 表现层捕获初始偏移 | 需验证不同模型、Socket 配置对初始偏移的影响 | 待验证 |
| Raycast 命中排序 | 需要明确 PhysicsWorld.Raycast 的返回顺序，必要时自排序 | 待确认 |
| 穿透与碰撞共存 | 多段穿透 + 多效果的逻辑顺序需写自动化测试 | 待实现 |
| ViewBridge 缓存策略 | 需要评估大规模弹道生成时的缓存/查找成本 | 待评估 |
| 调试可视化 | 射线路径、命中点等调试功能尚未设计 | 待规划 |

---

## 更新日志

### v0.1.0 – 2025-11-10
- ✅ 完成《射击系统技术设计》文档，覆盖运行时/表现层/配置全流程
- ✅ 明确事件驱动的抛射物生成流程（SkillExecutor → Event → SpawnCapability）
- ✅ 设计射线碰撞 + 穿透体系，替换传统碰撞体判定
- ✅ 设计表现层追赶逻辑，解决 Socket 与逻辑位置偏差
- ✅ 规划 SocketRefs / ViewBridge 集成方案

---

## 相关文档

- [Projectile-Shooting-System 技术设计](../射击系统/Projectile-Shooting-System%20射击系统技术设计.md)  
- [Action-System 动作系统](../技能系统/Action-System%20动作系统.md)  
- [Skill-System 技能系统](../技能系统/Skill-System%20技能系统.md)  
- [Skill-Effect-Runtime 技术方案](../技能系统/Skill-Effect-Runtime%20技能效果运行时.md)  
- [技能动画视觉跟随方案](../移动-位移/技能动画视觉跟随方案.md)
