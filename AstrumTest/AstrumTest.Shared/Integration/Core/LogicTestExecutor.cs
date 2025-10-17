using System;
using System.Collections.Generic;
using System.IO;
using Astrum.LogicCore;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Archetypes;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Factories;
using AstrumTest.Shared.Fixtures;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace AstrumTest.Shared.Integration.Core
{
    /// <summary>
    /// 逻辑测试执行引擎 - 固定流程封装
    /// 
    /// 用户只需调用：RunTestCase("scenarioName.json")
    /// 引擎自动完成：读取 → 初始化 → 执行 → 验证 → 报告
    /// </summary>
    public partial class LogicTestExecutor : IDisposable
    {
        // 真实的游戏逻辑对象
        public Room Room { get; private set; }
        public World World => Room?.MainWorld;
        public LSController LSController => Room?.LSController;
        
        private readonly ITestOutputHelper _output;
        private readonly string _dataDirectory;  // 测试数据目录
        private Dictionary<string, EntityTemplate> _entityTemplates;  // 实体模板
        private Dictionary<string, Entity> _entities;  // 实体映射表（entityId → Entity）
        private bool _isInitialized = false;
        
        public LogicTestExecutor(ITestOutputHelper output, ConfigFixture configFixture)
        {
            _output = output;
            
            // 测试数据目录：AstrumTest.Shared/Integration/Data/Scenarios/
            _dataDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "Integration", "Data", "Scenarios"
            );
            
            // 确保目录存在
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }
        
        /// <summary>
        /// 运行测试用例（完整固定流程）
        /// 用户只传文件名，引擎自动完成所有步骤
        /// </summary>
        public TestResult RunTestCase(string jsonFileName)
        {
            _output.WriteLine($"========================================");
            _output.WriteLine($"运行测试用例: {jsonFileName}");
            _output.WriteLine($"========================================");
            
            try
            {
                // ===== 步骤1: 读取 JSON 用例文件 =====
                var testCase = LoadTestCaseFromJson(jsonFileName);
                _output.WriteLine($"[1/4] ✓ 读取测试用例: {testCase.Name}");
                
                // ===== 步骤2: 初始化所有 Manager =====
                if (!_isInitialized)
                {
                    InitializeManagers();
                    _isInitialized = true;
                }
                _output.WriteLine($"[2/4] ✓ Manager 初始化完成");
                
                // ===== 步骤3: 创建 Room + World =====
                CreateRoomAndWorld();
                _output.WriteLine($"[3/4] ✓ Room + World 创建完成");
                
                // ===== 步骤4: 逐帧执行测试（核心流程）=====
                // 注意：实体现在通过 CreateEntity 输入指令创建，不再预先创建
                var (success, passedFrames, failedFrames) = ExecuteFrames(testCase.Frames);
                _output.WriteLine($"[4/4] ✓ 所有帧执行完成");
                
                // 生成测试报告
                var result = new TestResult
                {
                    TestCaseName = testCase.Name,
                    Success = success,
                    TotalFrames = testCase.Frames.Count,
                    PassedFrames = passedFrames,
                    FailedFrames = failedFrames,
                    FailureMessage = success ? null : $"有 {failedFrames} 帧验证失败"
                };
                
                _output.WriteLine($"========================================");
                _output.WriteLine($"{(success ? "✅ 测试通过" : "❌ 测试失败")}: {testCase.Name}");
                _output.WriteLine($"  总帧数: {result.TotalFrames}");
                _output.WriteLine($"  通过帧: {result.PassedFrames}");
                _output.WriteLine($"  失败帧: {result.FailedFrames}");
                _output.WriteLine($"========================================");
                
                return result;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ 测试执行异常: {ex.Message}");
                _output.WriteLine(ex.StackTrace);
                
                return new TestResult
                {
                    Success = false,
                    FailureMessage = $"测试执行异常: {ex.Message}"
                };
            }
        }
        
        // ========== 固定流程实现 ==========
        
        /// <summary>
        /// 步骤1: 读取 JSON 用例文件
        /// </summary>
        private TestCaseData LoadTestCaseFromJson(string jsonFileName)
        {
            var filePath = Path.Combine(_dataDirectory, jsonFileName);
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"测试用例文件不存在: {filePath}");
            }
            
            var json = File.ReadAllText(filePath);
            var testCase = JsonConvert.DeserializeObject<TestCaseData>(json);
            
            _output.WriteLine($"  测试用例: {testCase.Name}");
            _output.WriteLine($"  实体模板数量: {testCase.EntityTemplates.Count}");
            _output.WriteLine($"  测试帧数: {testCase.Frames.Count}");
            
            // 加载实体模板到字典
            _entityTemplates = new Dictionary<string, EntityTemplate>();
            foreach (var template in testCase.EntityTemplates)
            {
                _entityTemplates[template.TemplateId] = template;
                _output.WriteLine($"    模板: {template.TemplateId} (RoleId={template.RoleId})");
            }
            
            return testCase;
        }
        
        /// <summary>
        /// 步骤2: 初始化所有 Manager（模拟游戏启动）
        /// </summary>
        private void InitializeManagers()
        {
            // 初始化 ArchetypeManager
            ArchetypeManager.Instance.Initialize();
            _output.WriteLine("  ✓ ArchetypeManager initialized");
            
            // ConfigManager 已在 ConfigFixture 中初始化
            _output.WriteLine("  ✓ ConfigManager already initialized");
            
            // 初始化 SkillEffectManager
            SkillEffectManager.Instance.ClearQueue();
            _output.WriteLine("  ✓ SkillEffectManager initialized");
            
            // HitManager 使用单例，自动初始化
            _output.WriteLine("  ✓ HitManager ready");
        }
        
        /// <summary>
        /// 步骤3: 创建 Room + World（模拟单人模式创建）
        /// </summary>
        private void CreateRoomAndWorld()
        {
            // 创建 Room（真实环境）
            Room = new Room(1, "TestRoom");
            
            // 创建 World（真实环境）
            var world = new World { WorldId = 1, Name = "TestWorld" };
            Room.MainWorld = world;
            
            // 初始化 Room（会创建 LSController）
            Room.Initialize();
            
            _output.WriteLine($"  ✓ Room created: ID={Room.RoomId}, Name={Room.Name}");
            _output.WriteLine($"  ✓ World created: ID={world.WorldId}, Name={world.Name}");
            _output.WriteLine($"  ✓ LSController created");
        }
        
        /// <summary>
        /// 步骤4: 逐帧执行测试（核心流程）
        /// 每一帧：执行输入 → 更新逻辑 → 执行查询 → 验证预期
        /// </summary>
        private (bool success, int passedFrames, int failedFrames) ExecuteFrames(List<FrameData> frames)
        {
            _output.WriteLine("========== 开始逐帧执行 ==========");
            
            // 初始化实体映射表
            _entities = new Dictionary<string, Entity>();
            
            // 启动帧同步
            LSController.Start();
            _output.WriteLine("  ✓ LSController 已启动\n");
            
            int currentFrame = 0;
            int passedCount = 0;
            int failedCount = 0;
            
            foreach (var frameData in frames)
            {
                // 推进到目标帧
                while (currentFrame < frameData.FrameNumber)
                {
                    UpdateLogicFrame();
                    currentFrame++;
                }
                
                _output.WriteLine($"========== Frame {frameData.FrameNumber} ==========");
                _output.WriteLine($"  {frameData.Comment}");
                
                // [1] 执行输入指令
                if (frameData.Inputs != null && frameData.Inputs.Count > 0)
                {
                    _output.WriteLine($"  [输入] 执行 {frameData.Inputs.Count} 个输入指令");
                    foreach (var input in frameData.Inputs)
                    {
                        ExecuteInput(input);
                    }
                }
                
                // [2] 更新游戏逻辑一帧
                UpdateLogicFrame();
                currentFrame++;
                
                // [3] 执行查询指令
                var queryResults = new Dictionary<string, object>();
                if (frameData.Queries != null && frameData.Queries.Count > 0)
                {
                    _output.WriteLine($"  [查询] 执行 {frameData.Queries.Count} 个查询指令");
                    foreach (var query in frameData.Queries)
                    {
                        var result = ExecuteQuery(query);
                        queryResults[query.ResultKey] = result;
                        _output.WriteLine($"    {query.ResultKey} = {result}");
                    }
                }
                
                // [4] 验证预期输出
                if (frameData.ExpectedOutputs != null && frameData.ExpectedOutputs.Count > 0)
                {
                    _output.WriteLine($"  [验证] 验证 {frameData.ExpectedOutputs.Count} 个预期结果");
                    bool frameSuccess = VerifyExpectedOutputs(queryResults, frameData.ExpectedOutputs);
                    
                    if (!frameSuccess)
                    {
                        _output.WriteLine($"  ❌ Frame {frameData.FrameNumber} 验证失败");
                        failedCount++;
                    }
                    else
                    {
                        _output.WriteLine($"  ✅ Frame {frameData.FrameNumber} 验证通过");
                        passedCount++;
                    }
                }
                
                _output.WriteLine("");
            }
            
            _output.WriteLine("========== 逐帧执行完成 ==========\n");
            
            return (failedCount == 0, passedCount, failedCount);
        }
        
        /// <summary>
        /// 更新单帧逻辑（模拟单人模式 Update）
        /// </summary>
        private void UpdateLogicFrame()
        {
            // 模拟单人模式：AuthorityFrame = PredictionFrame
            if (LSController != null && LSController.IsRunning)
            {
                LSController.AuthorityFrame = LSController.PredictionFrame;
            }
            
            // Room.Update() → LSController.Tick() → World.Update()
            Room.Update(0.016f);  // 60FPS
            
            // 处理技能效果队列（关键！）
            SkillEffectManager.Instance.Update();
        }
        
        public void Dispose()
        {
            try
            {
                SkillEffectManager.Instance?.ClearQueue();
                _entities?.Clear();
                _output.WriteLine("✅ 测试执行引擎已清理");
            }
            catch
            {
                // 忽略 Dispose 中的错误（避免 MemoryPack 序列化问题）
            }
        }
    }
}

