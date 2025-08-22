# AstrumConfig

Astrum项目的配置和协议数据目录。

## 目录结构

```
AstrumConfig/
├── Proto/              # Protocol Buffer协议文件
│   ├── test_C_1.proto  # 测试客户端协议
│   └── game_S_100.proto # 游戏服务端协议
└── README.md           # 本文件
```

## Proto文件命名规范

Proto文件必须按照以下格式命名：`name_C_1.proto`

- `name`: 协议名称（如：test, game, user等）
- `C`: 使用范围标识
  - `C`: 仅客户端使用
  - `S`: 仅服务端使用  
  - `CS`: 客户端和服务端都使用
- `1`: 起始opcode编号

## 协议文件格式

### 基本结构

```protobuf
// 协议说明
// ResponseType: ResponseMessageName

message RequestMessage
{
    int32 id = 1; // 字段说明
    string data = 2; // 字段说明
}

message ResponseMessage
{
    bool success = 1; // 字段说明
    string message = 2; // 字段说明
}
```

### 特殊注释

- `// ResponseType: MessageName` - 指定响应消息类型
- `// no dispose` - 在消息结束的`}`后添加，表示不自动生成Dispose方法

### 支持的数据类型

- 基本类型：`int32`, `int64`, `uint32`, `uint64`, `bool`, `string`, `bytes`
- 集合类型：`repeated Type`, `map<KeyType, ValueType>`
- 嵌套消息：`message Type`

## 代码生成

Proto文件通过Proto2CS工具自动生成C#代码：

1. 运行Proto2CS工具
2. 自动生成对应的C#消息类
3. 生成的代码包含：
   - MemoryPack序列化属性
   - 对象池管理方法
   - Dispose方法
   - Opcode常量定义

## 注意事项

- 保持协议版本兼容性
- 字段编号不要随意修改
- 添加新字段时使用新的编号
- 删除字段时保留编号，标记为deprecated
- 定期更新协议文档
