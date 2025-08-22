# Astrum 游戏项目

> 基于 Unity 的多人游戏项目，采用客户端-服务器架构和帧同步技术

## 📚 文档导航

### 🚀 新手入门 (推荐阅读顺序)

1. **[快速开始指南](QUICK_START_GUIDE.md)** ⭐  
   *5分钟快速了解项目，适合新的AI会话或开发者*

2. **[项目概述](ASTRUM_PROJECT_OVERVIEW.md)** 📖  
   *完整的项目说明，包含架构、技术栈、开发指南*

3. **[技术架构](ASTRUM_ARCHITECTURE.md)** 🏗️  
   *详细的架构图和技术细节*

### 🔧 开发相关

- **[Unity命令行测试说明](Unity命令行测试说明.md)** 🧪  
  *完整的测试系统使用指南*

- **测试脚本**:
  - `Test-UnityNetwork.ps1` - PowerShell测试脚本 (推荐)
  - `run_unity_tests.bat` - 批处理测试脚本
  - `unity_test.bat` - 通用Unity测试脚本

## 🎯 项目特点

- ✅ **基于Session的网络通信** (最新重构)
- ✅ **帧同步游戏逻辑**
- ✅ **ECS架构设计**
- ✅ **完整的命令行测试系统**
- ✅ **模块化管理器架构**

## 🏗️ 项目结构

```
Astrum/
├── 📁 AstrumProj/        # Unity主项目
├── 📁 AstrumServer/      # .NET服务器
├── 📁 AstrumLogic/       # 逻辑核心
├── 📁 AstrumTool/        # 开发工具
├── 📁 AstrumConfig/      # 配置文件
├── 📄 快速开始指南       # ← 从这里开始
├── 📄 项目概述           # 完整说明
└── 📄 技术架构           # 架构详情
```

## ⚡ 快速开始

### 1. 运行测试 (验证环境)
```powershell
.\Test-UnityNetwork.ps1 -TestType compile
```

### 2. 查看网络状态
```csharp
var networkManager = NetworkManager.Instance;
bool isConnected = networkManager.IsConnected();
```

### 3. 发送网络消息
```csharp
var message = NetworkMessageExtensions.CreateSuccess("ping", data);
NetworkManager.Instance.Send(message);
```

## 📋 最近更新

### 🔄 网络模块重构 (最新)
- **移除**: 复杂的RPC系统
- **新增**: 基于Session的简化通信
- **改进**: 事件驱动的消息处理
- **增强**: 完整的测试系统

**影响文件**:
- `NetworkManager.cs` - 大幅简化
- `GameApplication.cs` - 消息处理更新
- `GamePlayManager.cs` - API适配

## 🛠️ 技术栈

- **Unity**: 6000.2.0b7
- **.NET**: 8.0
- **网络**: 自定义TCP + Session
- **序列化**: MemoryPack
- **架构**: ECS + 帧同步

## 🧪 测试系统

### 可用测试
- ✅ 编译检查
- ✅ 网络功能测试  
- ✅ 性能基准测试
- ✅ 环境清理

### 运行方式
```powershell
# 运行所有测试
.\Test-UnityNetwork.ps1 -TestType all

# 单独测试类型
.\Test-UnityNetwork.ps1 -TestType network
```

## 🔍 快速诊断

| 问题类型 | 检查方法 | 解决方案 |
|---------|---------|---------|
| 编译错误 | 运行编译测试 | 查看错误日志 |
| 网络问题 | 检查连接状态 | 运行网络测试 |
| 性能问题 | 运行性能测试 | 分析性能报告 |

## 📞 获取帮助

1. **查看文档**: 从快速开始指南开始
2. **运行测试**: 使用测试脚本诊断问题
3. **检查日志**: 查看生成的`.log`文件
4. **查看代码**: 关键文件都有详细注释

---

## 📖 文档说明

- 📄 **README.md** - 本文件，项目导航
- 📄 **QUICK_START_GUIDE.md** - 5分钟快速上手 ⭐
- 📄 **ASTRUM_PROJECT_OVERVIEW.md** - 完整项目文档
- 📄 **ASTRUM_ARCHITECTURE.md** - 技术架构详解

**建议**: 新用户从快速开始指南开始，需要深入了解时查看项目概述。

---

*最后更新: 2024年12月 - 网络模块重构完成*

