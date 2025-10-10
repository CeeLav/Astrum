# 阶段2 - Bug修复与改进记录

> 📅 **日期**: 2025-10-08  
> 🎯 **版本**: v0.2.1  
> 🔧 **状态**: 已完成

---

## 概述

本次修复了角色编辑器在实际使用中发现的关键问题，包括UI交互失效、CSV数据读取错误、动画切换失效、模型位移等问题。

---

## 修复问题列表

### 1. ✅ UI交互完全失效

**问题描述**：
- 所有按钮无法点击
- 所有文本和数字字段无法编辑
- 控制台报错：`Automatic inspector undo only works when you're inspecting a type derived from UnityEngine.Object`

**根本原因**：
`RoleEditorData` 继承自普通的 `object` 类，导致 Odin Inspector 的 Undo 功能无法正常工作。

**解决方案**：

1. **修改数据模型基类**：
   ```csharp
   // 修改前
   public class RoleEditorData
   
   // 修改后
   public class RoleEditorData : ScriptableObject
   ```

2. **更新创建方法**：
   ```csharp
   public static RoleEditorData CreateDefault(int id)
   {
       var data = CreateInstance<RoleEditorData>();  // 使用ScriptableObject.CreateInstance
       // ... 字段赋值 ...
       return data;
   }
   ```

3. **重写Clone方法**：
   ```csharp
   public RoleEditorData Clone()
   {
       var clone = CreateInstance<RoleEditorData>();
       // 手动复制所有字段（不能用MemberwiseClone）
       clone.EntityId = this.EntityId;
       // ... 复制其他字段 ...
       return clone;
   }
   ```

4. **修改窗口绘制逻辑**：
   ```csharp
   // 从 OdinEditorWindow 改为 EditorWindow，允许自定义布局
   public class RoleEditorWindow : EditorWindow
   
   // 使用 PropertyTree 手动绘制
   _propertyTree = PropertyTree.Create(_selectedRole);
   InspectorUtilities.BeginDrawPropertyTree(_propertyTree, true);
   foreach (var property in _propertyTree.EnumerateTree(false))
   {
       property.Draw();
   }
   InspectorUtilities.EndDrawPropertyTree(_propertyTree);
   
   // 支持Undo
   if (_propertyTree.ApplyChanges())
   {
       _selectedRole.MarkDirty();
       EditorUtility.SetDirty(_selectedRole);
   }
   ```

**影响文件**：
- `RoleEditorData.cs`
- `RoleEditorWindow.cs`

---

### 2. ✅ ActionTable CSV读取失败

**问题描述**：
- 控制台警告：`[ConfigTableHelper] Animation path is empty for actionId 1001`
- 实际CSV文件有数据，但读取结果为空
- 底层错误：`CsvHelper.TypeConversion.TypeConverterException: The conversion cannot be performed`

**根本原因**：
`ActionTableData` 的字段映射与实际CSV列不匹配：
1. 缺少 `duration` 字段映射
2. `TableField` 索引计算错误（未考虑 `HasEmptyFirstColumn = true` 会自动 +1）

**解决方案**：

1. **添加缺失字段**：
   ```csharp
   public class ActionTableData
   {
       [TableField(0, "actionId")]      // 索引从0开始
       public int Id { get; set; }
       
       [TableField(1, "actionName")]
       public string Name { get; set; }
       
       [TableField(2, "actionType")]
       public string ActionType { get; set; }
       
       [TableField(3, "duration")]      // 新增：缺失的字段
       public int Duration { get; set; }
       
       [TableField(4, "AnimationName")]
       public string AnimationPath { get; set; }
   }
   ```

2. **索引映射逻辑**：
   ```
   CSV实际列:    列0(空) | 列1(actionId) | 列2(actionName) | 列3(actionType) | 列4(duration) | 列5(AnimationName)
   TableField:            |    [0]        |     [1]         |     [2]         |     [3]       |      [4]
   自动+1后:              |    1          |     2           |     3           |     4         |      5
   ```

**调试过程**：
- 添加详细日志追踪CSV加载
- 发现转换异常：尝试将 `'idle_01'` 转换为 `int`
- 定位到索引错位和字段缺失

**影响文件**：
- `ConfigTableHelper.cs` (ActionTableData类)

---

### 3. ✅ 预览渲染错误

**问题描述**：
- 控制台错误：`RenderTexture.Create failed: Texture must have height greater than 0`
- 相机裁剪过多，模型位置偏上

**解决方案**：

