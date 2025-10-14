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

## 总结
成功开发了ThirdPart资源提取工具，解决了ArtRes仓库体积过大的问题。通过基于YooAsset清单的依赖分析，自动识别并移动实际使用的资源，大大减少了需要版本控制的资源数量。工具设计简洁，使用Unity原生API确保资源引用的完整性和准确性。

