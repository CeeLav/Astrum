# 2025-10-12 开发日志：碰撞盒系统优化与自动打表功能

## 📅 日期
2025年10月12日

## 🎯 主要工作

### 1. 碰撞盒偏移量功能 ✅

#### **需求**
支持碰撞盒相对于角色中心的偏移配置，使碰撞盒能够精确放置在需要的位置。

#### **实现**
- **格式扩展**：`Box:2.1x0.9x1.3@0,0.1,0.5`
  - 基础格式：`Box:宽x高x深`
  - 偏移格式：`@X,Y,Z`（可选）
  - 向后兼容：没有 `@` 则偏移量为零

- **解析器增强**：
  ```csharp
  // 分离偏移量部分
  int atIndex = collisionInfo.IndexOf('@');
  if (atIndex > 0)
  {
      mainPart = collisionInfo.Substring(0, atIndex);
      offsetPart = collisionInfo.Substring(atIndex + 1);
      localOffset = ParseOffset(offsetPart);
  }
  ```

- **编辑器UI**：
  - 新增"偏移量设置（高级）"面板
  - Vector3Field 输入偏移值
  - 实时预览碰撞盒位置
  - 智能提示偏移方向

#### **影响文件**
- `CollisionInfoParser.cs` - 运行时解析
- `CollisionInfoEditor.cs` - 编辑器UI
- `CollisionShape.cs` - 数据结构（已有 LocalOffset 字段）

---

### 2. 编辑器预览实时更新 ✅

#### **问题**
碰撞盒参数编辑后，动画预览中的碰撞盒不实时更新，必须保存刷新后才能看到变化。

#### **根因**
- `UpdateFrameCollisionInfo` 只从 `TriggerEffects` 读取数据
- `TriggerEffects` 只在保存/加载时同步
- UI修改只更新 `TimelineEvents`

#### **解决方案**
重构 `UpdateFrameCollisionInfo` 的数据读取优先级：
```csharp
// 优先从 TimelineEvents 中查找（最新数据）
var timelineEvent = _selectedSkillAction.TimelineEvents
    ?.FirstOrDefault(evt => evt.TrackType == "SkillEffect" && 
                           frame >= evt.StartFrame && 
                           frame <= evt.EndFrame);

// 回退：从 TriggerEffects 中查找（兼容旧数据）
if (timelineEvent == null)
{
    var frameData = _selectedSkillAction.TriggerEffects
        .FirstOrDefault(t => frame >= t.StartFrame && ...);
}
```

#### **影响文件**
- `SkillActionEditorWindow.cs`
- `EventDetailModule.cs` - 添加事件修改通知

---

### 3. 预览场景坐标系优化 ✅

#### **问题**
编辑器预览中，模型被移动到 `-bounds.center`，导致碰撞盒需要额外的坐标转换。

#### **优化方案**
**从"移动模型"改为"移动相机"**：

**修改前** ❌：
```csharp
// 移动模型到原点
_previewInstance.transform.position = -bounds.center;
_modelCenter = Vector3.zero;

// 相机看向原点
camera.LookAt(Vector3.zero);

// 碰撞盒需要传递模型位置补偿
DrawCollisionInfo(collisionInfo, camera, color, modelPosition);
```

**修改后** ✅：
```csharp
// 模型保持在原点
_previewInstance.transform.position = Vector3.zero;
_modelCenter = bounds.center;  // 保存实际中心

// 相机环绕模型中心
camera.LookAt(_modelCenter);

// 碰撞盒直接使用相对偏移
DrawCollisionInfo(collisionInfo, camera);
```

#### **优势**
- ✅ 角色坐标系统清晰（始终在原点）
- ✅ 碰撞盒偏移量语义明确
- ✅ 代码更简洁
- ✅ 易于调试

#### **影响文件**
- `BasePreviewModule.cs` - 基类预览模块
- `RolePreviewModule.cs` - 角色预览模块
- `CollisionShapePreview.cs` - 碰撞盒绘制

---

### 4. 运行时碰撞盒解析修复 ✅

#### **问题**
运行时 Scene 视图不显示攻击碰撞盒（实体碰撞盒正常）。

