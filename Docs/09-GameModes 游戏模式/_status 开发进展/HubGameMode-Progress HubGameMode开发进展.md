# 🏠 HubGameMode 开发进展

**版本历史**: 设计阶段  
**最后更新**: 2025-01-15

---

## 📊 总体进度

| 阶段 | 状态 | 完成度 | 说明 |
|------|------|--------|------|
| Phase 0: 技术方案设计 | ✅ 完成 | 100% | 完整技术设计文档 |
| Phase 1: 基础架构 | 📝 计划中 | 0% | GameMode、数据结构 |
| Phase 2: 场景和UI | 📝 计划中 | 0% | HubScene、HubView |
| Phase 3: 集成与测试 | 📝 计划中 | 0% | 循环测试 |
| Phase 4: 场景切换优化 | 📝 计划中 | 0% | 加载进度、动画 |

**总体完成度**: 10% (仅设计完成)

---

## 版本历史

### 2025-01-15 - 设计阶段完成

#### ✅ Phase 0: 技术方案设计

**里程碑**: HubGameMode 完整技术方案设计完成！

##### 1. 架构设计确定 ✅

**核心决策**：
- ✅ 使用独立的 PlayerDataManager 统一管理玩家数据
- ✅ 只持久化基础进度数据（Level、Exp、加点等），不保存派生属性
- ✅ GameMode 不持有 HubView 引用，通过事件通信
- ✅ 简化的 GameModeState，无内部状态
- ✅ 探索结算功能暂不实现（先跑通循环）

**数据管理方案**：
```
PlayerDataManager (全局单例)
    ↓ 管理
PlayerProgressData (持久化数据)
    ├─ Level, Exp, RoleId
    ├─ AvailableStatPoints
    ├─ AllocatedXxxPoints
    └─ StarFragments
```

##### 2. 接口设计 ✅

**HubGameMode 核心接口**：
- `Initialize()` - 基础初始化
- `Update()` - 无特殊逻辑（UI 驱动）
- `Shutdown()` - 清理资源
- `StartExploration()` - 切换到探索模式

**PlayerDataManager 核心接口**：
- `Initialize()` - 加载数据
- `LoadProgressData()` - 从文件加载
- `SaveProgressData()` - 保存到文件

##### 3. UI 设计 ✅

**HubView 简化设计**：
- 开始探索按钮（核心功能）
- 资源显示（星能碎片）
- 无升级面板（暂不实现）

**UI 与 GameMode 解耦**：
- HubGameMode 不持有 HubView 引用
- 通过事件 `StartExplorationRequestEventData` 通信
- UI 通过 PlayerDataManager 获取数据

##### 4. 场景切换设计 ✅

**HubScene**：
- 空场景占位
- 仅包含 HubView UI
- 预留塔防建造空间

##### 5. 与现有模式的集成 ✅

**模式切换流程**：
```
LoginGameMode → HubGameMode → SinglePlayerGameMode → HubGameMode
```

**GameModeType 扩展**：
- 添加 `Hub` 枚举值
- GameDirector 支持 Hub 模式创建和切换

---

## 📋 开发任务清单

### Phase 1: 基础架构（3-4天）

#### 1.1 核心数据结构
- [ ] 创建 `PlayerProgressData.cs`
  - [ ] 定义基础字段（Level, Exp, RoleId 等）
  - [ ] 定义加点字段（AllocatedXxxPoints）
  - [ ] 定义资源字段（StarFragments, Resources）
  - [ ] 添加 MemoryPack 序列化标记
- [ ] 创建 `PlayerDataManager.cs`
  - [ ] 实现 Singleton 模式
  - [ ] 实现 Initialize()
  - [ ] 实现 LoadProgressData()
  - [ ] 实现 SaveProgressData()
  - [ ] 实现 CreateDefaultProgressData()

#### 1.2 GameMode 实现
- [ ] 创建 `HubGameMode.cs`
  - [ ] 继承 BaseGameMode
  - [ ] 实现 Initialize()
  - [ ] 实现 Update()
  - [ ] 实现 Shutdown()
  - [ ] 实现 StartExploration()
  - [ ] 订阅 StartExplorationRequestEventData 事件

