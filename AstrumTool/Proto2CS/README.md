# Proto2CS 工具

这是一个用于将Protocol Buffer (.proto) 文件转换为C#代码的工具，专门为Astrum项目设计。

## 功能特性

- 自动生成C#消息类
- 支持MemoryPack序列化
- 自动生成对象池管理代码
- 支持ResponseType属性
- 自动生成opcode常量

## 使用方法

### 1. 准备Proto文件

Proto文件需要按照特定格式命名：`name_C_1.proto`

- `name`: 协议名称
- `C`: 表示客户端使用 (C=Client, S=Server, CS=ClientServer)
- `1`: 起始opcode编号

### 2. 文件格式

```protobuf
// 测试消息定义
// ResponseType: TestResponse

message TestMessage
{
    int32 id = 1; // 消息ID
    string content = 2; // 消息内容
    repeated int32 numbers = 3; // 数字列表
    map<string, int32> data = 4; // 键值对数据
}

message TestResponse
{
    bool success = 1; // 是否成功
    string message = 2; // 响应消息
}
```

### 3. 运行工具

```bash
cd AstrumTool/Proto2CS
dotnet run
```

### 4. 输出

工具会在以下目录生成C#代码：
- `AstrumProj/Assets/Script/Network/Generated/ClientServer/Message/`

**注意**: 为了避免重复，所有生成的代码都统一放在ClientServer目录中，客户端和服务端都可以使用。

## 生成代码特性

- 自动添加MemoryPack属性
- 自动生成Create方法用于对象池
- 自动生成Dispose方法
- 自动生成opcode常量类
- 支持注释转换为XML文档注释

## 注意事项

- 确保Proto文件在`AstrumConfig/Proto/`目录中
- 文件名必须严格按照格式命名
- 生成的代码依赖Astrum.CommonBase命名空间
