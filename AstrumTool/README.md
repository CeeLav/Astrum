# AstrumTool

Astrum项目的开发工具集合。

## 工具列表

### 1. Proto2CS

Protocol Buffer到C#代码生成工具。

**位置**: `AstrumTool/Proto2CS/`

**功能**:
- 将.proto文件转换为C#消息类
- 自动生成MemoryPack序列化代码
- 自动生成对象池管理代码
- 支持ResponseType属性
- 自动生成opcode常量

**使用方法**:
```bash
cd AstrumTool/Proto2CS
dotnet run
```

**输入**: `AstrumConfig/Proto/` 目录中的.proto文件
**输出**: 
- **Unity客户端**: `AstrumProj/Assets/Script/Generated/Message/` 目录中的C#代码
- **服务器端**: `AstrumServer/AstrumServer/Generated/` 目录中的C#代码

## 项目结构

```
AstrumTool/
├── Proto2CS/           # 协议代码生成工具
│   ├── Proto2CS.cs     # 主要代码生成逻辑
│   ├── Program.cs      # 程序入口
│   ├── Proto2CS.csproj # 项目文件
│   ├── README.md       # 工具说明
│   └── run.bat         # Windows运行脚本
└── README.md           # 本文件
```

## 输出目录结构

### Unity客户端
```
AstrumProj/Assets/Script/Generated/
└── Message/            # 生成的网络消息代码
    ├── networkcommon_C_1.cs      # 网络通用消息
    ├── gamemessages_C_20.cs      # 游戏消息
    ├── connectionstatus_C_10.cs  # 连接状态消息
    ├── game_S_100.cs             # 游戏服务端消息
    └── test_C_1.cs               # 测试消息
```

### 服务器端
```
AstrumServer/AstrumServer/
└── Generated/          # 生成的网络消息代码
    ├── networkcommon_C_1.cs      # 网络通用消息
    ├── gamemessages_C_20.cs      # 游戏消息
    ├── connectionstatus_C_10.cs  # 连接状态消息
    ├── game_S_100.cs             # 游戏服务端消息
    └── test_C_1.cs               # 测试消息
```

## 开发说明

- 所有工具都使用.NET 8.0
- 工具设计为独立运行，不依赖Unity环境
- 生成的代码遵循Astrum项目的编码规范
- 支持跨平台运行（Windows/Linux/macOS）

## 扩展新工具

要添加新工具，请：

1. 在`AstrumTool/`下创建新目录
2. 创建对应的.csproj项目文件
3. 实现工具逻辑
4. 添加README说明文档
5. 更新本README文件