#### 1.3 事件定义
- [ ] 创建 `StartExplorationRequestEventData.cs`
  - [ ] 继承 EventData
  - [ ] 无额外数据（空事件）
- [ ] 创建 `PlayerDataChangedEventData.cs`
  - [ ] 继承 EventData
  - [ ] 包含 PlayerProgressData 引用

#### 1.4 系统集成
- [ ] 扩展 GameModeType 枚举
  - [ ] 添加 Hub 值
  - [ ] 更新 GameDirector.SwitchGameMode() switch 语句
- [ ] 创建 SaveSystem（或使用现有实现）
  - [ ] LoadPlayerProgressData()
  - [ ] SavePlayerProgressData()
  - [ ] 使用 MemoryPack 序列化

#### 1.5 初始化集成
- [ ] GameDirector.Initialize() 中初始化 PlayerDataManager
  - [ ] 在 InitializeManagers() 中调用 PlayerDataManager.Initialize()

---

### Phase 2: 场景和UI（2-3天）

#### 2.1 场景创建
- [ ] 创建 HubScene Unity 场景
  - [ ] 空场景，设置基础灯光
  - [ ] 添加 Canvas（UI 容器）
  - [ ] 场景名称：HubScene

#### 2.2 HubView UI
- [ ] 创建 HubView.cs 脚本
  - [ ] 继承 UIRefs
  - [ ] 定义 UI 引用（按钮、文本）
  - [ ] 实现 OnInitialize()
  - [ ] 订阅 PlayerDataChangedEventData
  - [ ] 订阅按钮点击事件
- [ ] 创建 HubView Prefab
  - [ ] 设计基础布局
  - [ ] 添加开始探索按钮
  - [ ] 添加资源显示文本
  - [ ] 绑定 UI 组件引用
  - [ ] 生成代码引用（UITool）

#### 2.3 UI 功能
- [ ] 开始探索按钮
  - [ ] 点击触发 StartExplorationRequestEventData
- [ ] 资源显示
  - [ ] 监听 PlayerDataChangedEventData
  - [ ] 更新星能碎片显示
- [ ] 玩家等级显示
  - [ ] 监听数据变化
  - [ ] 更新等级文本

---

### Phase 3: 集成与测试（2-3天）

#### 3.1 GameMode 集成
- [ ] LoginGameMode 集成
  - [ ] 单机游戏按钮切换到 HubGameMode
  - [ ] 测试模式切换流程
- [ ] SinglePlayerGameMode 集成
  - [ ] 探索结束切换到 HubGameMode
  - [ ] 移除/注释掉旧的跳转逻辑
- [ ] HubGameMode 集成
  - [ ] Initialize() 显示 HubView
  - [ ] Shutdown() 清理 UI
  - [ ] StartExploration() 切换到探索

#### 3.2 场景切换集成
- [ ] HubGameMode 场景切换
  - [ ] 进入 Hub 时加载 HubScene
  - [ ] 场景加载完成后显示 UI
- [ ] SinglePlayerGameMode 场景切换
  - [ ] 确保 DungeonsGame 场景正确切换
  - [ ] 场景加载完成后创建玩家

#### 3.3 数据流测试
- [ ] PlayerDataManager 测试
  - [ ] 首次运行创建默认数据
  - [ ] 数据保存和加载
  - [ ] 数据一致性验证
- [ ] 模式切换数据测试
  - [ ] Hub → 探索，数据正确传递
  - [ ] 探索 → Hub，数据正确保存
  - [ ] 多次循环，数据不丢失

#### 3.4 完整循环测试
- [ ] Login → Hub → 探索 → Hub 循环
  - [ ] 模式切换正确
  - [ ] 场景切换正确
  - [ ] UI 显示正确
  - [ ] 数据保存正确

---

### Phase 4: 场景切换优化（1-2天）

#### 4.1 加载提示
- [ ] 创建 LoadingUI
  - [ ] 加载进度条
  - [ ] 加载提示文本
