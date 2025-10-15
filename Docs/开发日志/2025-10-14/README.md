# 2025-10-14 开发日志

## 📦 ThirdPart资源提取工具开发

### 背景
ArtRes仓库因包含大量ThirdPart第三方资源包（约2万+文件）而体积过大。这些资源包中大部分是Demo场景、示例和文档，实际游戏中只使用了很小一部分。需要提取实际使用的资源到独立目录，以便分离管理。

### 需求分析
- **目标**：从ThirdPart中提取实际被游戏使用的资源
- **方法**：基于YooAsset资源清单分析依赖关系
- **输出**：移动到 `Assets/ArtRes/GameAssets` 目录
- **约束**：
  - 排除场景文件（避免引用过多）
  - 保持ThirdPart原始结构完整
  - Unity自动更新所有引用

### 实现方案演进

#### 方案1：复制+更新GUID引用（已废弃）
- 复制ThirdPart资源到GameAssets（生成新GUID）
- 遍历所有资源文件，替换GUID引用
- **问题**：
  - 复制后ThirdPart内部Demo会引用外部资源
  - 需要手动处理GUID替换，复杂且容易出错
  - 性能开销大（需遍历所有资源文件）

#### 方案2：直接移动资源（最终方案）
- 使用 `AssetDatabase.MoveAsset()` 移动资源
- **优势**：
  - ✅ GUID保持不变
  - ✅ Unity自动更新所有引用
  - ✅ 简单、快速、安全
  - ✅ 不需要手动处理引用

### 关键技术点

#### 1. 依赖分析
使用 `AssetDatabase.GetDependencies()` 递归分析：
```csharp
// 从JSON清单读取源头资源（排除.unity场景文件）
foreach (var asset in manifest.AssetList)
{
    if (!asset.AssetPath.EndsWith(".unity"))
    {
        _sourceAssets.Add(asset.AssetPath);
    }
}

// 递归分析所有依赖
string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);

// 过滤出ThirdPart资源
if (dep.StartsWith("Assets/ArtRes/ThirdPart"))
{
    _thirdPartDependencies.Add(dep);
}
```

#### 2. 目录创建问题修复
**问题**：使用 `Directory.CreateDirectory()` 导致错误：
```
Parent directory is not in asset database
```

**原因**：Unity的AssetDatabase不识别通过.NET文件API创建的目录。

**解决方案**：递归使用 `AssetDatabase.CreateFolder()`
```csharp
private void EnsureDirectoryExists(string path)
{
    if (AssetDatabase.IsValidFolder(path))
        return;
    
    // 递归确保父目录存在
    string parentPath = Path.GetDirectoryName(path).Replace("\\", "/");
    if (parentPath != "Assets" && !AssetDatabase.IsValidFolder(parentPath))
    {
        EnsureDirectoryExists(parentPath);
    }
    
    // 使用AssetDatabase API创建目录
    string folderName = Path.GetFileName(path);
    AssetDatabase.CreateFolder(parentPath, folderName);
}
```

#### 3. 资源移动
```csharp
// 计算新路径（保持ThirdPart相对结构）
string relativePath = oldPath.Substring("Assets/ArtRes/ThirdPart/".Length + 1);
string newPath = Path.Combine("Assets/ArtRes/GameAssets", relativePath);

// 确保目录存在
EnsureDirectoryExists(Path.GetDirectoryName(newPath));

// 移动资源（GUID不变，引用自动更新）
string error = AssetDatabase.MoveAsset(oldPath, newPath);
```

### 工具功能

**文件位置**：`AstrumProj/Assets/Script/Editor/Tools/ThirdPartAssetExtractor.cs`

**菜单位置**：Unity → `Astrum/Asset 资源管理/Extract ThirdPart Assets 提取ThirdPart资源`

**工作流程**：
1. 点击"一键分析并移动资源"按钮
2. 读取YooAsset清单（排除场景文件）
3. 递归分析所有依赖关系
4. 过滤ThirdPart资源
5. 移动到GameAssets目录（保持目录结构）
6. Unity自动更新所有引用

**特性**：
- ✅ 排除场景文件依赖（避免引用过多）
- ✅ 保持目录结构
- ✅ GUID不变，引用自动更新
- ✅ 详细的进度显示和日志
- ✅ 显示将要移动的资源列表

### 最终效果

**移动前**：
```
Assets/ArtRes/ThirdPart/PolygonDungeon/Models/Wall.fbx (GUID: xxx)
```

**移动后**：
```
Assets/ArtRes/GameAssets/PolygonDungeon/Models/Wall.fbx (GUID: xxx 保持不变)
Assets/ArtRes/ThirdPart/PolygonDungeon/Models/ (Wall.fbx已删除)
```

所有预制体、材质中对该资源的引用自动更新到新位置！

### 注意事项
1. ⚠️ **移动操作不可逆**：建议先备份项目或提交git
2. ⚠️ **ThirdPart资源会被删除**：移动后ThirdPart中的资源会消失
3. ✅ **场景引用保持不变**：因为分析时排除了场景，场景仍引用ThirdPart原始位置
4. ✅ **Unity自动处理**：所有引用更新由Unity自动完成，无需手动干预

### 技术收获
1. **Unity资源管理API**：必须使用 `AssetDatabase.CreateFolder()` 而不是 `Directory.CreateDirectory()`
2. **资源移动机制**：`AssetDatabase.MoveAsset()` 保持GUID不变，Unity自动更新引用
3. **依赖分析**：`AssetDatabase.GetDependencies()` 可以递归获取所有依赖
4. **YooAsset清单**：JSON清单只包含显式收集的资源，不包含依赖（依赖需要手动分析）

