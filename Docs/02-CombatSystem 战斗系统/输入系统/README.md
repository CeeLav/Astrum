# 输入系统文档

## 文档列表

### [Input-System 输入系统架构设计](./Input-System%20输入系统架构设计.md)

完整的输入系统架构设计文档，包含：
- 背景与问题分析
- 三层架构设计
- CSV配置表设计
- 代码实现方案
- 扩展性说明
- 实施步骤

## 快速导航

### 配置表文件

所有输入配置表位于：`AstrumConfig/Tables/Datas/Input/`

- `#InputBindingTable.csv` - 输入绑定配置
- `#LSInputFieldMappingTable.csv` - LSInput字段映射
- `#ActionCommandMappingTable.csv` - ActionCommand映射
- `#InputContextTable.csv` - 输入上下文配置

### 代码文件

- `AstrumProj/Assets/Script/AstrumClient/Managers/InputManager.cs` - 输入管理器（待重构）
- `AstrumProj/Assets/Script/AstrumLogic/Capabilities/ActionCapability.cs` - 动作能力（待修改）

## 当前状态

- ✅ 架构设计完成
- ✅ CSV配置表创建完成
- ⏳ 代码实现待开始
- ⏳ 测试验证待开始

## 相关系统

- [Action-System 动作系统](../技能系统/Action-System%20动作系统.md)
- [帧同步系统](../../03-NetworkSystem%20网络系统/)（如果存在）

## 更新日志

### 2025-11-09
- 创建输入系统架构设计文档
- 创建四个CSV配置表
- 定义三层架构和数据流

