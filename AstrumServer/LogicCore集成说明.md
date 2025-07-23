# LogicCore 服务器端集成说明

## 集成完成！

LogicCore程序集已成功集成到AstrumServer项目中。

## 集成内容

### 1. 项目引用
- 在 `AstrumServer.csproj` 中添加了对LogicCore.dll的引用
- 引用路径：`..\..\AstrumProj\Library\ScriptAssemblies\LogicCore.dll`

### 2. 代码集成
- 导入了 `using Astrum.LogicCore;`
- 集成了 `GameStateManager` 单例
- 集成了 `PlayerManager` 玩家管理
- 集成了 `GameConfig` 配置管理

### 3. 功能增强

#### 游戏状态管理
- 服务器启动时自动初始化游戏逻辑
- 监听游戏状态变化并记录日志
- 支持游戏状态转换

#### 玩家管理
- 客户端连接时自动添加到游戏逻辑
- 客户端断开时自动从游戏逻辑移除
- 监听玩家加入/离开事件

#### 命令系统增强
- `help` - 显示帮助信息
- `status` - 显示服务器状态和游戏状态
- `players` - 显示当前玩家列表
- `move <x> <y> <z>` - 移动玩家
- `jump` - 玩家跳跃
- `quit` - 退出连接

#### 事件系统
- 游戏状态改变事件
- 玩家加入/离开事件
- 玩家位置更新事件

## 测试方法

### 1. 启动服务器
```powershell
cd AstrumServer\AstrumServer
dotnet run
```

### 2. 运行测试脚本
```powershell
cd AstrumServer
.\test_logiccore.ps1
```

### 3. 手动测试
使用telnet或PowerShell连接到服务器：
```powershell
telnet localhost 8888
```

## 测试命令示例

```
help                    # 查看帮助
status                  # 查看服务器状态
players                 # 查看玩家列表
move 1 0 0             # 向右移动
jump                   # 跳跃
move 0 0 5             # 向前移动
players                # 再次查看玩家列表
quit                   # 退出
```

## 集成优势

1. **逻辑一致性**: 客户端和服务器端使用相同的游戏逻辑
2. **状态同步**: 服务器维护统一的游戏状态
3. **事件驱动**: 通过事件系统实现组件间通信
4. **配置管理**: 统一的游戏配置管理
5. **可扩展性**: 易于添加新的游戏功能

## 日志输出示例

```
info: AstrumServer.GameServer[0]
      LogicCore游戏逻辑已初始化
info: AstrumServer.GameServer[0]
      游戏配置: 最大玩家数=16, 端口=8888
info: AstrumServer.GameServer[0]
      游戏状态从 None 变为 Loading
info: AstrumServer.GameServer[0]
      游戏状态从 Loading 变为 MainMenu
info: AstrumServer.GameServer[0]
      玩家 Player_abc123 (ID: abc123) 加入了游戏
info: AstrumServer.GameServer[0]
      玩家 Player_abc123 位置更新: (1.00, 0.00, 0.00)
```

## 注意事项

1. 确保Unity项目已编译LogicCore程序集
2. 服务器启动时会自动初始化游戏逻辑
3. 所有玩家操作都会通过LogicCore处理
4. 游戏状态变化会记录到日志中
5. 支持多客户端同时连接和操作

## 下一步开发

1. 添加更多游戏命令
2. 实现房间/大厅系统
3. 添加游戏规则和逻辑
4. 实现数据持久化
5. 添加网络同步功能 