#### **根因**
`SkillConfigManager.ParseTriggerFrames` 使用简单的 `Split(',')` 分割字符串：
```
输入: Frame6-7:Collision(Box:2.1x0.9x1.3@0,0.7,0.8):4022

错误分割 ❌:
[0] = "Frame6-7:Collision(Box:2.1x0.9x1.3@0"
[1] = "0.7"
[2] = "0.8):4022"
```

#### **解决方案**
**智能分割算法**：
```csharp
private static string[] SplitIgnoringParentheses(string input, char separator)
{
    int parenthesesDepth = 0;
    // 只在括号外的分隔符才分割
    if (c == separator && parenthesesDepth == 0)
    {
        // 分割
    }
}
```

**智能冒号解析**：
```csharp
// 在右括号之后查找最后一个冒号
int lastParenIndex = trimmed.LastIndexOf(')');
int searchStartIndex = lastParenIndex > 0 ? lastParenIndex : firstColonIndex + 1;
int lastColonIndex = trimmed.IndexOf(':', searchStartIndex);
```

#### **影响文件**
- `SkillConfigManager.cs` - 运行时解析
- `SkillActionDataWriter.cs` - 编辑器验证
- `SkillActionEditorData.cs` - 编辑器解析

---

### 5. 技能动作保存逻辑优化 ✅

#### **问题**
技能动作编辑器保存后，会把 `ActionTable.csv` 中的非技能动作（idle, walk）删除。

#### **根因**
`WriteActionTableCSV` 直接用技能动作列表覆盖整个 ActionTable。

#### **解决方案**
**智能合并逻辑**：
```csharp
// 1. 读取现有的所有动作
var existingActions = ActionTableData.ReadAllActions();

// 2. 过滤出非技能动作
var nonSkillActions = existingActions
    .Where(a => !string.Equals(a.ActionType, "skill", ...))
    .ToList();

// 3. 合并：非技能动作 + 新的技能动作
var mergedActions = new List<ActionTableData>();
mergedActions.AddRange(nonSkillActions);
mergedActions.AddRange(skillActionTableData);

// 4. 按 ActionId 排序并写入
mergedActions = mergedActions.OrderBy(a => a.ActionId).ToList();
```

#### **结果**
- ✅ 非技能动作被保留
- ✅ 技能动作正常更新
- ✅ 数据完整性得到保证

#### **影响文件**
- `SkillActionDataWriter.cs`
- `ActionTableData.cs` - 添加 `ReadAllActions()` 方法

---

### 6. 自动打表功能 ✅

#### **需求**
所有表格保存后，自动执行 `gen_client.bat` 打表，生成最新的 `.bytes` 文件。

#### **实现**

##### **6.1 LubanTableGenerator 工具类**
```csharp
public static class LubanTableGenerator
{
    // 执行打表
    public static bool GenerateClientTables(bool showDialog = true, bool forceGenerate = false)
    {
        // 检查自动打表开关
        if (!forceGenerate && !AutoGenerateEnabled)
            return true;
        
        // 执行 gen_client.bat
        var process = new Process { ... };
        
        // 捕获输出并记录到 Console
        // 超时限制：30秒
        // 自动刷新 Unity 资源
    }
}
```

##### **6.2 集成到所有 Writer**
```csharp
if (success)
{
    Debug.Log($"{LOG_PREFIX} Successfully saved data");
    
    // 自动打表
    Core.LubanTableGenerator.GenerateClientTables(showDialog: false);
}
```

**集成位置**：
- ✅ SkillActionDataWriter
- ✅ ActionDataWriter
- ✅ RoleDataWriter
- ✅ SkillDataWriter
- ✅ EntityModelDataWriter

##### **6.3 菜单项**
- `Astrum → Tables → 生成客户端表格 Bytes` - 手动打表
- `Astrum → Tables → 自动打表开关` - 切换开关（带勾选标记）

##### **6.4 自动打表开关**
```csharp
public static bool AutoGenerateEnabled
{
    get => EditorPrefs.GetBool("Astrum.AutoGenerateTables", true);
    set => EditorPrefs.SetBool("Astrum.AutoGenerateTables", value);
}
```

默认**开启**，可通过菜单切换。

#### **工作流程**
```
保存CSV → 检查开关 → 执行gen_client.bat → 生成.bytes → 刷新Unity资源 → 完成
```

