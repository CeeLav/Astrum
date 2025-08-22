# 服务器依赖配置说明

## 概述

AstrumServer 现在直接依赖 Unity 客户端工程的三个核心程序集，实现了代码复用和类型一致性。

## 依赖的程序集

### 1. AstrumLogic.dll
- **路径**: `AstrumProj\Library\ScriptAssemblies\AstrumLogic.dll`
- **内容**: 游戏核心逻辑
- **主要命名空间**: `Astrum.LogicCore`
- **关键类**:
  - `FrameSync.FrameBuffer` - 帧同步缓冲区
  - `Core.Entity` - 游戏实体
  - `Core.World` - 游戏世界

### 2. CommonBase.dll
- **路径**: `AstrumProj\Library\ScriptAssemblies\CommonBase.dll`
- **内容**: 公共基础功能
- **主要命名空间**: `Astrum.CommonBase`
- **关键类**:
  - `ASLogger` - 日志系统
  - `TimeInfo` - 时间管理
  - `ObjectPool` - 对象池
  - `Singleton<T>` - 单例基类

### 3. Network.dll
- **路径**: `AstrumProj\Library\ScriptAssemblies\Network.dll`
- **内容**: 网络通信框架
- **主要命名空间**: `Astrum.Network`, `Astrum.Network.Generated`
- **关键类**:
  - `Session` - 网络会话
  - `NetworkMessage` - 网络消息
  - `TService` - TCP服务

## NuGet 依赖包

```xml
<PackageReference Include="MemoryPack" Version="1.21.4" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="System.Numerics.Vectors" Version="4.6.1" />
```

## 配置文件示例

### AstrumServer.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
    <PackageReference Include="MemoryPack" Version="1.21.4" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="System.Numerics.Vectors" Version="4.6.1" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="AstrumLogic">
      <HintPath>..\..\AstrumProj\Library\ScriptAssemblies\AstrumLogic.dll</HintPath>
    </Reference>
    <Reference Include="CommonBase">
      <HintPath>..\..\AstrumProj\Library\ScriptAssemblies\CommonBase.dll</HintPath>
    </Reference>
    <Reference Include="Network">
      <HintPath>..\..\AstrumProj\Library\ScriptAssemblies\Network.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

## 构建和部署流程

### 1. 开发环境构建
```bash
# 1. 首先编译Unity项目（生成程序集dll）
# 在Unity Editor中: Build -> Compile Scripts

# 2. 编译服务器项目
cd AstrumServer
dotnet build

# 3. 运行服务器
dotnet run --project AstrumServer
```

### 2. 生产环境部署
```bash
# 1. 确保Unity程序集dll文件存在
# 复制以下文件到服务器:
# - AstrumLogic.dll
# - CommonBase.dll  
# - Network.dll

# 2. 发布服务器项目
dotnet publish -c Release -o ./publish

# 3. 部署到目标服务器
# 确保Unity程序集dll在正确路径下
```

## 依赖测试

服务器启动时会自动运行依赖测试：

```csharp
// DependencyTest.cs 提供的测试功能:
DependencyTest.RunAllTests();

// 测试内容:
// ✓ CommonBase程序集 (ASLogger, TimeInfo)
// ✓ AstrumLogic程序集 (FrameBuffer)
// ⚠ Network程序集 (MemoryPack序列化可能有兼容性问题)
```

## 已知问题和解决方案

### 1. MemoryPack序列化问题
**问题**: Unity的MemoryPack版本与.NET版本可能不兼容
**解决方案**: 
- 使用Newtonsoft.Json作为备选序列化方案
- 或升级到兼容的MemoryPack版本

### 2. 路径依赖问题
**问题**: Unity程序集dll路径可能变化
**解决方案**:
- 使用相对路径引用
- 在CI/CD中确保路径正确性
- 考虑将dll复制到固定位置

### 3. 版本同步问题
**问题**: Unity程序集更新后服务器需要重新编译
**解决方案**:
- 建立自动化构建流程
- 版本控制Unity程序集dll
- 使用符号链接或脚本自动同步

## 优势

1. **代码复用**: 减少重复代码，统一维护
2. **类型一致**: 客户端和服务器使用相同的数据结构
3. **简化开发**: 网络消息定义只需维护一份
4. **提高效率**: 避免手动同步数据结构

## 注意事项

1. **构建顺序**: 必须先编译Unity项目，再编译服务器项目
2. **依赖管理**: Unity程序集更新时需要重新编译服务器
3. **部署要求**: 生产环境需要包含Unity程序集dll文件
4. **版本兼容**: 确保Unity和.NET版本的第三方库兼容性

---

**最后更新**: 2024年12月  
**适用版本**: Unity 6000.2.0b7, .NET 9.0

