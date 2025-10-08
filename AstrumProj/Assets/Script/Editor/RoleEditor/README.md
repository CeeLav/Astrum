# 角色编辑器

> 📖 **项目路径**: `AstrumProj/Assets/Script/Editor/RoleEditor/`  
> 🔧 **依赖**: Odin Inspector, Animancer Lite, CsvHelper  
> ✅ **状态**: 阶段2完成，待测试

## 快速开始

### 打开编辑器

```
Unity菜单 > Tools > Role & Skill Editor > Role Editor
```

### 基本操作

1. **选择角色** - 左侧列表点击选择
2. **编辑属性** - 中间面板修改（Odin自动UI）
3. **预览模型** - 右侧3D预览窗口
4. **播放动画** - 右侧底部动画控制
5. **保存修改** - 顶部"保存"按钮

---

## 功能特性

### ✅ 已实现功能

**数据编辑**：
- 编辑 EntityBaseTable（实体配置、模型、基础动作）
- 编辑 RoleBaseTable（角色属性、技能槽位）
- 数据验证和错误提示
- 自动备份（保留5个历史版本）

**界面功能**：
- 角色列表（搜索、筛选）
- CRUD操作（新建、复制、删除）
- Odin自动生成详情UI
- 未保存提醒

**预览功能**：
- 3D模型实时预览
- 相机控制（旋转、缩放）
- 动画播放（使用Animancer）
- 动画选择和速度控制

---

## 目录结构

```
RoleEditor/
├── README.md                    # 本文档
├── CHANGELOG.md                 # 更新日志 ⭐
├── 常见问题快速修复.md           # 快速修复指南 🔧
├── SETUP.md                     # 设置说明
├── 阶段1完成总结.md              # 阶段1总结
├── 阶段2完成总结.md              # 阶段2总结
├── 阶段2测试指南.md              # 测试指南
├── 阶段2-Bug修复记录.md          # Bug修复记录 ⭐
├── RoleEditor.asmdef            # 程序集定义
│
├── Core/
│   └── EditorConfig.cs          # 配置常量
│
├── Data/
│   ├── RoleEditorData.cs        # 角色数据模型（含Odin特性）
│   └── RoleDataValidator.cs     # 数据验证器
│
├── Services/                    # 服务层（工具类）
│   ├── ConfigTableHelper.cs     # 配置表查询工具
│   └── AnimationHelper.cs       # Animancer操作工具
│
├── Modules/                     # UI模块层
│   ├── RoleListModule.cs        # 角色列表模块
│   └── RolePreviewModule.cs     # 角色预览模块
│
├── Windows/
│   └── RoleEditorWindow.cs      # 主编辑器窗口
│
├── Persistence/                 # 数据持久化
│   ├── Core/                    # 通用CSV框架
│   ├── Mappings/                # 表数据映射
│   ├── RoleDataReader.cs        # 角色数据读取
│   └── RoleDataWriter.cs        # 角色数据写入
│
└── Test/
    └── CSVTest.cs               # CSV测试菜单
```

---

## 架构设计

### 分层架构

**服务层**（可复用）：
- ConfigTableHelper - 配置表查询
- AnimationHelper - Animancer操作

**UI模块层**（独立组件）：
- RoleListModule - 角色列表
- RolePreviewModule - 模型和动画预览

**窗口层**（组合层）：
- RoleEditorWindow - 组装所有模块

### 设计原则

✅ **职责分离** - 每个类职责单一  
✅ **逻辑解耦** - Helper独立于UI  
✅ **模块复用** - 服务层可在其他编辑器使用  
✅ **事件驱动** - 模块间通过事件通信

---

## 技术栈

| 技术 | 用途 | 版本 |
|------|------|------|
| Odin Inspector | 自动生成详情UI | 3.3.1.13 |
| Animancer | 动画播放系统 | Lite 8.2.2 |
| CsvHelper | CSV文件读写 | 33.1.0 |
| PreviewRenderUtility | 3D场景预览 | Unity内置 |

---

## 开发阶段

### ✅ 阶段1：CSV框架（已完成）

- 通用Luban CSV读写框架
- 表数据映射系统
- 角色数据读写逻辑

📖 [阶段1完成总结](./阶段1完成总结.md)

### ✅ 阶段2：编辑器UI（已完成）

- 角色列表模块
- 模型预览模块（含Animancer）
- 主编辑器窗口

📖 [阶段2完成总结](./阶段2完成总结.md)  
📖 [阶段2测试指南](./阶段2测试指南.md)  
📖 [阶段2 Bug修复记录](./阶段2-Bug修复记录.md) ⭐ **最新**

### 🔜 阶段3：技能编辑器（待规划）

### 🔜 阶段4：时间轴系统（待规划）

---

## 使用说明

### 编辑角色属性

1. 选择角色（左侧列表）
2. 在中间详情面板编辑：
   - **实体配置** - 模型路径、基础动作
   - **角色配置** - 名称、类型、描述
   - **基础属性** - 攻击、防御、生命、速度
   - **成长属性** - 各属性成长值
   - **技能槽位** - 轻击、重击、技能1、技能2

3. 修改后会显示 * 标记
4. 点击"保存"写入CSV文件

### 预览模型和动画

1. 选择角色后右侧自动加载模型
2. **相机控制**：
   - 左键拖拽 - 旋转模型
   - 滚轮 - 缩放视角
   - 重置视角 - 恢复初始状态

3. **动画控制**：
   - 动画下拉框 - 选择动作（待机、行走等）
   - 播放/暂停 - 控制播放
   - 停止 - 重置到开头
   - 速度滑块 - 调整播放速度

### 创建新角色

1. 点击"新建"按钮
2. 自动生成唯一ID
3. 填写角色信息
4. 配置模型路径
5. 配置基础动作ID
6. 保存

### 复制角色

1. 选中要复制的角色
2. 点击"复制"按钮
3. 修改名称和属性
4. 保存

---

## 相关文档

**编辑器文档**：
- [更新日志](./CHANGELOG.md) ⭐
- [常见问题快速修复](./常见问题快速修复.md) 🔧
- [设置说明](./SETUP.md)
- [阶段2 Bug修复记录](./阶段2-Bug修复记录.md)

**框架文档**：
- [Luban CSV框架使用指南](../../../AstrumConfig/Doc/Luban_CSV框架使用指南.md)
- [CSV框架快速参考](../../../AstrumConfig/Doc/CSV框架快速参考.md)

**策划文档**：
- [技能系统策划案](../../../AstrumConfig/Doc/技能系统策划案.md)

---

## 常见问题

**Q: 打开编辑器窗口后列表为空？**  
A: 运行CSV读取测试，检查路径配置

**Q: 预览窗口不显示模型？**  
A: 检查角色的ModelPath是否正确配置

**Q: 动画不播放？**  
A: 检查ActionID配置，查看Console错误日志

**Q: 保存后CSV文件没变化？**  
A: 检查文件权限，查看备份文件是否生成

**Q: Odin特性不生效？**  
A: 确保Odin Inspector已正确导入

---

**最后更新**: 2025-10-08  
**开发者**: Astrum Team  
**版本**: v0.2.1 (阶段2 Bug修复完成)

