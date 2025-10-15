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
        
        private Vector2 _scrollPosition;
        private bool _isAnalyzed = false;
        
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
                "移动到GameAssets目录。Unity会自动更新所有引用。", 
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 一键移动按钮
            EditorGUILayout.LabelField("一键移动ThirdPart资源", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "此操作会：\n" +
                "1. 分析依赖关系（排除场景文件）\n" +
                "2. 移动资源到GameAssets（GUID不变，引用自动更新）\n\n" +
                "⚠️ 移动后ThirdPart中这些资源将被删除！建议先备份项目！",
                MessageType.Warning);
            
            if (GUILayout.Button("一键分析并移动资源", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("确认移动", 
                    "此操作会：\n" +
                    "1. 分析ThirdPart依赖\n" +
                    "2. 移动资源到GameAssets（保留目录结构）\n" +
                    "3. Unity会自动更新所有引用\n\n" +
                    "⚠️ ThirdPart中对应资源会被删除！\n" +
                    "建议先备份项目！确定继续？", 
                    "确定", "取消"))
                {
                    ExecuteFullProcess();
                }
            }
            
            EditorGUILayout.Space(20);
            
            // 分析结果
            if (_isAnalyzed)
            {
                EditorGUILayout.LabelField("分析结果", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"找到 {_sourceAssets.Count} 个源头资源（已排除场景）");
                EditorGUILayout.LabelField($"找到 {_thirdPartDependencies.Count} 个ThirdPart依赖资源");
                
                // 显示依赖资源列表
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("将要移动的资源列表:", EditorStyles.boldLabel);
                
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
                
                // 步骤2: 移动资源
                if (!MoveAssets())
                {
                    return;
                }
                
                EditorUtility.DisplayDialog("完成", 
                    $"ThirdPart资源移动完成！\n" +
                    $"移动了 {_thirdPartDependencies.Count} 个资源到 {TARGET_PATH}\n" +
                    $"Unity已自动更新所有引用。", 
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
        /// 移动资源到GameAssets目录
        /// </summary>
        private bool MoveAssets()
        {
            try
            {
                // 确保目标根目录存在
                EnsureDirectoryExists(TARGET_PATH);
                
                int movedCount = 0;
                
                for (int i = 0; i < _thirdPartDependencies.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("移动资源", 
                        $"移动资源 {i + 1}/{_thirdPartDependencies.Count}", 
                        (float)i / _thirdPartDependencies.Count);
                    
                    string oldPath = _thirdPartDependencies[i];
                    
                    // 计算新路径（保持相对结构）
                    string relativePath = oldPath.Substring(THIRD_PART_PATH.Length + 1);
                    string newPath = Path.Combine(TARGET_PATH, relativePath).Replace("\\", "/");
                    
                    // 确保目标子目录存在（使用AssetDatabase创建）
                    string newDir = Path.GetDirectoryName(newPath).Replace("\\", "/");
                    if (!string.IsNullOrEmpty(newDir))
                    {
                        EnsureDirectoryExists(newDir);
                    }
                    
                    // 移动资源（GUID保持不变，Unity会自动更新引用）
                    string error = AssetDatabase.MoveAsset(oldPath, newPath);
                    
                    if (string.IsNullOrEmpty(error))
                    {
                        movedCount++;
                        Debug.Log($"[ThirdPartExtractor] 移动: {oldPath} -> {newPath}");
                    }
                    else
                    {
                        Debug.LogError($"[ThirdPartExtractor] 移动失败: {oldPath} -> {newPath}, 错误: {error}");
                    }
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"[ThirdPartExtractor] 成功移动 {movedCount}/{_thirdPartDependencies.Count} 个资源");
                return true;
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[ThirdPartExtractor] 移动失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"移动失败：{ex.Message}", "确定");
                return false;
            }
        }
        
        /// <summary>
        /// 确保目录存在（递归创建）
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            // 已经存在则返回
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            
            // 获取父目录路径
            string parentPath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parentPath))
            {
                return;
            }
            
            parentPath = parentPath.Replace("\\", "/");
            
            // 递归确保父目录存在（除了Assets根目录）
            if (parentPath != "Assets" && !AssetDatabase.IsValidFolder(parentPath))
            {
                EnsureDirectoryExists(parentPath);
            }
            
            // 创建当前目录
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(folderName))
            {
                string result = AssetDatabase.CreateFolder(parentPath, folderName);
                if (string.IsNullOrEmpty(result))
                {
                    Debug.LogError($"[ThirdPartExtractor] 创建目录失败: {parentPath}/{folderName}");
                }
                else
                {
                    Debug.Log($"[ThirdPartExtractor] 创建目录: {path}");
                }
            }
        }
        
        /// <summary>
        /// 重置状态
        /// </summary>
        private void ResetState()
        {
            _sourceAssets.Clear();
            _thirdPartDependencies.Clear();
            _isAnalyzed = false;
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

