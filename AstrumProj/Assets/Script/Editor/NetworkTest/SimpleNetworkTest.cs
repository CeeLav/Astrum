using UnityEngine;
using UnityEditor;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
// Unity内置JSON支持，不需要System.Text.Json

namespace Astrum.Editor
{
    /// <summary>
    /// 简单网络测试 - 避开MemoryPack序列化问题，直接测试TCP连接
    /// </summary>
    public static class SimpleNetworkTest
    {
        /// <summary>
        /// 运行简单网络测试
        /// 可通过命令行调用: Unity.exe -batchmode -quit -executeMethod Astrum.Editor.SimpleNetworkTest.RunSimpleNetworkTest
        /// </summary>
        [MenuItem("Tests/Run Simple Network Test")]
        public static async void RunSimpleNetworkTest()
        {
            Debug.Log("=== 开始简单网络测试 ===");
            Debug.Log($"测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            try
            {
                await TestTcpConnection();
                await TestJsonCommunication();
                
                Debug.Log("=== 简单网络测试完成 - 所有测试通过 ===");
                
                // 在批处理模式下退出
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"=== 简单网络测试失败 ===");
                Debug.LogError($"错误: {ex.Message}");
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
                
                // 在批处理模式下以错误码退出
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
        
        /// <summary>
        /// 测试TCP连接
        /// </summary>
        private static async Task TestTcpConnection()
        {
            Debug.Log("测试 1: TCP连接到服务器");
            
            try
            {
                using var client = new TcpClient();
                
                // 连接到服务器
                Debug.Log("  尝试连接到 127.0.0.1:8888...");
                await client.ConnectAsync("127.0.0.1", 8888);
                Debug.Log("  ✅ TCP连接成功!");
                
                // 连接成功后立即断开
                client.Close();
                Debug.Log("  ✅ 连接已正常关闭");
                
            }
            catch (Exception ex)
            {
                throw new Exception($"TCP连接测试失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试JSON通信
        /// </summary>
        private static async Task TestJsonCommunication()
        {
            Debug.Log("测试 2: JSON消息通信");
            
            try
            {
                using var client = new TcpClient();
                
                // 连接到服务器
                Debug.Log("  连接到服务器...");
                await client.ConnectAsync("127.0.0.1", 8888);
                
                using var stream = client.GetStream();
                
                // 接收欢迎消息
                var buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string welcomeMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log($"  📨 收到欢迎消息: {welcomeMessage.Trim()}");
                
                // 验证是否为有效JSON (简单检查)
                if (welcomeMessage.Trim().StartsWith("{") && welcomeMessage.Trim().EndsWith("}") &&
                    welcomeMessage.Contains("\"type\"") && welcomeMessage.Contains("\"welcome\""))
                {
                    Debug.Log("  ✅ 欢迎消息格式正确");
                }
                else
                {
                    throw new Exception("欢迎消息格式不正确");
                }
                
                // 发送Unity客户端ping消息 (手动构建JSON)
                string messageJson = $@"{{
    ""type"": ""ping"",
    ""message"": ""Unity Editor测试ping"",
    ""client_type"": ""Unity_Editor"",
    ""timestamp"": {DateTimeOffset.Now.ToUnixTimeMilliseconds()},
    ""test_id"": ""{Guid.NewGuid().ToString("N").Substring(0, 8)}""
}}" + "\n";
                byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);
                
                Debug.Log($"  📤 发送ping消息: {messageJson.Trim()}");
                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                
                // 接收pong响应
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log($"  📨 收到响应: {response.Trim()}");
                
                // 验证响应格式 (简单检查)
                if (response.Trim().StartsWith("{") && response.Trim().EndsWith("}") &&
                    response.Contains("\"type\"") && response.Contains("\"pong\""))
                {
                    Debug.Log("  ✅ Pong响应格式正确");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️  响应格式不是预期的pong格式: {response.Trim()}");
                }
                
                // 发送文本消息测试
                string textMessage = "Unity Editor测试文本消息\\n";
                byte[] textBytes = Encoding.UTF8.GetBytes(textMessage);
                
                Debug.Log($"  📤 发送文本消息: {textMessage.Trim()}");
                await stream.WriteAsync(textBytes, 0, textBytes.Length);
                
                // 等待一下确保消息发送完成
                await Task.Delay(100);
                
                Debug.Log("  ✅ JSON通信测试完成");
                
            }
            catch (Exception ex)
            {
                throw new Exception($"JSON通信测试失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 运行连接性能测试
        /// </summary>
        [MenuItem("Tests/Run Connection Performance Test")]
        public static async void RunPerformanceTest()
        {
            Debug.Log("=== 开始连接性能测试 ===");
            
            const int connectionCount = 10;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                for (int i = 0; i < connectionCount; i++)
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync("127.0.0.1", 8888);
                    Debug.Log($"  连接 {i + 1}/{connectionCount} 成功");
                    client.Close();
                }
                
                stopwatch.Stop();
                Debug.Log($"=== 性能测试完成 ===");
                Debug.Log($"总耗时: {stopwatch.ElapsedMilliseconds} ms");
                Debug.Log($"平均每次连接: {stopwatch.ElapsedMilliseconds / (float)connectionCount:F2} ms");
                
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"性能测试失败: {ex.Message}");
                
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}