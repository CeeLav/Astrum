# Change: 重构时间同步逻辑

## Why

当前 `ClientLSController` 中维护了 `_serverTimeDiff` 字段及其计算逻辑，这导致时间同步逻辑分散在多个地方。将时间同步逻辑集中到 `TimeInfo` 类中可以：
- 提高代码的内聚性和可维护性
- 统一时间同步的管理入口
- 简化 `ClientLSController` 的职责

## What Changes

- 将 `ClientLSController` 中的 `_serverTimeDiff` 计算逻辑（包括平滑算法）移动到 `TimeInfo` 类
- 修改 `TimeInfo.ServerNow()` 方法，改为使用内部的 `_serverTimeDiff` 计算（`ClientNow() + _serverTimeDiff`）
- 在 `TimeInfo` 中添加 `UpdateServerTime(long serverTimestamp)` 方法，用于更新服务器时间差
- 更新 `ClientLSController` 使用 `TimeInfo` 的新方法，移除内部的 `_serverTimeDiff` 字段和相关逻辑
- 更新所有使用 `ClientLSController._serverTimeDiff` 的代码，改为使用 `TimeInfo` 的方法

## Impact

- 影响的规范：时间同步系统（新增）
- 影响的代码：
  - `AstrumProj/Assets/Script/CommonBase/TimeInfo/TimeInfo.cs` - 添加时间差计算逻辑
  - `AstrumProj/Assets/Script/AstrumLogic/Core/ClientLSController.cs` - 移除时间差相关代码，使用 `TimeInfo` 的方法
  - `AstrumProj/Assets/Script/AstrumClient/UI/LatencyDisplay.cs` - 更新对时间差的访问方式
  - `AstrumProj/Assets/Script/AstrumClient/Managers/GameModes/Handlers/FrameSyncHandler.cs` - 更新调用方式
