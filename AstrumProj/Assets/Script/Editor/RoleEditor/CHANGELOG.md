# 更新日志

## [v0.2.1] - 2025-10-08

### 🐛 Bug修复

- **[关键] UI交互完全失效**
  - 修复：将 `RoleEditorData` 改为继承 `ScriptableObject`
  - 修复：使用 `PropertyTree` 手动绘制Odin UI
  - 影响：所有按钮和字段现在可以正常交互

- **[关键] ActionTable CSV读取失败**
  - 修复：添加缺失的 `duration` 字段映射
  - 修复：修正 `TableField` 索引计算（从0开始）
  - 影响：动画路径现在可以正确读取

- **[关键] 动画切换失效**
  - 修复：添加 `_selectedAnimationIndex` 状态字段
  - 修复：下拉菜单选择改变时自动播放动画
  - 影响：动画切换现在完全可用

- **[重要] 动画产生位移（Root Motion）**
  - 修复：设置 `animator.applyRootMotion = false`
  - 修复：播放动画前重置模型位置和旋转
  - 影响：角色现在固定在原地播放动画

- **预览渲染错误**
  - 修复：RenderTexture高度计算，确保最小高度
  - 修复：优化相机定位，使用球面坐标系
  - 影响：模型预览正常显示，相机视角更合理

- **动画控制面板位置混乱**
  - 修复：移除重复的动画控制面板代码
  - 修复：明确由 `RolePreviewModule` 负责动画控制UI
  - 影响：UI布局更清晰

### ✨ 新增功能

- **可调整预览窗口大小**
  - 添加可拖拽的分隔条
  - 支持用户自定义预览面板宽度

### 🔧 改进

- **调试日志增强**
  - CSV读取成功日志
  - ActionTable加载详细日志
  - 动画路径查询日志

- **代码质量提升**
  - 职责分离更清晰
  - 状态管理更规范
  - 错误处理更完善

### 📝 文档

- 新增：[阶段2 Bug修复记录](./阶段2-Bug修复记录.md)
- 更新：README.md 版本信息

---

## [v0.2.0] - 2025-10-07

### ✨ 新增功能

**阶段2完成：编辑器UI**

- 角色列表模块（搜索、筛选、CRUD）
- 模型预览模块（3D预览、相机控制）
- 动画播放系统（Animancer集成）
- Odin Inspector自动UI生成
- 三列布局编辑器窗口

### 📝 文档

- 新增：[阶段2完成总结](./阶段2完成总结.md)
- 新增：[阶段2测试指南](./阶段2测试指南.md)

---

## [v0.1.0] - 2025-10-06

### ✨ 新增功能

**阶段1完成：CSV框架**

- 通用Luban CSV读写框架
  - `LubanCSVReader` - 泛型CSV读取器
  - `LubanCSVWriter` - 泛型CSV写入器
  - `TableFieldAttribute` - 字段映射特性
  - `NullableConverters` - 空值处理转换器

- 表数据映射
  - `EntityTableData` - 实体表映射
  - `RoleTableData` - 角色表映射

- 角色数据持久化
  - `RoleDataReader` - 角色数据读取
  - `RoleDataWriter` - 角色数据写入
  - 自动备份功能（保留5个版本）

### 📝 文档

- 新增：[阶段1完成总结](./阶段1完成总结.md)
- 新增：[Luban CSV框架使用指南](../../../AstrumConfig/Doc/Luban_CSV框架使用指南.md)
- 新增：[CSV框架快速参考](../../../AstrumConfig/Doc/CSV框架快速参考.md)

---

## 版本说明

- **v0.2.x** - 阶段2：编辑器UI开发
- **v0.1.x** - 阶段1：CSV框架开发
- **v0.3.x** (计划中) - 阶段3：技能编辑器
- **v0.4.x** (计划中) - 阶段4：时间轴系统

---

**更新频率**: 每个重要功能或Bug修复后更新  
**维护者**: Astrum Team

