# 存档系统开发进展

> 📊 **当前版本**: v0.3.0  
> 📅 **最后更新**: 2025-01-27  
> 👤 **负责人**: Lavender

## TL;DR（四象限）
- 状态/进度：账号存档系统核心功能已实现，完成度约85%
- 已达成：实现客户端实例ID管理器；改造存档系统支持账号存档；实现服务器端账号存档管理器；更新登录流程
- 风险/阻塞：Unity需要刷新识别新文件；账号存档同步协议待实现（可选功能）
- 下一步：激活Unity刷新文件；测试ParrelSync多实例存档隔离；实现账号存档同步协议（可选）

## 版本历史

### v0.3.1 - 账号存档系统实现 (2025-01-27)
**状态**: 🚧 开发中

**完成内容**:
- [x] 完成账号存档系统技术设计文档（[Account-Save-System-Design 账号存档系统设计.md](Account-Save-System-Design%20账号存档系统设计.md)）
- [x] 设计客户端实例ID持久化方案
- [x] 统一客户端实例账号和存档地址（客户端实例ID直接作为账号ID）
- [x] 设计ParrelSync多实例支持方案
- [x] 更新协议定义（LoginRequest添加ClientInstanceId字段）
- [x] 实现 `ClientInstanceIdManager`（客户端实例ID管理器）
- [x] 实现 `ParrelSyncHelper`（ParrelSync实例检测辅助类）
- [x] 改造 `SaveSystem` 支持存档类型和客户端实例ID路径
- [x] 改造 `PlayerDataManager` 支持账号存档模式
- [x] 改造服务器端 `UserManager` 使用客户端实例ID作为账号ID
- [x] 实现服务器端 `AccountSaveManager`（账号存档管理器）
- [x] 更新登录流程集成账号存档加载
- [x] 更新客户端登录请求发送ClientInstanceId
- [x] 重新生成协议代码

**待完成**:
- [x] 修复ParrelSync编译错误（使用反射访问，避免编译时依赖）
- [ ] 实现账号存档同步协议（LoadAccountSaveRequest/Response, SaveAccountSaveRequest/Response）（可选）
- [ ] 测试ParrelSync多实例存档隔离

**预计工时**: 24 小时（已完成约20小时）

---

### v0.3.0 - 账号存档系统设计 (2025-01-27)
**状态**: ✅ 已完成

**完成内容**:
- [x] 完成账号存档系统技术设计文档
- [x] 设计客户端实例ID持久化方案
- [x] 统一客户端实例账号和存档地址
- [x] 设计ParrelSync多实例支持方案
- [x] 更新协议定义（LoginRequest添加ClientInstanceId字段）

**预计工时**: 4 小时

**相关文档**:
- [Account-Save-System-Design 账号存档系统设计.md](Account-Save-System-Design%20账号存档系统设计.md)

---

### v0.2.0 - 数据接入 (2025-11-10)
**状态**: ✅ 已完成

**完成内容**:
- [x] 梳理 `LevelComponent`、`GrowthComponent` 等数值结构
- [x] 明确持久化范围与排除项（参考《存档数值方案.md》）
- [x] 形成存档数据结构草案
- [x] 实现 `CurrencyComponent`、`InventoryComponent` 及序列化支持
- [x] 在单机模式加载/保存流程中接入实体 ↔️ 存档映射

**预计工时**: 16 小时

---

## 当前阶段

**阶段名称**: 账号存档系统实现

**完成度**: 90%（核心功能已实现并编译通过，待测试）

**下一步计划**:
1. 激活Unity刷新文件识别新代码
2. 测试ParrelSync多实例存档隔离
3. 实现账号存档同步协议（可选功能）

**已实现文件**:
- `AstrumProj/Assets/Script/AstrumClient/Data/ClientInstanceIdManager.cs` - 客户端实例ID管理器
- `AstrumProj/Assets/Script/AstrumClient/Data/ParrelSyncHelper.cs` - ParrelSync辅助类
- `AstrumProj/Assets/Script/AstrumClient/Data/SaveSystem.cs` - 改造后的存档系统
- `AstrumProj/Assets/Script/AstrumClient/Data/PlayerDataManager.cs` - 改造后的玩家数据管理器
- `AstrumServer/AstrumServer/Managers/UserManager.cs` - 改造后的用户管理器
- `AstrumServer/AstrumServer/Data/AccountSaveManager.cs` - 服务器端账号存档管理器

---

*文档版本：v0.3.1*  
*创建时间：2025-11-10*  
*最后更新：2025-01-27*  
*状态：🚧 开发中*  
*Owner*: Lavender  
*变更摘要*: 完成账号存档系统核心功能实现，包括客户端实例ID管理、存档系统改造、服务器端账号存档管理
