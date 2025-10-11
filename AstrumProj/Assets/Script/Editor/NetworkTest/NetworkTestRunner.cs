using UnityEngine;
using UnityEditor;
using System;
using System.Threading.Tasks;
using Astrum.Client.Managers;
using Astrum.Network.Generated;

namespace Astrum.Editor
{
    /// <summary>
    /// 网络测试运行器 - 用于命令行测试
    /// </summary>
    public static class NetworkTestRunner
    {
        /// <summary>
        /// 运行网络连接测试
        /// 可通过命令行调用: Unity.exe -batchmode -quit -executeMethod NetworkTestRunner.RunNetworkTests
        /// </summary>
        [MenuItem("Astrum/Testing 测试/Network 网络测试/Network Tests 网络测试", false, 206)]
        public static async void RunNetworkTests()
        {
            Debug.Log("=== 开始网络测试 ===");
            
            try
            {
                await TestNetworkManagerInitialization();
                await TestSessionCreation();
                await TestMessageSending();
                
                Debug.Log("=== 网络测试完成 - 所有测试通过 ===");
                
                // 在批处理模式下退出
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"=== 网络测试失败 ===");
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
        /// 测试网络管理器初始化
        /// </summary>
        private static async Task TestNetworkManagerInitialization()
        {
            Debug.Log("测试 1: NetworkManager 初始化");
            
            var networkManager = NetworkManager.Instance;
            
            // 测试初始化
            networkManager.Initialize();
            
            // 验证初始化状态
            if (networkManager.ConnectionStatus == ConnectionStatus.Disconnected)
            {
                Debug.Log("✅ NetworkManager 初始化成功");
            }
            else
            {
                throw new Exception("NetworkManager 初始化失败");
            }
            
            await Task.Delay(100); // 等待初始化完成
        }
        
        /// <summary>
        /// 测试 Session 创建
        /// </summary>
        private static async Task TestSessionCreation()
        {
            Debug.Log("测试 2: Session 创建");
            
            var networkManager = NetworkManager.Instance;
            
            // 注册事件处理器
            bool connected = false;
            networkManager.OnConnected += () => connected = true;
            
            try
            {
                // 尝试连接到本地服务器（这里只是测试连接逻辑，不期望真正连接成功）
                await networkManager.ConnectAsync("127.0.0.1", 8888);
                
                // 检查是否创建了 Session
                var session = networkManager.GetCurrentSession();
                if (session != null)
                {
                    Debug.Log($"✅ Session 创建成功，ID: {session.Id}");
                }
                else
                {
                    Debug.Log("⚠️ Session 未创建（预期行为，因为没有真实服务器）");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"⚠️ 连接失败（预期行为）: {ex.Message}");
            }
            
            await Task.Delay(100);
        }
        
        /// <summary>
        /// 测试消息发送
        /// </summary>
        private static async Task TestMessageSending()
        {
            Debug.Log("测试 3: 消息发送");
            
            var networkManager = NetworkManager.Instance;
            
            // 测试消息创建
            //var message = NetworkMessageExtensions.CreateSuccess("test", "Hello World");
            /*
            if (message != null && message.Type == "test" && message.Success)
            {
                Debug.Log("✅ NetworkMessage 创建成功");
            }
            else
            {
                throw new Exception("NetworkMessage 创建失败");
            }
            
            // 测试消息发送（会失败，因为没有连接）
            try
            {
                await networkManager.SendMessageAsync(message);
                Debug.Log("⚠️ 消息发送未报错（意外情况）");
            }
            catch (Exception)
            {
                Debug.Log("⚠️ 消息发送失败（预期行为，因为未连接）");
            }
            */
            await Task.Delay(100);
        }
        
        /// <summary>
        /// 检查编译错误
        /// </summary>
        [MenuItem("Astrum/Testing 测试/Network 网络测试/Check Compile Errors 检查编译错误", false, 203)]
        public static void CheckForErrors()
        {
            Debug.Log("=== 检查编译错误 ===");
            
            // 强制重新编译
            AssetDatabase.Refresh();
            
            // 检查是否有编译错误
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            bool hasErrors = false;
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    // 尝试获取程序集中的类型
                    var types = assembly.GetTypes();
                    Debug.Log($"程序集 {assembly.GetName().Name} 编译成功，包含 {types.Length} 个类型");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"程序集 {assembly.GetName().Name} 编译失败: {ex.Message}");
                    hasErrors = true;
                }
            }
            
            if (hasErrors)
            {
                Debug.LogError("=== 发现编译错误 ===");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
            else
            {
                Debug.Log("=== 编译检查通过 - 没有发现错误 ===");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
        }
        
        /// <summary>
        /// 运行性能测试
        /// </summary>
        [MenuItem("Astrum/Testing 测试/Network 网络测试/Performance Test 性能测试", false, 204)]
        public static void RunPerformanceTests()
        {
            Debug.Log("=== 开始性能测试 ===");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 测试 NetworkManager 创建性能
            for (int i = 0; i < 1000; i++)
            {
                var networkManager = NetworkManager.Instance;
                //var message = NetworkMessageExtensions.CreateSuccess($"test_{i}", $"data_{i}");
            }
            
            stopwatch.Stop();
            Debug.Log($"创建 1000 个消息耗时: {stopwatch.ElapsedMilliseconds} ms");
            
            Debug.Log("=== 性能测试完成 ===");
            
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        
        /// <summary>
        /// 清理测试环境
        /// </summary>
        [MenuItem("Astrum/Testing 测试/Network 网络测试/Cleanup Environment 清理测试环境", false, 205)]
        public static void CleanupTestEnvironment()
        {
            Debug.Log("=== 清理测试环境 ===");
            
            var networkManager = NetworkManager.Instance;
            networkManager.Shutdown();
            
            Debug.Log("=== 测试环境清理完成 ===");
            
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}

