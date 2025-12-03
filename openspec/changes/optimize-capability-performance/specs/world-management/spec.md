# Spec Delta: World Management - Physics Query Support

## MODIFIED Requirements

### Requirement: BepuPhysicsWorld 应提供空间范围查询方法

BepuPhysicsWorld SHALL 提供 AABB 或球形范围查询方法，用于快速查询指定区域内的实体。查询应利用 BEPU 内部的空间索引结构（BroadPhase）。

**Rationale**: 项目已使用 BEPU 物理引擎，其内部已有高效的空间索引（DynamicHierarchy）。复用该索引比创建新的空间索引系统更简单高效。

**Priority**: P0

#### Scenario: 查询指定范围内的实体

**Given** World 中有 N 个实体分布在不同位置  
**When** 查询某个位置半径 R 范围内的实体  
**Then** 应仅返回真正在范围内的实体  
**And** 查询时间应与总实体数 N 无关（仅与范围内实体数 k 相关）  
**And** k << N 时查询时间应 << O(N) 全量遍历

#### Scenario: 实体位置变化时更新空间索引

**Given** 一个实体已注册到空间索引  
**When** 实体位置发生变化  
**Then** 空间索引应自动更新该实体的网格位置  
**And** 后续查询应能在新位置找到该实体  
**And** 在旧位置查询不应返回该实体

#### Scenario: 实体添加和移除时维护空间索引

**Given** World 的空间索引系统已初始化  
**When** 新实体被添加到 World  
**Then** 该实体应自动注册到空间索引  
**When** 实体从 World 移除  
**Then** 该实体应自动从空间索引注销  
**And** 后续查询不应返回已移除的实体

### Requirement: 空间索引应使用网格划分优化查询性能

空间索引 SHALL 使用固定大小网格（如 10×10 单位）划分 2D 空间，实体根据位置映射到对应网格，查询时仅检查相关网格。

**Rationale**: 2D 空间哈希是简单高效的空间索引结构，适合大多数游戏场景。

**Priority**: P0

#### Scenario: 使用固定大小网格划分空间

**Given** World 的 2D 空间  
**When** 初始化空间索引系统  
**Then** 应使用固定大小的网格（如 10×10 单位）划分空间  
**And** 每个实体应根据位置映射到对应网格  
**And** 查询时应仅检查目标位置周围的相关网格

#### Scenario: 查询性能与网格大小的关系

**Given** 实体均匀分布在空间中  
**When** 使用不同的网格大小（5, 10, 20 单位）  
**Then** 网格大小应根据典型查询半径优化  
**And** 网格过小会导致过多的网格遍历  
**And** 网格过大会导致单个网格内实体过多  
**And** 推荐网格大小 = 平均查询半径

### Requirement: 空间索引应支持高效的位置更新

空间索引 SHALL 支持 O(1) 复杂度的位置更新操作，仅在实体跨越网格边界时才更新索引，避免不必要的开销。

**Rationale**: 实体位置每帧可能变化，更新操作必须高效。

**Priority**: P0

#### Scenario: 批量更新实体位置

**Given** 多个实体在同一帧内位置发生变化  
**When** MovementCapability 更新所有实体位置  
**Then** 空间索引应支持批量更新操作  
**And** 批量更新应比逐个更新更高效  
**And** 更新操作应为 O(1) 复杂度（仅涉及网格变化的实体）

#### Scenario: 仅在跨网格时更新索引

**Given** 一个实体在网格内移动  
**When** 新旧位置在同一网格内  
**Then** 不应触发空间索引更新  
**When** 新旧位置跨越网格边界  
**Then** 才应更新空间索引（从旧网格移到新网格）

## MODIFIED Requirements

### Requirement: World 应在实体位置变化时维护空间索引

World SHALL 在实体添加、移除和位置变化时自动维护空间索引，确保索引始终与实体状态同步。

**Rationale**: 扩展现有的实体管理机制以支持空间索引。

**Priority**: P0

#### Scenario: 集成空间索引到实体生命周期

