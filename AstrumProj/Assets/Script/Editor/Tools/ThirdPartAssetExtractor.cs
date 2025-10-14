using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor
{
    /// <summary>
    /// ThirdPart资源提取工具 - 从ThirdPart中提取实际使用的资源到GameAssets目录
    /// </summary>
    public class ThirdPartAssetExtractor : EditorWindow
    {
        private const string MANIFEST_PATH = "Bundles/StandaloneWindows64/DefaultPackage/Simulate/DefaultPackage_Simulate.json";
        private const string THIRD_PART_PATH = "Assets/ArtRes/ThirdPart";
        private const string TARGET_PATH = "Assets/ArtRes/GameAssets";
        
        private List<string> _sourceAssets = new List<string>();
        private List<string> _thirdPartDependencies = new List<string>();
        private Dictionary<string, string> _guidMapping = new Dictionary<string, string>(); // 旧GUID -> 新GUID
        private Dictionary<string, string> _pathMapping = new Dictionary<string, string>(); // 旧路径 -> 新路径
        
        private Vector2 _scrollPosition;
        private bool _isAnalyzed = false;
        private bool _isCopied = false;
        
        [MenuItem("Astrum/Asset 资源管理/Extract ThirdPart Assets 提取ThirdPart资源", false, 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<ThirdPartAssetExtractor>("ThirdPart资源提取工具");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("ThirdPart资源提取工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "此工具会分析YooAsset清单，找出ThirdPart中实际被使用的资源（排除场景），\n" +
                "复制到GameAssets目录并自动更新所有引用。", 
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 一键提取按钮
            EditorGUILayout.LabelField("一键提取ThirdPart资源", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "此操作会：\n" +
                "1. 分析依赖关系（排除场景文件）\n" +
                "2. 复制资源到GameAssets\n" +
                "3. 更新所有引用\n\n" +
                "⚠️ 建议先备份项目或提交git！",
                MessageType.Warning);
            
            if (GUILayout.Button("一键提取并更新引用", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("确认执行", 
                    "此操作会：\n" +
                    "1. 分析并复制ThirdPart资源到GameAssets\n" +
                    "2. 更新所有引用（排除场景文件）\n\n" +
                    "建议先备份项目！确定继续？", 
                    "确定", "取消"))
                {
                    ExecuteFullProcess();
                }
            }
            
            EditorGUILayout.Space(20);
            
            // 分步操作（折叠）
            if (_isAnalyzed)
            {
                EditorGUILayout.LabelField("分析结果", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"找到 {_sourceAssets.Count} 个源头资源");
                EditorGUILayout.LabelField($"找到 {_thirdPartDependencies.Count} 个ThirdPart依赖资源");
                
                // 显示依赖资源列表
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("ThirdPart依赖资源列表:", EditorStyles.boldLabel);
                
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
                foreach (var asset in _thirdPartDependencies)
                {
                    EditorGUILayout.LabelField($"  {asset}");
                }
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.Space(10);
            
            // 重置按钮
            if (GUILayout.Button("重置", GUILayout.Height(25)))
            {
                ResetState();
            }
        }
        
        /// <summary>
        /// 执行完整流程
        /// </summary>
        private void ExecuteFullProcess()
        {
            try
            {
                // 步骤1: 分析依赖
                if (!AnalyzeDependencies())
                {
                    return;
                }
                
                // 步骤2: 复制资源
                if (!CopyAssets())
                {
                    return;
                }
                
                // 步骤3: 更新引用
                UpdateReferences();
                
                EditorUtility.DisplayDialog("完成", 
                    $"ThirdPart资源提取完成！\n" +
                    $"提取了 {_thirdPartDependencies.Count} 个资源到 {TARGET_PATH}\n" +
                    $"已更新所有引用。", 
                    "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[ThirdPartExtractor] 执行失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"执行失败：{ex.Message}", "确定");
            }
        }
        
        /// <summary>
        /// 分析依赖关系
        /// </summary>
        private bool AnalyzeDependencies()
        {
            try
            {
                EditorUtility.DisplayProgressBar("分析依赖", "正在读取资源清单...", 0f);
                
                // 1. 读取JSON清单
                string jsonPath = Path.Combine(Application.dataPath, "..", MANIFEST_PATH);
                if (!File.Exists(jsonPath))
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("错误", $"找不到资源清单文件：{jsonPath}\n请先生成资源清单！", "确定");
                    return false;
                }
                
                string jsonContent = File.ReadAllText(jsonPath);
                var manifest = JsonUtility.FromJson<ManifestData>(jsonContent);
                
                if (manifest == null || manifest.AssetList == null)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("错误", "无法解析资源清单！", "确定");
                    return false;
                }
                
                // 2. 提取源头资源路径（排除场景文件）
                _sourceAssets.Clear();
                foreach (var asset in manifest.AssetList)
                {
                    if (!string.IsNullOrEmpty(asset.AssetPath) && !asset.AssetPath.EndsWith(".unity"))
                    {
                        _sourceAssets.Add(asset.AssetPath);
                    }
                }
                
                Debug.Log($"[ThirdPartExtractor] 找到 {_sourceAssets.Count} 个源头资源（已排除场景）");
                
                // 3. 分析所有依赖
                EditorUtility.DisplayProgressBar("分析依赖", "正在分析依赖关系...", 0.3f);
                
                HashSet<string> allDependencies = new HashSet<string>();
                for (int i = 0; i < _sourceAssets.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("分析依赖", 
                        $"分析资源 {i + 1}/{_sourceAssets.Count}", 
                        0.3f + (0.6f * i / _sourceAssets.Count));
                    
                    string[] dependencies = AssetDatabase.GetDependencies(_sourceAssets[i], true);
                    foreach (var dep in dependencies)
                    {
                        allDependencies.Add(dep);
                    }
                }
                
                Debug.Log($"[ThirdPartExtractor] 找到 {allDependencies.Count} 个依赖资源");
                
                // 4. 过滤ThirdPart资源
                EditorUtility.DisplayProgressBar("分析依赖", "过滤ThirdPart资源...", 0.9f);
                
                _thirdPartDependencies.Clear();
                foreach (var dep in allDependencies)
                {
                    if (dep.StartsWith(THIRD_PART_PATH, StringComparison.OrdinalIgnoreCase))
                    {
                        _thirdPartDependencies.Add(dep);
                    }
                }
                
                _thirdPartDependencies.Sort();
                
                Debug.Log($"[ThirdPartExtractor] 找到 {_thirdPartDependencies.Count} 个ThirdPart依赖资源");
                
                _isAnalyzed = true;
                _isCopied = false;
                
                EditorUtility.ClearProgressBar();
                return true;
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[ThirdPartExtractor] 分析失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"分析失败：{ex.Message}", "确定");
                return false;
            }
        }
        
        /// <summary>
        /// 复制资源到GameAssets目录
        /// </summary>
        private bool CopyAssets()
        {
            try
            {
                // 确保目标目录存在
                if (!AssetDatabase.IsValidFolder(TARGET_PATH))
                {
                    string[] pathParts = TARGET_PATH.Split('/');
                    string currentPath = pathParts[0];
                    for (int i = 1; i < pathParts.Length; i++)
                    {
                        string nextPath = currentPath + "/" + pathParts[i];
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                        }
                        currentPath = nextPath;
                    }
                }
                
                _guidMapping.Clear();
                _pathMapping.Clear();
                
                for (int i = 0; i < _thirdPartDependencies.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("复制资源", 
                        $"复制资源 {i + 1}/{_thirdPartDependencies.Count}", 
                        (float)i / _thirdPartDependencies.Count);
                    
                    string oldPath = _thirdPartDependencies[i];
                    
                    // 计算新路径（保持相对结构）
                    string relativePath = oldPath.Substring(THIRD_PART_PATH.Length + 1);
                    string newPath = Path.Combine(TARGET_PATH, relativePath).Replace("\\", "/");
                    
                    // 确保目标子目录存在
                    string newDir = Path.GetDirectoryName(newPath);
                    if (!string.IsNullOrEmpty(newDir) && !AssetDatabase.IsValidFolder(newDir))
                    {
                        Directory.CreateDirectory(newDir);
                    }
                    
                    // 获取旧GUID
                    string oldGuid = AssetDatabase.AssetPathToGUID(oldPath);
                    
                    // 复制资源
                    AssetDatabase.CopyAsset(oldPath, newPath);
                    
                    // 获取新GUID
                    string newGuid = AssetDatabase.AssetPathToGUID(newPath);
                    
                    // 记录映射
                    _guidMapping[oldGuid] = newGuid;
                    _pathMapping[oldPath] = newPath;
                    
                    Debug.Log($"[ThirdPartExtractor] 复制: {oldPath} -> {newPath} (GUID: {oldGuid} -> {newGuid})");
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                _isCopied = true;
                
                EditorUtility.ClearProgressBar();
                return true;
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[ThirdPartExtractor] 复制失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"复制失败：{ex.Message}", "确定");
                return false;
            }
        }
        
        /// <summary>
        /// 更新所有引用（排除场景文件）
        /// </summary>
        private void UpdateReferences()
        {
            try
            {
                // 获取所有需要更新引用的文件类型（排除.unity场景文件）
                string[] assetPaths = AssetDatabase.GetAllAssetPaths()
                    .Where(p => p.StartsWith("Assets/") && 
                               (p.EndsWith(".prefab") || 
                                p.EndsWith(".mat") || 
                                p.EndsWith(".asset") ||
                                p.EndsWith(".controller")))
                    .ToArray();
                
                int updatedCount = 0;
                
                for (int i = 0; i < assetPaths.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("更新引用", 
                        $"更新文件 {i + 1}/{assetPaths.Length}", 
                        (float)i / assetPaths.Length);
                    
                    string filePath = assetPaths[i];
                    
                    // 读取文件内容
                    string content = File.ReadAllText(filePath);
                    string originalContent = content;
                    
                    // 替换所有GUID引用
                    foreach (var mapping in _guidMapping)
                    {
                        string oldGuid = mapping.Key;
                        string newGuid = mapping.Value;
                        
                        // 替换格式: "guid: 旧GUID" -> "guid: 新GUID"
                        content = content.Replace($"guid: {oldGuid}", $"guid: {newGuid}");
                    }
                    
                    // 如果有修改，写回文件
                    if (content != originalContent)
                    {
                        File.WriteAllText(filePath, content);
                        updatedCount++;
                        Debug.Log($"[ThirdPartExtractor] 更新引用: {filePath}");
                    }
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"[ThirdPartExtractor] 引用更新完成！更新了 {updatedCount} 个文件（已排除场景文件）");
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[ThirdPartExtractor] 更新引用失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"更新引用失败：{ex.Message}", "确定");
            }
        }
        
        /// <summary>
        /// 重置状态
        /// </summary>
        private void ResetState()
        {
            _sourceAssets.Clear();
            _thirdPartDependencies.Clear();
            _guidMapping.Clear();
            _pathMapping.Clear();
            _isAnalyzed = false;
            _isCopied = false;
            _scrollPosition = Vector2.zero;
        }
        
        #region JSON数据结构
        
        [Serializable]
        private class ManifestData
        {
            public List<AssetInfo> AssetList;
        }
        
        [Serializable]
        private class AssetInfo
        {
            public string AssetPath;
        }
        
        #endregion
    }
}

