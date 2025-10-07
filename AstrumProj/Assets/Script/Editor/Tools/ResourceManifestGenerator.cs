using System;
using System.IO;
using UnityEditor;
using UnityEngine;

#if YOO_ASSET_2
using YooAsset;
using YooAsset.Editor;
#endif

namespace Astrum.Editor
{
    /// <summary>
    /// 资源清单生成器 - 用于手动生成YooAsset资源清单，避免运行时卡顿
    /// </summary>
    public class ResourceManifestGenerator
    {
        private const string DEFAULT_PACKAGE_NAME = "DefaultPackage";
        
        [MenuItem("Astrum/资源管理/生成资源清单")]
        public static void GenerateResourceManifest()
        {
#if YOO_ASSET_2
            try
            {
                EditorUtility.DisplayProgressBar("生成资源清单", "正在生成资源清单，请稍候...", 0f);
                
                // 显示开始时间
                DateTime startTime = DateTime.Now;
                Debug.Log($"[ResourceManifestGenerator] 开始生成资源清单 - {startTime:HH:mm:ss}");
                
                // 调用EditorSimulateModeHelper.SimulateBuild生成资源清单
                var buildResult = EditorSimulateModeHelper.SimulateBuild(DEFAULT_PACKAGE_NAME);
                
                // 显示完成时间
                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;
                
                EditorUtility.ClearProgressBar();
                
                if (buildResult != null)
                {
                    string packageRoot = buildResult.PackageRootDirectory;
                    Debug.Log($"[ResourceManifestGenerator] 资源清单生成成功！");
                    Debug.Log($"[ResourceManifestGenerator] 包根目录: {packageRoot}");
                    Debug.Log($"[ResourceManifestGenerator] 生成耗时: {duration.TotalSeconds:F2} 秒");
                    
                    // 检查生成的文件
                    CheckGeneratedFiles(packageRoot);
                    
                    EditorUtility.DisplayDialog("成功", 
                        $"资源清单生成成功！\n" +
                        $"耗时: {duration.TotalSeconds:F2} 秒\n" +
                        $"目录: {packageRoot}", 
                        "确定");
                        
                    // 刷新资源窗口
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogError("[ResourceManifestGenerator] 资源清单生成失败！");
                    EditorUtility.DisplayDialog("错误", "资源清单生成失败！", "确定");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[ResourceManifestGenerator] 生成资源清单时发生异常: {ex.Message}");
                Debug.LogError($"[ResourceManifestGenerator] 异常堆栈: {ex.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"生成资源清单时发生异常:\n{ex.Message}", "确定");
            }
#else
            EditorUtility.DisplayDialog("错误", "当前项目未启用YooAsset，无法生成资源清单", "确定");
#endif
        }
        
        [MenuItem("Astrum/资源管理/清理资源清单")]
        public static void CleanResourceManifest()
        {
#if YOO_ASSET_2
            try
            {
                // 获取当前平台的Bundles目录
                string bundlesPath = GetBundlesPath();
                
                if (string.IsNullOrEmpty(bundlesPath))
                {
                    EditorUtility.DisplayDialog("错误", "无法找到Bundles目录", "确定");
                    return;
                }
                
                // 查找Simulate目录
                string simulatePath = Path.Combine(bundlesPath, DEFAULT_PACKAGE_NAME, "Simulate");
                
                if (Directory.Exists(simulatePath))
                {
                    bool confirm = EditorUtility.DisplayDialog("确认删除", 
                        $"确定要删除以下目录及其所有内容吗？\n{simulatePath}", 
                        "删除", "取消");
                        
                    if (confirm)
                    {
                        Directory.Delete(simulatePath, true);
                        AssetDatabase.Refresh();
                        
                        Debug.Log($"[ResourceManifestGenerator] 已清理资源清单目录: {simulatePath}");
                        EditorUtility.DisplayDialog("成功", "资源清单已清理", "确定");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "未找到资源清单目录，无需清理", "确定");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManifestGenerator] 清理资源清单时发生异常: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"清理资源清单时发生异常:\n{ex.Message}", "确定");
            }
#else
            EditorUtility.DisplayDialog("错误", "当前项目未启用YooAsset，无法清理资源清单", "确定");
#endif
        }
        
        [MenuItem("Astrum/资源管理/打开资源清单目录")]
        public static void OpenResourceManifestDirectory()
        {
#if YOO_ASSET_2
            try
            {
                // 获取当前平台的Bundles目录
                string bundlesPath = GetBundlesPath();
                
                if (string.IsNullOrEmpty(bundlesPath))
                {
                    EditorUtility.DisplayDialog("错误", "无法找到Bundles目录", "确定");
                    return;
                }
                
                // 查找Simulate目录
                string simulatePath = Path.Combine(bundlesPath, DEFAULT_PACKAGE_NAME, "Simulate");
                
                if (Directory.Exists(simulatePath))
                {
                    // 在文件管理器中打开目录
                    EditorUtility.RevealInFinder(simulatePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "资源清单目录不存在，请先生成资源清单", "确定");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManifestGenerator] 打开资源清单目录时发生异常: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"打开资源清单目录时发生异常:\n{ex.Message}", "确定");
            }
#else
            EditorUtility.DisplayDialog("错误", "当前项目未启用YooAsset", "确定");
#endif
        }
        
#if YOO_ASSET_2
        /// <summary>
        /// 获取当前平台的Bundles目录路径
        /// </summary>
        /// <returns>Bundles目录路径</returns>
        private static string GetBundlesPath()
        {
            string projectPath = Application.dataPath;
            string projectRoot = Path.GetDirectoryName(projectPath);
            
            // 根据当前平台确定目录名
            string platformName = GetPlatformName();
            string bundlesPath = Path.Combine(projectRoot, "Bundles", platformName);
            
            return bundlesPath;
        }
        
        /// <summary>
        /// 获取当前平台的目录名称
        /// </summary>
        /// <returns>平台目录名称</returns>
        private static string GetPlatformName()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "StandaloneWindows64";
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.StandaloneOSX:
                    return "StandaloneOSX";
                case BuildTarget.WebGL:
                    return "WebGL";
                default:
                    return "StandaloneWindows64"; // 默认使用Windows64
            }
        }
        
        /// <summary>
        /// 检查生成的文件
        /// </summary>
        /// <param name="packageRoot">包根目录</param>
        private static void CheckGeneratedFiles(string packageRoot)
        {
            try
            {
                if (Directory.Exists(packageRoot))
                {
                    var files = Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories);
                    Debug.Log($"[ResourceManifestGenerator] 生成的文件数量: {files.Length}");
                    
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        Debug.Log($"[ResourceManifestGenerator] 文件: {Path.GetRelativePath(packageRoot, file)} ({fileInfo.Length} bytes)");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ResourceManifestGenerator] 包根目录不存在: {packageRoot}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManifestGenerator] 检查生成文件时发生异常: {ex.Message}");
            }
        }
#endif
    }
}