#### **影响文件**
- `LubanTableGenerator.cs` - 新建，核心工具类
- 所有 `*DataWriter.cs` - 添加自动打表调用

---

## 📊 代码统计

| 类别 | 新增文件 | 修改文件 | 代码行数 |
|------|---------|---------|---------|
| **碰撞盒偏移** | 0 | 3 | ~150 |
| **实时更新** | 0 | 2 | ~50 |
| **坐标系优化** | 0 | 3 | ~30 |
| **运行时解析** | 0 | 3 | ~150 |
| **保存逻辑** | 0 | 2 | ~50 |
| **自动打表** | 1 | 5 | ~221 |
| **总计** | **1** | **18** | **~651** |

---

## 🐛 解决的问题

1. ✅ 碰撞盒无法配置偏移量
2. ✅ 编辑器预览碰撞盒不实时更新
3. ✅ 预览场景坐标系混乱
4. ✅ 运行时攻击碰撞盒不显示
5. ✅ 保存技能动作删除其他动作
6. ✅ TriggerFrames 格式验证失败
7. ✅ 每次保存后需要手动打表

---

## 🎯 技术亮点

### 1. 智能字符串分割算法
**问题**：碰撞盒信息中包含多个分隔符（逗号、冒号）。

**解决**：追踪括号深度，只在括号外分割：
```csharp
int parenthesesDepth = 0;
foreach (char c in input)
{
    if (c == '(') parenthesesDepth++;
    else if (c == ')') parenthesesDepth--;
    else if (c == separator && parenthesesDepth == 0)
    {
        // 分割
    }
}
```

**应用位置**：
- `SkillActionEditorData.cs` - 编辑器解析
- `SkillConfigManager.cs` - 运行时解析
- `SkillActionDataWriter.cs` - 格式验证

### 2. 数据同步优先级设计
**TimelineEvents** (UI最新) → **TriggerEffects** (持久化) → **清空**

确保编辑器UI始终显示最新的碰撞盒数据。

### 3. 坐标系统重构
**从"移动模型适应相机"改为"移动相机适应模型"**：
- 模型固定在原点 (0,0,0)
- 相机环绕模型的实际中心
- 碰撞盒偏移量语义清晰

### 4. 进程管理与超时控制
打表进程：
- 异步输出捕获
- 30秒超时限制
- 自动资源清理
- 完整的错误处理

---

## ✅ 测试验证

### 1. 碰撞盒偏移量
- ✅ 编辑器UI正常显示和编辑
- ✅ 格式正确保存：`Box:2.1x0.9x1.3@0,0.7,0.8`
- ✅ 运行时正确解析和显示
- ✅ 向后兼容无偏移量格式

### 2. 实时更新
- ✅ 修改碰撞盒参数后立即显示
- ✅ 拖动时间轴时正确显示

### 3. 坐标系
- ✅ 模型始终在原点 (0,0,0)
- ✅ 碰撞盒相对原点偏移正确

### 4. 运行时
- ✅ Scene视图显示实体碰撞盒（绿色）
- ✅ Scene视图显示攻击碰撞盒（红色/黄色）

### 5. 保存逻辑
- ✅ 保存技能动作不删除 idle/walk
- ✅ ActionTable 包含所有动作类型

### 6. 自动打表
- ✅ 保存后自动打表
- ✅ Console 显示打表日志
- ✅ .bytes 文件正确生成
- ✅ Unity 资源自动刷新
- ✅ 开关可正常切换

---

## 🚀 后续优化建议

1. **碰撞盒可视化增强**
   - 支持旋转角度配置
   - 显示碰撞范围半透明区域
   - 支持多个碰撞盒预览

2. **打表性能优化**
   - 批量保存时只打表一次
   - 增量打表（只更新修改的表）
   - 打表进度条

3. **编辑器UX优化**
   - 碰撞盒大小调整手柄（Gizmos）
   - 偏移量可视化拖动
   - 预设碰撞盒模板

---

## 📝 备注

- Unity 编译器有时会缓存旧代码，需要重启编辑器或触碰文件强制重新编译
- 自动打表功能默认开启，如需批量修改建议先关闭开关
- 碰撞盒偏移量使用角色本地坐标系，Y轴向上为正

---

## 👤 开发者
Cursor AI Assistant

## 📅 完成时间
2025-10-12











