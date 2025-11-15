# 存档系统开发进展

> 📊 **当前版本**: v0.3.0  
> 📅 **最后更新**: 2025-01-27  
> 👤 **负责人**: Lavender

## TL;DR（四象限）
- 状态/进度：账号存档系统设计方案完成，完成度约70%
- 已达成：数值体系调研完成；实现 Currency/Inventory 组件；存档读写同步实体逻辑；账号存档系统设计方案
- 风险/阻塞：需要实现客户端实例ID管理器和服务器端账号存档管理器；协议需要重新生成
- 下一步：实现客户端实例ID持久化；改造服务器端UserManager；实现账号存档同步协议

## 版本历史

### v0.3.0 - 账号存档系统设计 (2025-01-27)
**状态**: 📝 设计完成，待实现

**完成内容**:
- [x] 完成账号存档系统技术设计文档（[Account-Save-System-Design 账号存档系统设计.md](Account-Save-System-Design%20账号存档系统设计.md)）
- [x] 设计客户端实例ID持久化方案
- [x] 统一客户端实例账号和存档地址（客户端实例ID直接作为账号ID）
- [x] 设计ParrelSync多实例支持方案
- [x] 更新协议定义（LoginRequest添加ClientInstanceId字段）

**待完成**:
- [ ] 实现 `ClientInstanceIdManager`（客户端实例ID管理器）
- [ ] 实现 `ParrelSyncHelper`（ParrelSync实例检测辅助类）
- [ ] 改造 `SaveSystem` 支持存档类型和客户端实例ID路径
- [ ] 改造 `PlayerDataManager` 支持账号存档模式
- [ ] 改造服务器端 `UserManager` 使用客户端实例ID作为账号ID
- [ ] 实现服务器端 `AccountSaveManager`（账号存档管理器）
- [ ] 更新登录流程集成账号存档加载
- [ ] 实现账号存档同步协议（LoadAccountSaveRequest/Response, SaveAccountSaveRequest/Response）
- [ ] 重新生成协议代码
- [ ] 测试ParrelSync多实例存档隔离

**预计工时**: 24 小时

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

**完成度**: 30%（设计完成，代码待实现）

**下一步计划**:
1. 实现客户端实例ID管理器和ParrelSync辅助类
2. 改造存档系统支持账号存档
3. 实现服务器端账号存档管理器
4. 集成登录流程和存档同步协议

---

*文档版本：v0.3.0*  
*创建时间：2025-11-10*  
*最后更新：2025-01-27*  
*状态：📝 设计完成，待实现*  
*Owner*: Lavender  
*变更摘要*: 添加账号存档系统设计方案，统一客户端实例账号和存档地址