- [ ] 场景加载期间显示
  - [ ] HubGameMode 切换时显示
  - [ ] SinglePlayerGameMode 切换时显示

#### 4.2 切换优化
- [ ] 场景切换动画
  - [ ] 淡入淡出效果
  - [ ] 黑屏过渡
- [ ] 性能优化
  - [ ] 异步加载优化
  - [ ] UI 显示延迟优化

---

## 🔧 技术要点

### 已确定的设计决策

#### 1. 数据管理方式
- ✅ 独立的 PlayerDataManager 管理玩家进度
- ✅ 只持久化 Level、Exp、加点、资源
- ✅ 不保存派生属性（MaxHP、ATK 等计算值）

#### 2. UI 架构
- ✅ GameMode 不持有 HubView 引用
- ✅ 通过事件系统通信
- ✅ UI 通过 PlayerDataManager 获取数据

#### 3. 接口设计
- ✅ HubGameMode 实现最简接口
- ✅ 无内部状态机
- ✅ 通过 BaseGameMode 通用功能

#### 4. 场景管理
- ✅ HubScene 空场景占位
- ✅ 使用 SceneManager.LoadSceneAsync()
- ✅ 场景加载完成后初始化 UI

### 待讨论的技术问题

#### 1. IGameMode 接口重构
**问题**：是否将 Initialize() 和 StartGame() 合并为 OnModeEnter()

**影响**：
- 影响所有现有 GameMode
- 需要统一重构

**推荐**：
- 暂时保持不变，确保 HubGameMode 先实现
- 接口重构作为独立任务

#### 2. SaveSystem 实现位置
**问题**：SaveSystem 放在哪个命名空间和文件

**选项**：
- A. 独立的 SaveSystem 静态类
- B. 放在 PlayerDataManager 中
- C. 放在 CommonBase

**推荐**：
- 选项 A，保持职责清晰

#### 3. PlayerDataManager 初始化时机
**问题**：何时初始化 PlayerDataManager

**选项**：
- A. GameDirector.Initialize() 时
- B. 第一次使用 HubGameMode 时

**推荐**：
- 选项 A，确保数据随时可用

---

## 🎯 验收标准

### Phase 1 验收
- [ ] HubGameMode.cs 编译通过
- [ ] PlayerDataManager.cs 编译通过
- [ ] 所有事件定义编译通过
- [ ] GameDirector 支持 Hub 模式切换
- [ ] 单元测试通过（如有）

### Phase 2 验收
- [ ] HubScene 场景可加载
- [ ] HubView UI 正确显示
- [ ] 开始探索按钮点击有响应
- [ ] 资源数据正确显示

### Phase 3 验收
- [ ] Login → Hub 切换正常
- [ ] Hub → 探索 切换正常
- [ ] 探索 → Hub 返回正常
- [ ] 玩家数据保存和加载正常
- [ ] 多次循环无异常

### Phase 4 验收
- [ ] 场景切换有加载提示
- [ ] 加载过程流畅无卡顿
- [ ] 切换动画效果自然
- [ ] 性能达标（启动 < 5秒）

---

## 📝 已知限制

### 当前版本限制
1. **无结算功能**：探索完成后无奖励计算
2. **无养成功能**：无升级、加点功能
3. **无塔防**：无夜潮塔防入口
4. **UI 简化**：界面较为基础

### 后续计划
- v0.2: 添加探索结算功能
- v0.3: 添加塔防夜潮入口
- v0.4: 添加角色养成和加点
- v0.5: 添加基地建造系统

---

## 📚 相关文档

- [HubGameMode 技术设计](../HubGameMode-Technical-Design%20HubGameMode技术设计.md)
- [单机模式设计](../Single-Player%20单机模式.md)
- [数值系统设计](../../02-CombatSystem%20战斗系统/数值系统/Stats-System%20数值系统.md)
- [单机闭环 v0.1 计划](../../项目管理/SinglePlayer-Loop/SinglePlayer-Loop-v0.1.md)

---

**维护者**: 开发团队  
**最后更新**: 2025-01-15
