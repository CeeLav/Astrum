## ADDED Requirements

### Requirement: 时间同步管理
`TimeInfo` 类 SHALL 负责管理客户端与服务器之间的时间差计算和同步。

#### Scenario: 更新服务器时间戳
- **WHEN** 调用 `TimeInfo.Instance.UpdateServerTime(serverTimestamp)`
- **THEN** `TimeInfo` 计算新的时间差（`ClientNow() - serverTimestamp`）
- **AND** 使用平滑算法更新内部 `_serverTimeDiff` 字段
- **AND** 首次调用时直接设置时间差，后续调用使用指数平滑算法

#### Scenario: 获取服务器时间
- **WHEN** 调用 `TimeInfo.Instance.ServerNow()`
- **THEN** 返回 `ClientNow() + _serverTimeDiff`
- **AND** 时间差由 `UpdateServerTime()` 方法维护

## MODIFIED Requirements

### Requirement: ClientLSController 时间同步
`ClientLSController` SHALL 通过 `TimeInfo` 管理时间同步，不再维护内部的时间差字段。

#### Scenario: 更新服务器时间
- **WHEN** `ClientLSController.UpdateServerTime(serverTimestamp)` 被调用
- **THEN** 该方法调用 `TimeInfo.Instance.UpdateServerTime(serverTimestamp)`
- **AND** `ClientLSController` 不再维护 `_serverTimeDiff` 字段

#### Scenario: 使用服务器时间
- **WHEN** `ClientLSController` 需要获取服务器时间
- **THEN** 使用 `TimeInfo.Instance.ServerNow()` 方法
- **AND** 不再直接访问 `_serverTimeDiff` 字段
