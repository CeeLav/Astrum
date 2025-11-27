# 🔧 技术参考文档

## 📖 文档列表

### Unity 技术

#### [Partial类生成指南](Partial-Class-Generation%20Partial类生成指南.md)
Partial Class 的使用和自动生成

**内容**:
- Partial Class 概念
- UIGenerator 的 Partial Class 生成
- 手动代码和生成代码的分离

#### [资源清单生成器](ResourceManifest-Generator%20资源清单生成器.md)
ResourceManifestGenerator 的使用说明

**功能**:
- 自动扫描资源
- 生成资源清单
- 资源加载优化

#### [TMP转换指南](TextMeshPro-Conversion%20TMP转换指南.md)
TextMeshPro 和 Unity Text 的转换

**用途**:
- UI组件类型转换
- 兼容性处理

---

## 🔧 技术组件

### 代码内文档

以下技术组件的详细文档位于各自的代码目录：

#### UI系统
- **UIGenerator** - `AstrumProj/Assets/Script/Editor/UIGenerator/README.md`
  - 自动生成UI代码
  - UIRefs 组件系统

#### 日志系统
- **ASLogger** - `AstrumProj/Assets/Script/Editor/ASLogger/README.md`
  - 统一日志接口
  - 多端日志输出

#### 网络系统
- **Network** - `AstrumProj/Assets/Script/Network/README.md`
  - TCP 网络通信
  - Protocol Buffers 序列化
  - MemoryPack 集成

#### 客户端系统
- **AstrumClient** - `AstrumProj/Assets/Script/AstrumClient/README.md`
  - 客户端架构
  - 网络管理器
  - 事件系统

---

## 🏗️ 核心技术栈

### 客户端 (Unity)
- **Unity 2022.3 LTS** - 游戏引擎
- **TrueSync** - 确定性物理
- **BEPU Physics v1** - 碰撞检测
- **MemoryPack** - 高性能序列化
- **Odin Inspector** - 编辑器扩展
- **Animancer** - 动画系统

### 服务器 (.NET)
- **.NET 9.0** - 服务器框架
- **Protocol Buffers** - 网络协议
- **MemoryPack** - 序列化

### 工具链
- **Luban** - 配置表生成
- **Proto2CS** - 协议代码生成
- **UIGenerator** - UI代码生成

---

## 🔗 相关文档

- [核心架构](../05-CoreArchitecture%20核心架构/) - ECC架构、序列化
- [配置系统](../06-Configuration%20配置系统/) - 配置表生成
- [开发指南](../07-Development%20开发指南/) - 开发流程

---

## 📚 技术文章

### 序列化与回滚
- [序列化最佳实践](../05-CoreArchitecture%20核心架构/Serialization-Best-Practices%20序列化最佳实践.md)
- [序列化检查清单](../05-CoreArchitecture%20核心架构/Serialization-Checklist%20序列化检查清单.md)

### 架构设计
- [ECC结构说明](../05-CoreArchitecture%20核心架构/ECC-System%20ECC结构说明.md)
- [Archetype系统](../05-CoreArchitecture%20核心架构/Archetype-System%20Archetype结构说明.md)

---

**返回**: [文档中心](../README.md)