1. **修复RenderTexture高度**：
   ```csharp
   private void RenderPreview(Rect rect)
   {
       // 确保最小高度
       float previewHeight = Mathf.Max(rect.height - 120, 100f);
       Rect previewRect = new Rect(rect.x, rect.y, rect.width, previewHeight);
       // ...
   }
   ```

2. **优化相机定位**：
   ```csharp
   // 计算模型边界框和中心
   private void CalculateModelBounds()
   {
       var renderers = _previewInstance.GetComponentsInChildren<Renderer>();
       Bounds bounds = renderers[0].bounds;
       foreach (var renderer in renderers)
       {
           bounds.Encapsulate(renderer.bounds);
       }
       
       _modelCenter = bounds.center;
       _orbitRadius = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 1.5f;
       
       // 将模型移到原点
       _previewInstance.transform.position = -_modelCenter;
       _modelCenter = Vector3.zero;
   }
   
   // 球面坐标相机控制
   private void RenderPreview(Rect rect)
   {
       float theta = _dragRotation.x * Mathf.Deg2Rad;
       float phi = _dragRotation.y * Mathf.Deg2Rad;
       float radius = _orbitRadius * _zoomLevel;
       
       Vector3 cameraPos = _modelCenter + new Vector3(
           radius * Mathf.Sin(phi) * Mathf.Cos(theta),
           radius * Mathf.Cos(phi),
           radius * Mathf.Sin(phi) * Mathf.Sin(theta)
       );
       
       _previewRenderUtility.camera.transform.position = cameraPos;
       _previewRenderUtility.camera.transform.LookAt(_modelCenter);
   }
   ```

**影响文件**：
- `RolePreviewModule.cs`

---

### 4. ✅ 动画控制面板位置混乱

**问题描述**：
- 动画控制同时出现在左下（中间面板）和右下（预览面板）
- 需求：动画控制应该在右下预览面板底部

**解决方案**：
1. 从 `RoleEditorWindow.cs` 移除重复的 `DrawAnimationControls()` 方法
2. 保留 `RolePreviewModule.cs` 中的动画控制绘制逻辑
3. 明确职责：预览模块自己处理动画控制UI

**影响文件**：
- `RoleEditorWindow.cs`

---

### 5. ✅ 动画切换失效

**问题描述**：
- 动画下拉菜单选择其他动画后没有切换
- 下拉菜单总是显示第一项

**根本原因**：
`EditorGUILayout.Popup` 的 `selectedIndex` 参数固定传入 `0`，导致无法记住用户选择。

**解决方案**：

1. **添加状态字段**：
   ```csharp
   // RoleEditorWindow.cs
   private int _selectedAnimationIndex = 0;
   ```

2. **修复下拉菜单逻辑**：
   ```csharp
   // 使用状态字段作为当前索引
   int newIndex = EditorGUILayout.Popup(_selectedAnimationIndex, actionNames, ...);
   
   // 检测选择改变，自动播放
   if (newIndex != _selectedAnimationIndex && newIndex >= 0)
   {
       _selectedAnimationIndex = newIndex;
       _previewModule?.PlayAction(actions[_selectedAnimationIndex].ActionId);
   }
   ```

3. **角色切换时重置索引**：
   ```csharp
   private void OnRoleSelected(RoleEditorData role)
   {
       // ...
       _selectedAnimationIndex = 0;  // 重置动画选择
       _previewModule?.SetRole(role);
   }
   ```

**新增功能**：
- ✅ 下拉菜单正确显示当前选中动画
- ✅ 选择不同动画时自动播放（无需点击"播放"按钮）
- ✅ 切换角色时动画选择自动重置

**影响文件**：
- `RoleEditorWindow.cs`

---

### 6. ✅ 动画产生位移（Root Motion）

**问题描述**：
- 播放某些动画（如行走）时，角色会在预览窗口中移动
- 累积多次播放后，角色可能移出视野

**根本原因**：
Animator 的 `applyRootMotion` 默认为 `true`，导致动画中的根运动被应用到模型Transform。

**解决方案**：

1. **关闭Root Motion**：
   ```csharp
   // 在模型加载时
   private void ReloadModel()
   {
       _animancer = AnimationHelper.GetOrAddAnimancer(_previewInstance);
       
       // 关闭Root Motion，防止动画产生位移
       if (_animancer != null && _animancer.Animator != null)
       {
           _animancer.Animator.applyRootMotion = false;
       }
   }
   ```

