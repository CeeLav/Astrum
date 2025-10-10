# 💻 开发指南

## 📖 文档列表

### 服务器相关

#### [服务器配置](Server-Setup%20服务器配置.md)
服务器环境搭建和配置说明

#### [服务器使用说明](Server-Usage%20服务器使用说明.md)
服务器的启动、配置和使用方法

#### [LogicCore集成说明](LogicCore-Integration%20LogicCore集成说明.md)
LogicCore 逻辑层在服务器中的集成方式

#### [服务器依赖](Server-Dependencies%20服务器依赖.md)
服务器项目的依赖包和配置

### 测试相关

#### [测试快速开始](Test-Quick-Start%20测试快速开始.md)
快速开始编写和运行测试

#### [测试常用命令](Test-Commands%20测试常用命令.md)
常用的测试命令速查

#### [单个测试用例指南](Test-Single-Case%20单个测试用例指南.md)
如何运行单个测试用例

---

## 🚀 快速开始

### 启动服务器
```bash
# Windows
start_server.bat

# 或手动启动
cd AstrumServer/AstrumServer
dotnet run
```

### 运行测试
```bash
# 运行所有测试
cd AstrumTest/AstrumTest
dotnet test

# 运行单个测试类
dotnet test --filter ClassName=SkillEffectIntegrationTests
```

### 生成配置
```bash
# 客户端配置
cd AstrumConfig/Tables
gen_client.bat

# 服务器配置
gen.bat
```

---

## 🔗 相关文档

- [配置系统](../06-Configuration%20配置系统/) - 配置表生成和使用
- [技术参考](../08-Technical%20技术参考/) - 技术实现细节
- [项目快速开始](../../QUICK_START.md) - 项目级快速开始

---

## 📚 开发资源

### 工具目录
- `AstrumTool/Luban/` - 配置生成工具
- `AstrumTool/Proto2CS/` - 协议代码生成

### 测试项目
- `AstrumTest/AstrumTest/` - 单元测试和集成测试

### 服务器项目
- `AstrumServer/AstrumServer/` - .NET 服务器

---

**返回**: [文档中心](../README.md)

