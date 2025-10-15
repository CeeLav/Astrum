using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Astrum.Editor.RoleEditor.Persistence.Core
{
    /// <summary>
    /// Luban表格生成器 - 自动打表工具
    /// </summary>
    public static class LubanTableGenerator
    {
        private const string LOG_PREFIX = "[LubanTableGenerator]";
        
        // 打表脚本路径（相对于项目根目录）
        private const string GEN_CLIENT_BAT = "AstrumConfig/Tables/gen_client.bat";
        
        // EditorPref键
        private const string PREF_KEY_AUTO_GENERATE = "Astrum.AutoGenerateTables";
        
        /// <summary>
        /// 是否启用自动打表（默认开启）
        /// </summary>
        public static bool AutoGenerateEnabled
        {
            get => EditorPrefs.GetBool(PREF_KEY_AUTO_GENERATE, true);
            set => EditorPrefs.SetBool(PREF_KEY_AUTO_GENERATE, value);
        }
        
        /// <summary>
        /// 执行客户端打表（检查自动打表开关）
        /// </summary>
        /// <param name="showDialog">是否显示对话框</param>
        /// <param name="forceGenerate">是否强制打表（忽略开关）</param>
        /// <returns>是否成功</returns>
        public static bool GenerateClientTables(bool showDialog = true, bool forceGenerate = false)
        {
            // 如果不是强制打表，检查自动打表开关
            if (!forceGenerate && !AutoGenerateEnabled)
            {
                UnityEngine.Debug.Log($"{LOG_PREFIX} 自动打表已禁用，跳过");
                return true;
            }
            
            try
            {
                string projectRoot = GetProjectRoot();
                string batPath = Path.Combine(projectRoot, GEN_CLIENT_BAT);
                
                if (!File.Exists(batPath))
                {
                    UnityEngine.Debug.LogError($"{LOG_PREFIX} 打表脚本不存在: {batPath}");
                    if (showDialog)
                    {
                        EditorUtility.DisplayDialog("打表失败", $"打表脚本不存在:\n{batPath}", "确定");
                    }
                    return false;
                }
                
                UnityEngine.Debug.Log($"{LOG_PREFIX} 开始执行打表: {batPath}");
                
                // 配置进程启动信息
                var startInfo = new ProcessStartInfo
                {
                    FileName = batPath,
                    WorkingDirectory = Path.GetDirectoryName(batPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };
                
                using (var process = new Process { StartInfo = startInfo })
                {
                    // 捕获输出
                    string output = "";
                    string error = "";
                    
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            output += e.Data + "\n";
                            UnityEngine.Debug.Log($"{LOG_PREFIX} {e.Data}");
                        }
                    };
                    
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            error += e.Data + "\n";
                            UnityEngine.Debug.LogWarning($"{LOG_PREFIX} {e.Data}");
                        }
                    };
                    
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    
                    // 等待完成（最多30秒）
                    bool finished = process.WaitForExit(30000);
                    
                    if (!finished)
                    {
                        process.Kill();
                        UnityEngine.Debug.LogError($"{LOG_PREFIX} 打表超时（30秒）");
                        if (showDialog)
                        {
                            EditorUtility.DisplayDialog("打表失败", "打表超时（30秒）", "确定");
                        }
                        return false;
                    }
                    
                    int exitCode = process.ExitCode;
                    
                    if (exitCode == 0)
                    {
                        UnityEngine.Debug.Log($"{LOG_PREFIX} 打表成功");
                        
                        // 刷新Unity资源
                        AssetDatabase.Refresh();
                        
                        if (showDialog)
                        {
                            EditorUtility.DisplayDialog("打表成功", "表格Bytes已生成并刷新", "确定");
                        }
                        return true;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"{LOG_PREFIX} 打表失败，退出码: {exitCode}\n错误输出:\n{error}");
                        if (showDialog)
                        {
                            EditorUtility.DisplayDialog("打表失败", 
                                $"退出码: {exitCode}\n\n请查看Console了解详情", "确定");
                        }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"{LOG_PREFIX} 打表异常: {ex.Message}\n{ex.StackTrace}");
                if (showDialog)
                {
                    EditorUtility.DisplayDialog("打表异常", ex.Message, "确定");
                }
                return false;
            }
        }
        
        /// <summary>
        /// 获取项目根目录（包含AstrumConfig文件夹的目录）
        /// </summary>
        private static string GetProjectRoot()
        {
            // Unity项目根目录是Assets的父目录
            string assetsPath = Application.dataPath;
            string unityProjectRoot = Directory.GetParent(assetsPath).FullName;
            
            // 实际项目根目录是Unity项目的父目录
            string projectRoot = Directory.GetParent(unityProjectRoot).FullName;
            
            return projectRoot;
        }
        
        /// <summary>
        /// 添加菜单项：手动打表
        /// </summary>
        [MenuItem("Astrum/Tables/生成客户端表格 Bytes", false, 100)]
        public static void GenerateClientTablesMenu()
        {
            GenerateClientTables(showDialog: true, forceGenerate: true);
        }
        
        /// <summary>
        /// 添加菜单项：切换自动打表
        /// </summary>
        [MenuItem("Astrum/Tables/自动打表开关", false, 101)]
        public static void ToggleAutoGenerate()
        {
            AutoGenerateEnabled = !AutoGenerateEnabled;
            string status = AutoGenerateEnabled ? "已开启" : "已关闭";
            UnityEngine.Debug.Log($"{LOG_PREFIX} 自动打表{status}");
            EditorUtility.DisplayDialog("自动打表设置", $"自动打表{status}", "确定");
        }
        
        /// <summary>
        /// 添加菜单项：显示自动打表状态（勾选标记）
        /// </summary>
        [MenuItem("Astrum/Tables/自动打表开关", true)]
        public static bool ToggleAutoGenerateValidate()
        {
            Menu.SetChecked("Astrum/Tables/自动打表开关", AutoGenerateEnabled);
            return true;
        }
    }
}