### 代码统计
- 新增文件：1个（ThirdPartAssetExtractor.cs）
- 代码行数：约340行
- 核心方法：
  - `AnalyzeDependencies()` - 依赖分析
  - `MoveAssets()` - 资源移动
  - `EnsureDirectoryExists()` - 递归创建目录

---

## 📊 数值系统设计

### 背景
当前游戏的数值体系不完善，伤害计算使用写死的固定值，没有运行时属性系统，Buff、成长、装备等系统无法接入。需要设计完整的数值体系，包括静态配置、运行时组件和计算公式。

### 设计方案

#### 三层属性架构
```
BaseStatsComponent（基础属性）
    ↓
DerivedStatsComponent（派生属性 = 基础 + 修饰器）
    ↓
DynamicStatsComponent（动态资源 = HP/MP等当前值）
```

#### 核心组件设计（7个）

**属性组件**：
1. **BaseStatsComponent** - 基础属性（配置表+成长+加点）
2. **DerivedStatsComponent** - 最终属性（基础+Buff+装备）
3. **DynamicStatsComponent** - 动态资源（当前HP/MP/能量/怒气等）

**辅助组件**：
4. **BuffComponent** - Buff管理（叠加、过期、修饰器提取）
5. **StateComponent** - 状态标志（晕眩、无敌、死亡等）
6. **LevelComponent** - 等级经验管理
7. **GrowthComponent** - 自由加点系统

#### 字典+枚举设计

**优势**：
- ✅ 灵活扩展：添加新属性只需扩展枚举
- ✅ 紧凑存储：字典只存储有值的属性
- ✅ 类型安全：通过枚举访问
- ✅ 易于序列化：MemoryPack直接支持

**核心代码**：
```csharp
// StatType枚举定义
public enum StatType
{
    HP = 1, ATK = 2, DEF = 3, SPD = 4,
    CRIT_RATE = 10, CRIT_DMG = 11,
    ELEMENT_FIRE = 30, ELEMENT_ICE = 31, // 可扩展
}

// Stats通用属性容器
public class Stats
{
    private Dictionary<StatType, float> _values = new();
    
    public float Get(StatType type) => _values.TryGetValue(type, out var v) ? v : 0f;
    public void Set(StatType type, float value) => _values[type] = value;
    public void Add(StatType type, float delta) { ... }
}

// 组件使用Stats容器
public class BaseStatsComponent : BaseComponent
{
    public Stats BaseStats { get; set; } = new Stats();
}
```

#### 修饰器系统

**三种修饰器类型**：
1. **Flat**（固定加成）：+50攻击
2. **Percent**（百分比加成）：+20%攻击
3. **FinalMultiplier**（最终乘数）：×1.5伤害

**计算公式**：
```
最终属性 = (基础 + Flat) × (1 + Percent) × FinalMultiplier

示例：
基础攻击 = 100
Buff1: +20攻击（Flat）
Buff2: +30%攻击（Percent）
Buff3: ×1.5伤害（FinalMultiplier）

最终攻击 = (100 + 20) × (1 + 0.3) × 1.5 = 234
```

#### 完整伤害计算公式

```
1. 基础伤害 = 攻击力 × 技能倍率
2. 命中判定（命中率 - 闪避率）
3. 格挡判定（格挡率，减少固定值）
4. 暴击判定（暴击率，倍率翻倍）
5. 防御减免 = DEF / (DEF + 100)
6. 抗性减免（物理/魔法抗性）
7. 随机浮动（±5%）
```

#### 配置表扩展

**RoleBaseTable扩展**（+11个字段）：
```csv
baseCritRate, baseCritDamage, baseAccuracy, baseEvasion,
baseBlockRate, baseBlockValue, physicalRes, magicalRes,
baseMaxMana, manaRegen, healthRegen
```

**新增BuffTable**：
```csv
buffId, buffName, buffType, duration, stackable, maxStack,
modifiers, tickDamage, tickInterval

# 示例：
5001,力量祝福,1,600,true,3,ATK:Percent:0.2;SPD:Flat:1.0,0,0
```

**修饰器字符串格式**：
```
"ATK:Percent:0.2;SPD:Flat:1.0;CRIT_RATE:Flat:0.05"
解析为：
  - ATK +20%
  - SPD +1.0
  - CRIT_RATE +5%
```

### 技术亮点

1. **字典+枚举架构**：灵活可扩展，易于维护
2. **三层属性分离**：职责清晰，计算高效
3. **修饰器系统**：支持复杂的加成计算
4. **Buff叠加机制**：支持层数、时间刷新
5. **完整伤害公式**：命中/闪避/格挡/暴击/防御/抗性全覆盖

### 文档产出

**主文档**：`Docs/05-CoreArchitecture 核心架构/Stats-System 数值系统.md`（约2000行）

**包含内容**：
- 系统概述和设计理念
- 7个组件详细设计
- 完整伤害计算公式
- 配置表扩展方案
- 成长和Buff系统设计
- 数值平衡建议
- 使用示例和集成方案
- 实现清单（17个任务）

---

## 总结
今日完成两个重要工具/系统的设计：

1. **ThirdPart资源提取工具**（+340行代码）
   - 基于YooAsset清单的依赖分析
   - 自动移动资源到GameAssets
   - 解决了目录创建的Unity API问题

2. **数值系统完整设计**（+2000行文档）
   - 7个核心组件设计
   - 字典+枚举灵活架构
   - 完整伤害计算公式
   - 配置表扩展方案

这两个系统为后续的AI、音频、特效等功能奠定了坚实基础。

