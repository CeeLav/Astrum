## 1. 实施

- [x] 1.1 在 `TimeInfo` 类中添加 `_serverTimeDiff` 字段及相关平滑算法常量
- [x] 1.2 在 `TimeInfo` 类中实现 `UpdateServerTime(long serverTimestamp)` 方法，包含平滑计算逻辑
- [x] 1.3 修改 `TimeInfo.ServerNow()` 方法，改为 `ClientNow() + _serverTimeDiff`
- [x] 1.4 在 `ClientLSController` 中移除 `_serverTimeDiff` 字段及相关常量
- [x] 1.5 修改 `ClientLSController.UpdateServerTime()` 方法，改为调用 `TimeInfo.Instance.UpdateServerTime()`
- [x] 1.6 更新 `ClientLSController` 中所有使用 `_serverTimeDiff` 的地方，改为使用 `TimeInfo.Instance.ServerNow()` 或相应方法
- [x] 1.7 更新 `LatencyDisplay.cs` 中对 `_serverTimeDiff` 的访问
- [x] 1.8 更新 `FrameSyncHandler.cs` 中的调用方式（如果需要）

## 2. 验证

- [x] 2.1 编译项目确保无编译错误
- [x] 2.2 运行游戏验证时间同步功能正常
- [x] 2.3 检查所有使用 `ServerNow()` 的地方行为保持一致
