using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor
{
    /// <summary>
    /// 服务器控制窗口 - 用于启动服务器和查看日志
    /// </summary>
    public class ServerWindow : EditorWindow
    {
        private Process serverProcess;
        private bool isServerRunning = false;
        private string serverLogs = "";
        private Vector2 scrollPosition;
        private string serverPath = "";
        private string serverArguments = "";
        private bool autoScroll = true;
        private Thread logReaderThread;
        private bool shouldStopLogReader = false;
        private readonly object logLock = new object();



        [MenuItem("Astrum/Tools 工具/Server Control 服务器控制", false, 51)]
        public static void ShowWindow()
        {
            var window = GetWindow<ServerWindow>("服务器控制");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 设置默认服务器路径
            if (string.IsNullOrEmpty(serverPath))
            {
                serverPath = Path.Combine(Application.dataPath, "../../start_server.bat");
            }
            
            // 设置默认参数
            if (string.IsNullOrEmpty(serverArguments))
            {
                serverArguments = "";
            }
            
            // 检查服务器是否已经在运行
            CheckServerStatus();
        }

        private void OnDisable()
        {
            StopLogReader();
        }

        private void OnDestroy()
        {
            StopLogReader();
        }

        private void OnGUI()
        {
            GUILayout.Label("Astrum 服务器控制", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 服务器配置区域
            DrawServerConfiguration();
            EditorGUILayout.Space();

            // 服务器控制按钮
            DrawServerControls();
            EditorGUILayout.Space();

            // 服务器状态
            DrawServerStatus();
            EditorGUILayout.Space();

            // 日志显示区域
            DrawLogDisplay();
        }

        private void DrawServerConfiguration()
        {
            GUILayout.Label("服务器配置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("服务器路径:", GUILayout.Width(80));
            serverPath = EditorGUILayout.TextField(serverPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("选择服务器可执行文件", "", "exe");
                if (!string.IsNullOrEmpty(path))
                {
                    serverPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("启动参数:", GUILayout.Width(80));
            serverArguments = EditorGUILayout.TextField(serverArguments);
            EditorGUILayout.EndHorizontal();

            // 检查服务器文件是否存在
            if (!string.IsNullOrEmpty(serverPath) && !File.Exists(serverPath))
            {
                EditorGUILayout.HelpBox("服务器文件不存在，请检查路径", MessageType.Error);
            }
        }

        private void DrawServerControls()
        {
            GUILayout.Label("服务器控制", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (!isServerRunning)
            {
                GUI.enabled = File.Exists(serverPath);
                if (GUILayout.Button("启动服务器", GUILayout.Height(30)))
                {
                    StartServer();
                }
                GUI.enabled = true;
            }
            else
            {
                if (GUILayout.Button("停止服务器", GUILayout.Height(30)))
                {
                    StopServer();
                }
            }
            
            if (GUILayout.Button("刷新状态", GUILayout.Height(30)))
            {
                CheckServerStatus();
            }
            
            if (GUILayout.Button("清空日志", GUILayout.Height(30)))
            {
                ClearLogs();
            }
            
            if (GUILayout.Button("开始监控日志", GUILayout.Height(30)))
            {
                StartLogReader();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawServerStatus()
        {
            GUILayout.Label("服务器状态", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("状态:", GUILayout.Width(50));
            
            if (isServerRunning)
            {
                EditorGUILayout.LabelField("运行中", EditorStyles.boldLabel);
                GUI.color = Color.green;
                GUILayout.Label("●", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("已停止", EditorStyles.boldLabel);
                GUI.color = Color.red;
                GUILayout.Label("●", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (isServerRunning && serverProcess != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("进程ID:", GUILayout.Width(50));
                EditorGUILayout.LabelField(serverProcess.Id.ToString());
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawLogDisplay()
        {
            GUILayout.Label("服务器日志", EditorStyles.boldLabel);
            
            // 自动滚动选项
            autoScroll = EditorGUILayout.Toggle("自动滚动", autoScroll);
            
            // 日志显示区域
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            lock (logLock)
            {
                EditorGUILayout.TextArea(serverLogs, GUILayout.ExpandHeight(true));
            }
            
            EditorGUILayout.EndScrollView();
            
            // 滚动到底部
            if (autoScroll && Event.current.type == EventType.Layout)
            {
                scrollPosition.y = float.MaxValue;
            }
        }

        private void StartServer()
        {
            if (isServerRunning)
            {
                EditorUtility.DisplayDialog("提示", "服务器已在运行中", "确定");
                return;
            }

            if (string.IsNullOrEmpty(serverPath) || !File.Exists(serverPath))
            {
                EditorUtility.DisplayDialog("错误", "服务器文件不存在，请检查路径", "确定");
                return;
            }

            try
            {
                // 创建进程启动信息
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{serverPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false,
                    WorkingDirectory = Path.GetDirectoryName(serverPath)
                };

                // 启动服务器进程
                serverProcess = new Process { StartInfo = startInfo };
                serverProcess.OutputDataReceived += OnServerOutput;
                serverProcess.ErrorDataReceived += OnServerError;
                serverProcess.Exited += OnServerExited;

                serverProcess.Start();
                serverProcess.BeginOutputReadLine();
                serverProcess.BeginErrorReadLine();

                isServerRunning = true;
                AddLog("服务器启动成功");
                
                // 开始日志读取线程
                StartLogReader();
                
                EditorUtility.DisplayDialog("成功", "服务器启动成功", "确定");
            }
            catch (Exception ex)
            {
                AddLog($"启动服务器失败: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"启动服务器失败: {ex.Message}", "确定");
            }
        }

        private void StopServer()
        {
            if (!isServerRunning || serverProcess == null)
            {
                return;
            }

            try
            {
                AddLog("正在停止服务器...");
                
                // 停止日志读取
                StopLogReader();
                
                // 终止进程
                if (!serverProcess.HasExited)
                {
                    serverProcess.Kill();
                    serverProcess.WaitForExit(5000); // 等待5秒
                }
                
                serverProcess.Dispose();
                serverProcess = null;
                isServerRunning = false;
                
                AddLog("服务器已停止");
                EditorUtility.DisplayDialog("成功", "服务器已停止", "确定");
            }
            catch (Exception ex)
            {
                AddLog($"停止服务器失败: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"停止服务器失败: {ex.Message}", "确定");
            }
        }

        private void CheckServerStatus()
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
                isServerRunning = true;
            }
            else
            {
                isServerRunning = false;
                serverProcess = null;
            }
            
            Repaint();
        }

        private void ClearLogs()
        {
            lock (logLock)
            {
                serverLogs = "";
            }
            Repaint();
        }

        private void OnServerOutput(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AddLog($"[OUT] {e.Data}");
            }
        }

        private void OnServerError(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AddLog($"[ERR] {e.Data}");
            }
        }

        private void OnServerExited(object sender, EventArgs e)
        {
            isServerRunning = false;
            AddLog("服务器进程已退出");
            StopLogReader();
            
            // 在主线程中更新UI
            EditorApplication.delayCall += () =>
            {
                Repaint();
            };
        }

        private void AddLog(string message)
        {
            lock (logLock)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                serverLogs += $"[{timestamp}] {message}\n";
                
                // 限制日志长度，避免内存占用过大
                if (serverLogs.Length > 10000)
                {
                    int firstNewLine = serverLogs.IndexOf('\n');
                    if (firstNewLine > 0)
                    {
                        serverLogs = serverLogs.Substring(firstNewLine + 1);
                    }
                }
            }
            
            // 在主线程中更新UI
            EditorApplication.delayCall += () =>
            {
                Repaint();
            };
        }

        private void StartLogReader()
        {
            if (logReaderThread != null && logReaderThread.IsAlive)
            {
                return;
            }

            shouldStopLogReader = false;
            logReaderThread = new Thread(LogReaderWorker);
            logReaderThread.IsBackground = true;
            logReaderThread.Start();
        }

        private void StopLogReader()
        {
            shouldStopLogReader = true;
            
            if (logReaderThread != null && logReaderThread.IsAlive)
            {
                logReaderThread.Join(1000); // 等待1秒
                logReaderThread = null;
            }
        }

        private void LogReaderWorker()
        {
            try
            {
                // 尝试读取服务器日志文件
                string logFilePath = Path.Combine(Application.dataPath, "../../AstrumServer/AstrumServer/server.log");
                
                if (File.Exists(logFilePath))
                {
                    var fileInfo = new FileInfo(logFilePath);
                    long lastPosition = fileInfo.Length;
                    
                    while (!shouldStopLogReader)
                    {
                        try
                        {
                            if (File.Exists(logFilePath))
                            {
                                fileInfo.Refresh();
                                
                                if (fileInfo.Length > lastPosition)
                                {
                                    // 有新内容，读取新行
                                    using (var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                    using (var streamReader = new StreamReader(fileStream))
                                    {
                                        fileStream.Seek(lastPosition, SeekOrigin.Begin);
                                        
                                        string line;
                                        while ((line = streamReader.ReadLine()) != null)
                                        {
                                            if (!string.IsNullOrEmpty(line))
                                            {
                                                AddLog($"[LOG] {line}");
                                            }
                                        }
                                        
                                        lastPosition = fileStream.Position;
                                    }
                                }
                                else if (fileInfo.Length < lastPosition)
                                {
                                    // 文件被重置或清空
                                    lastPosition = 0;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            AddLog($"日志读取错误: {ex.Message}");
                        }
                        
                        Thread.Sleep(500); // 每500ms检查一次
                    }
                }
                else
                {
                    AddLog("日志文件不存在，等待创建...");
                    while (!shouldStopLogReader && !File.Exists(logFilePath))
                    {
                        Thread.Sleep(1000); // 每秒检查一次文件是否存在
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"日志读取器错误: {ex.Message}");
            }
        }
    }
}
