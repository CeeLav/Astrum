using System;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Factories;

namespace AstrumLogic.Debug
{
    /// <summary>
    /// LogicCore 调试程序
    /// </summary>
    class LogicCoreDebug
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== AstrumLogic Debug Program ===");
            Console.WriteLine();

            try
            {
                // 测试 CommonBase
                TestCommonBase();
                
                // 测试 LogicCore
                TestLogicCore();

                Console.WriteLine("所有测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        static void TestCommonBase()
        {
            Console.WriteLine("=== 测试 CommonBase ===");
            
            // 测试日志系统
            ASLogger.Log(LogLevel.Info, "CommonBase", "日志系统测试");
            
            // 测试事件系统
            var eventSystem = EventSystem.Instance;
            Console.WriteLine($"事件系统初始化: {eventSystem != null}");
            
            // 测试对象池
            var pool = ObjectPool<TestObject>.Instance;
            var obj = pool.Get();
            pool.Return(obj);
            Console.WriteLine("对象池测试通过");
            
            Console.WriteLine("CommonBase 测试完成");
            Console.WriteLine();
        }

        static void TestLogicCore()
        {
            Console.WriteLine("=== 测试 LogicCore ===");
            
            // 创建世界
            var world = new World();
            Console.WriteLine($"世界创建成功: {world.Name}");
            
            // 创建实体工厂
            var factory = new EntityFactory(world);
            Console.WriteLine("实体工厂创建成功");
            
            // 创建一个 Unit
            var unit = factory.CreateEntity<Unit>("TestUnit");
            Console.WriteLine($"Unit 创建成功: {unit.Name}, ID: {unit.UniqueId}");
            
            // 检查组件
            var positionComponent = unit.GetComponent<PositionComponent>();
            if (positionComponent != null)
            {
                Console.WriteLine($"位置组件: {positionComponent.Position}");
            }
            
            var healthComponent = unit.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                Console.WriteLine($"生命值组件: {healthComponent.CurrentHealth}/{healthComponent.MaxHealth}");
            }
            
            Console.WriteLine("LogicCore 测试完成");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 测试对象
    /// </summary>
    public class TestObject
    {
        public int Value { get; set; }
    }
}
