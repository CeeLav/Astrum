# Capability: Game Mode

## MODIFIED Requirements

### Requirement: 游戏会话生命周期管理

GameMode SHALL 管理游戏会话的完整生命周期，包括逻辑线程的创建和销毁。

**原行为**:
- GameMode.StartGame() 调用 Room.Update() 在主线程
- GameMode.Update() 调用 Room.Update()
- GameMode.Shutdown() 清理 Room 资源

**新行为**:
- GameMode.StartGame() 创建并启动 LogicThread
- LogicThread 在独立线程调用 Room.Update()
- GameMode.Update() 仅处理主线程任务（Input, UI, View）
- GameMode.Shutdown() 停止 LogicThread 并等待线程退出

#### Scenario: 启动游戏时创建逻辑线程

- **WHEN** GameMode.StartGame(sceneId) 被调用
- **THEN** LogicThread 被创建并配置（Room, TickRate）
- **AND** LogicThread.Start() 被调用
- **AND** 逻辑线程开始以 20Hz 运行

#### Scenario: 游戏运行中主线程不再调用 Room.Update

- **WHEN** 逻辑线程模式启用
- **AND** GameMode.Update(deltaTime) 被调用
- **THEN** Room.Update() 不在主线程调用
- **AND** 主线程仅处理 InputManager, UIManager, ViewLayer

#### Scenario: 关闭游戏时等待逻辑线程退出

- **WHEN** GameMode.Shutdown() 被调用
- **THEN** LogicThread.Stop() 被调用
- **AND** 主线程等待逻辑线程退出（最多 5 秒）
- **AND** 如果超时则强制终止线程并记录警告
- **AND** Room 资源被清理

#### Scenario: 单线程模式向后兼容

- **WHEN** `LogicThreadingEnabled` 为 `false`
- **AND** GameMode.StartGame() 被调用
- **THEN** LogicThread 不被创建
- **AND** GameMode.Update() 继续在主线程调用 Room.Update()（旧行为）

## ADDED Requirements

_无_

## REMOVED Requirements

_无_