2. **播放时重置位置**：
   ```csharp
   public void PlayAction(int actionId)
   {
       // 重置模型位置和旋转，防止累积位移
       if (_previewInstance != null)
       {
           _previewInstance.transform.position = -_modelCenter;
           _previewInstance.transform.rotation = Quaternion.identity;
       }
       
       _currentAnimState = AnimationHelper.PlayAnimationByActionId(_animancer, actionId, ...);
   }
   ```

**影响文件**：
- `RolePreviewModule.cs`

---

## 新增功能

### 可调整预览窗口大小

**实现内容**：
- 添加可拖拽的分隔条
- 允许用户自定义预览面板宽度

```csharp
// 添加字段
private float _previewWidth = PREVIEW_WIDTH;
private bool _isResizingPreview = false;

// 绘制分隔条
private void DrawResizeHandle()
{
    float handleX = position.width - _previewWidth - 2.5f;
    Rect handleRect = new Rect(handleX, 0, 5, position.height);
    
    EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);
    
    if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
    {
        _isResizingPreview = true;
        Event.current.Use();
    }
    
    if (_isResizingPreview)
    {
        _previewWidth = position.width - Event.current.mousePosition.x;
        _previewWidth = Mathf.Clamp(_previewWidth, 300f, position.width - 500f);
        Repaint();
    }
    
    if (Event.current.type == EventType.MouseUp)
    {
        _isResizingPreview = false;
    }
}
```

**影响文件**：
- `RoleEditorWindow.cs`

---

## 调试改进

### 添加详细日志

为了更好地追踪问题，添加了多处调试日志：

1. **CSV读取日志**：
   ```csharp
   // LubanCSVReader.cs
   Debug.Log($"{LOG_PREFIX} Successfully loaded {result.Count} records from {config.FilePath}");
   ```

2. **ActionTable加载日志**：
   ```csharp
   // ConfigTableHelper.cs
   Debug.Log($"{LOG_PREFIX} Attempting to load ActionTable from: {config.FilePath}");
   Debug.Log($"{LOG_PREFIX} Loaded {_actionTableCache.Count} action records from CSV");
   
   // 打印前几条记录
   for (int i = 0; i < Mathf.Min(3, _actionTableCache.Count); i++)
   {
       var record = _actionTableCache[i];
       Debug.Log($"{LOG_PREFIX} Record {i}: Id={record.Id}, Name={record.Name}, AnimationPath={record.AnimationPath}");
   }
   ```

3. **动画路径查询日志**：
   ```csharp
   // ConfigTableHelper.cs
   public static string GetAnimationPath(int actionId)
   {
       var actionTable = GetActionTable(actionId);
       if (actionTable == null)
       {
           Debug.LogWarning($"{LOG_PREFIX} ActionTable data not found for actionId {actionId}");
           return string.Empty;
       }
       
       if (string.IsNullOrEmpty(actionTable.AnimationPath))
       {
           Debug.LogWarning($"{LOG_PREFIX} Animation path is empty for actionId {actionId}");
           return string.Empty;
       }
       
       return actionTable.AnimationPath;
   }
   ```

---

## 技术要点总结

### 1. Odin Inspector + ScriptableObject

**核心要点**：
- Odin的Undo功能要求数据类继承自 `UnityEngine.Object`
- `ScriptableObject` 是最适合的选择（轻量、可序列化）
- 使用 `PropertyTree.Create()` + `InspectorUtilities` 手动绘制
- 必须调用 `EditorUtility.SetDirty()` 支持Undo

**最佳实践**：
```csharp
// 数据类
public class RoleEditorData : ScriptableObject { }

// 窗口类
public class RoleEditorWindow : EditorWindow  // 不是 OdinEditorWindow
{
    private PropertyTree _propertyTree;
    
    void OnEnable()
    {
        _propertyTree = PropertyTree.Create(_selectedRole);
    }
    
    void DrawDetailPanel()
    {
        _propertyTree.UpdateTree();
        InspectorUtilities.BeginDrawPropertyTree(_propertyTree, true);
        foreach (var property in _propertyTree.EnumerateTree(false))
        {
            property.Draw();
        }
        InspectorUtilities.EndDrawPropertyTree(_propertyTree);
        
        if (_propertyTree.ApplyChanges())
        {
            EditorUtility.SetDirty(_selectedRole);
        }
    }
}
```

### 2. CsvHelper + Luban表格式

**索引计算规则**：
```
TableField索引 = CSV列索引 - (HasEmptyFirstColumn ? 1 : 0)

示例：
CSV列:        0(空) | 1      | 2      | 3
TableField:         | [0]    | [1]    | [2]
自动+1:             | 1      | 2      | 3
```