**Given** World 的实体管理系统  
**When** 调用 `World.AddEntity(entity)` 时  
**Then** 应自动将实体注册到空间索引  
**When** 调用 `World.RemoveEntity(entity)` 时  
**Then** 应自动从空间索引移除  
**When** 实体位置通过 `TransComponent` 变化时  
**Then** 应通知空间索引更新

## Performance Targets

### Target: 空间索引查询性能

- **基准**: 遍历 100 个实体需要 ~0.1ms
- **目标**: 空间索引查询 < 0.01ms（10x 提升）
- **场景**: 查询半径 10 单位，平均返回 3-5 个实体

### Target: 位置更新性能

- **目标**: 单次更新 < 0.001ms（1 微秒）
- **批量**: 100 个实体更新 < 0.05ms
- **开销**: 相比不使用空间索引，总体性能提升 > 50%

### Target: 内存开销

- **基准**: 不使用空间索引时的内存使用
- **目标**: 空间索引额外内存 < 1MB（1000 实体场景）
- **结构**: Dictionary<(int, int), HashSet<Entity>> 的内存占用

## Implementation Notes

### 空间索引实现要点

```csharp
public class SpatialIndexSystem
{
    // 网格大小（世界单位）
    private const int CELL_SIZE = 10;
    
    // 网格存储：(gridX, gridY) -> 该网格内的实体集合
    private Dictionary<(int, int), HashSet<Entity>> _grid;
    
    // 反向索引：entity -> 当前所在网格
    private Dictionary<long, (int, int)> _entityToGrid;
    
    // 将世界坐标转换为网格坐标
    private (int, int) WorldToGrid(TSVector position)
    {
        int gridX = (int)Math.Floor(position.x.AsFloat() / CELL_SIZE);
        int gridZ = (int)Math.Floor(position.z.AsFloat() / CELL_SIZE);
        return (gridX, gridZ);
    }
    
    // 查询指定范围内的实体
    public IEnumerable<Entity> QueryNearby(TSVector center, FP radius)
    {
        // 1. 计算查询范围覆盖的网格
        int gridRadius = (int)Math.Ceiling(radius.AsFloat() / CELL_SIZE);
        var centerGrid = WorldToGrid(center);
        
        // 2. 遍历相关网格（通常 3x3 或 5x5）
        for (int dx = -gridRadius; dx <= gridRadius; dx++)
        {
            for (int dz = -gridRadius; dz <= gridRadius; dz++)
            {
                var gridKey = (centerGrid.Item1 + dx, centerGrid.Item2 + dz);
                if (_grid.TryGetValue(gridKey, out var entities))
                {
                    foreach (var entity in entities)
                    {
                        // 3. 精确距离检查
                        var pos = entity.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
                        if ((pos - center).sqrMagnitude <= radius * radius)
                        {
                            yield return entity;
                        }
                    }
                }
            }
        }
    }
    
    // 更新实体位置
    public void UpdatePosition(Entity entity, TSVector oldPos, TSVector newPos)
    {
        var oldGrid = WorldToGrid(oldPos);
        var newGrid = WorldToGrid(newPos);
        
        // 仅在跨网格时更新
        if (oldGrid != newGrid)
        {
            // 从旧网格移除
            if (_grid.TryGetValue(oldGrid, out var oldSet))
            {
                oldSet.Remove(entity);
            }
            
            // 添加到新网格
            if (!_grid.TryGetValue(newGrid, out var newSet))
            {
                newSet = new HashSet<Entity>();
                _grid[newGrid] = newSet;
            }
            newSet.Add(entity);
            
            // 更新反向索引
            _entityToGrid[entity.UniqueId] = newGrid;
        }
    }
}
```

### 集成要点

1. **MovementCapability 通知**: 在 TransComponent.Position 变化后调用 `SpatialIndex.UpdatePosition()`
2. **惰性更新**: 仅在位置实际变化且跨网格时更新
3. **批量操作**: 考虑提供批量更新 API 以提高效率
4. **线程安全**: 当前为单线程，无需考虑线程安全

### 测试要点

1. **正确性**: 确保查询结果与暴力遍历一致
2. **性能**: Profile 查询和更新操作的耗时
3. **边界情况**: 网格边界、大半径查询、空网格
4. **内存**: 检查长时间运行后的内存使用

