# 受击动作集成开发进展

**版本**: v0.1  
**创建日期**: 2025-11-08  
**更新日期**: 2025-11-08  
**状态**: 🚧 开发中 - Action 系统对受击事件的支持推进中  

> 📖 **相关文档**：
> - [受击动作集成设计](../技能系统/ActionSystem-HitReaction-Integration-Design%20受击动作集成设计.md)
> - [受击与击退设计](../技能效果/Hit-Reaction-And-Knockback%20受击与击退.md)
> - [动作系统策划案](../技能系统/Action-System%20动作系统.md)

---

## TL;DR（四象限）
- **状态/进度**：30%（设计完成，开发启动）
- **已达成**：
  - ✅ 技术方案文档（ActionSystem-HitReaction-Integration-Design）完成
  - ✅ 确认 EntityBaseTable 需要新增 `HitAction` 字段
- **风险/阻塞**：
  - ⚠️ ActionCapability 需新增外部预约接口，可能影响现有输入驱动逻辑
  - ⚠️ Luban 生成代码需同步更新，编译需等待生成完成
- **下一步**：
  - 📌 扩展 ActionCapability 支持外部预订单注入
  - 📌 更新 EntityBaseTable CSV 并生成代码
  - 📌 实现 HitReactionCapability 的受击动作调用与朝向调整

---

## 版本历史

| 版本 | 日期       | 变更内容                       | 作者          |
|------|------------|--------------------------------|---------------|
| v0.1 | 2025-11-08 | 建立开发进展文档，记录当前状态 | AI Assistant |

---

## 任务计划

### Phase 1: 配置扩展 ✅ 设计完成 / ⏳ 开发中
- [ ] 更新 `AstrumConfig/Tables/Datas/Entity/#EntityBaseTable.csv`，新增 `HitAction` 列
- [ ] 运行 Luban 生成工具，更新客户端/服务器的 `EntityBaseTable` 代码
- [ ] 验证表格兼容性（旧数据补零）

### Phase 2: Action 系统升级 ⏳
- [ ] 在 `ActionCapability` 中添加外部动作预约接口（`EnqueueExternalAction`）
- [ ] 扩展 `ActionComponent` 存储结构（外部预订单缓存）
- [ ] 调整候选合并流程，确保外部预约的优先级生效
- [ ] 编写日志辅助排查（重复预约、缺失动作）

### Phase 3: HitReaction 能力增强 ⏳
- [ ] 在 `HitReactionCapability` 中实现受击动作处理
- [ ] 朝向更新逻辑：根据 `HitReactionEvent.HitDirection` 设置实体朝向
- [ ] 受击动作优先级策略与回退处理（无配置时记录 Warning）
- [ ] 保持既有特效/音效触发逻辑

### Phase 4: 测试与验证 ⏳
- [ ] 增加单元测试：外部动作预约、受击朝向计算
- [ ] 集成测试：模拟受击事件，验证动作切换和朝向变化
- [ ] 编译检查：`dotnet build AstrumProj/AstrumProj.sln`
- [ ] Unity 运行时验证（提醒用户刷新 Assets/Refresh）

---

## 风险与缓解

| 风险 | 描述 | 缓解策略 |
|------|------|----------|
| ActionCapability 合并逻辑冲突 | 外部预约可能与自动取消逻辑相互覆盖 | 为外部预约设置来源标签，合并前去重，并在日志中标记来源 |
| Luban 生成时间 | 新增字段需重新生成代码，耗时且需双端同步 | 在本地一次性生成并提交，记录生成时间与命令 |
| 朝向调整与击退/位移冲突 | 受击朝向与 Knockback 位移同时更新可能造成抖动 | 在朝向更新中仅调整 Yaw，保持位移组件数据不重复修改 |

---

## 依赖情况

| 依赖项 | 状态 | 说明 |
|--------|------|------|
| ActionSystem-HitReaction-Integration-Design | ✅ 完成 | 作为实现依据 |
| Luban CSV 生成工具 | ✅ 可用 | 需在更新配置后执行 |
| HitReactionCapability 现有实现 | ✅ 可读 | 当前逻辑留有 TODO，可扩展 |

---

## 指标与检查清单
- [ ] CSV 字段新增，生成代码通过编译
- [ ] ActionCapability 支持外部预约且不破坏现有输入链路
- [ ] HitReactionCapability 能在受击时切换动作并朝向攻击方向
- [ ] 受击动作缺省时的回退日志清晰
- [ ] 测试覆盖关键路径，含重复受击与无动作配置场景

---

## 下一次更新目标
- 完成 Phase 1 & Phase 2 的代码改动
- 输出实现清单与测试计划
- 提供编译结果与日志摘要