**字段映射要点**：
- 必须映射CSV中所有会读取的列
- 缺少字段映射会导致转换异常
- 使用 `NullableConverters` 处理空值

### 3. Animancer编辑器预览

**关键点**：
1. **关闭Root Motion**：
   ```csharp
   animator.applyRootMotion = false;
   ```

2. **手动更新**：
   ```csharp
   AnimationHelper.EvaluateAnimancer(_animancer, deltaTime);
   ```

3. **位置重置**：
   ```csharp
   // 每次播放前重置
   transform.position = initialPosition;
   transform.rotation = Quaternion.identity;
   ```

### 4. EditorGUI状态管理

**问题**：
`EditorGUILayout.Popup` 等控件需要保存状态，否则无法交互。

**解决**：
```csharp
// ❌ 错误：固定值
int index = EditorGUILayout.Popup(0, options);

// ✅ 正确：使用字段保存状态
private int _selectedIndex = 0;
int newIndex = EditorGUILayout.Popup(_selectedIndex, options);
if (newIndex != _selectedIndex)
{
    _selectedIndex = newIndex;
    // 处理改变...
}
```

---

## 测试建议

### 功能测试清单

- [x] UI交互：所有按钮可点击，所有字段可编辑
- [x] 数据加载：ActionTable正确加载，无转换错误
- [x] 动画切换：下拉菜单正确显示，切换动画自动播放
- [x] Root Motion：播放动画时角色不产生位移
- [x] 相机控制：模型居中显示，相机跟随模型中心旋转
- [x] 预览窗口：可拖拽调整大小
- [x] 角色切换：切换角色时动画索引重置
- [x] Undo/Redo：编辑字段后可撤销/重做（Ctrl+Z/Ctrl+Y）

### 回归测试

- [x] CSV读写功能正常
- [x] 保存后数据正确写入文件
- [x] 备份文件正常生成
- [x] 模型预览正常加载
- [x] 动画播放流畅无卡顿

---

## 文件修改清单

| 文件 | 修改类型 | 说明 |
|------|---------|------|
| `RoleEditorData.cs` | 重大修改 | 继承ScriptableObject，重写Clone方法 |
| `RoleEditorWindow.cs` | 重大修改 | 改用EditorWindow，手动Odin绘制，添加窗口调整 |
| `RolePreviewModule.cs` | 功能增强 | 添加Root Motion控制，优化相机定位 |
| `ConfigTableHelper.cs` | Bug修复 | 修正ActionTableData字段映射 |
| `LubanCSVReader.cs` | 日志增强 | 添加成功加载日志 |

---

## 性能优化

本次修复中实施的性能优化：

1. **按需加载ActionTable**：
   - 使用静态缓存，避免重复读取CSV
   - 仅在首次访问时加载

2. **预览渲染优化**：
   - 仅在动画播放时更新
   - 动态调整相机裁剪平面

3. **状态缓存**：
   - 角色列表缓存
   - 动画列表缓存
   - PropertyTree复用

---

## 后续改进建议

### 短期优化

1. **错误处理**：
   - 添加更友好的错误提示UI
   - CSV读取失败时的回退机制

2. **用户体验**：
   - 添加加载进度条
   - 预览窗口背景可自定义
   - 相机预设位置保存

3. **数据验证**：
   - 模型路径有效性检查
   - ActionID引用完整性验证
   - 技能ID存在性检查

### 中长期规划

1. **撤销系统优化**：
   - 自定义Undo记录合并
   - Undo历史记录UI

2. **批量操作**：
   - 多选角色批量编辑
   - 批量替换模型/动画

3. **导入导出**：
   - Excel批量导入
   - 配置模板系统

---

## 总结

本次修复解决了6个关键问题：

1. ✅ UI交互失效（ScriptableObject集成）
2. ✅ CSV读取错误（字段映射修正）
3. ✅ 预览渲染错误（相机优化）
4. ✅ 控制面板位置（职责明确）
5. ✅ 动画切换失效（状态管理）
6. ✅ Root Motion位移（动画配置）

**关键成果**：
- 编辑器完全可用，所有核心功能正常
- 代码质量提升，职责更清晰
- 添加了详细的调试日志
- 优化了用户交互体验

**技术收获**：
- 深入理解Odin Inspector的Undo机制
- 掌握CsvHelper的索引映射规则
- 学习Animancer的编辑器预览技巧
- 积累了EditorGUI状态管理经验

---

**记录完成日期**: 2025-10-08  
**修复版本**: v0.2.1  
**下一步**: 阶段3 - 技能编辑器开发

