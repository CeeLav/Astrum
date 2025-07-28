using System;
using System.Threading.Tasks;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.Factories;

namespace Astrum.LogicCore.Tests
{
    /// <summary>
    /// 战斗模拟控制台程序
    /// </summary>
    class Program
    {
        private static Room? gameRoom;
        private static World? gameWorld;
        private static EntityFactory? entityFactory;
        private static LSInputSystem? inputSystem;
        private static Random random = new Random();
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Astrum 战斗逻辑模拟器 ===");
            Console.WriteLine("程序开始执行...");
            Console.WriteLine();
            
            try 
            {
                Console.WriteLine("准备运行战斗模拟...");
                await RunBattleSimulation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"模拟过程中发生错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
        
        static async Task RunBattleSimulation()
        {
            // 初始化游戏环境
            InitializeGameEnvironment();
            
            // 创建玩家
            var players = CreatePlayers();
            
            // 显示初始状态
            DisplayGameState("=== 游戏开始 ===", players);
            
            // 运行战斗循环
            await RunBattleLoop(players, 300); // 运行300帧 (5秒 @ 60fps)
            
            // 显示最终结果
            DisplayFinalResults(players);
        }
        
        static void InitializeGameEnvironment()
        {
            Console.WriteLine("初始化游戏环境...");
            
            // 创建房间和世界
            gameRoom = new Room(1, "战斗房间");
            gameWorld = new World { WorldId = 1, Name = "战斗世界" };
            entityFactory = EntityFactory.Instance;
            entityFactory.Initialize(gameWorld);
            
            // 设置房间
            gameRoom.AddWorld(gameWorld);
            gameRoom.Initialize();
            
            // 初始化输入系统
            inputSystem = LSInputSystem.GetInstance();
            inputSystem.Reset();
            inputSystem.InputDelay = 0; // 无延迟模式
            
            Console.WriteLine("游戏环境初始化完成！");
            Console.WriteLine();
        }
        
        static Entity[] CreatePlayers()
        {
            Console.WriteLine("创建玩家实体...");
            
            var player1 = entityFactory!.CreateEntity<Unit>("勇士阿尔法");
            var player2 = entityFactory!.CreateEntity<Unit>("法师贝塔");
            
            // 设置初始位置
            player1.GetComponent<PositionComponent>()?.SetPosition(-5, 0, 0);
            player2.GetComponent<PositionComponent>()?.SetPosition(5, 0, 0);
            
            // 设置不同的生命值和属性
            var health1 = player1.GetComponent<HealthComponent>();
            var health2 = player2.GetComponent<HealthComponent>();
            
            if (health1 != null) 
            {
                health1.MaxHealth = 150;
                health1.CurrentHealth = 150;
            }
            
            if (health2 != null) 
            {
                health2.MaxHealth = 100;
                health2.CurrentHealth = 100;
            }
            
            // 添加玩家到房间
            gameRoom!.AddPlayer(1);
            gameRoom!.AddPlayer(2);
            
            Console.WriteLine($"玩家1: {player1.Name} (生命值: {health1?.CurrentHealth}/{health1?.MaxHealth})");
            Console.WriteLine($"玩家2: {player2.Name} (生命值: {health2?.CurrentHealth}/{health2?.MaxHealth})");
            Console.WriteLine();
            
            return new Entity[] { player1, player2 };
        }
        
        static async Task RunBattleLoop(Entity[] players, int totalFrames)
        {
            Console.WriteLine("开始战斗模拟...");
            Console.WriteLine();
            
            float deltaTime = 1f / 60f; // 60 FPS
            int reportInterval = 60; // 每秒报告一次状态
            
            for (int frame = 0; frame < totalFrames; frame++)
            {
                // 生成AI输入
                GenerateAIInputs(players, frame);
                
                // 处理输入帧
                inputSystem!.ProcessFrame(frame);
                
                // 获取帧输入并应用到实体
                var frameInputs = inputSystem.GetFrameInputs(frame);
                if (frameInputs != null)
                {
                    gameWorld!.ApplyInputsToEntities(frameInputs);
                }
                
                // 更新游戏状态
                gameRoom!.Update(deltaTime);
                
                // 模拟战斗伤害
                SimulateCombatDamage(players, frame);
                
                // 定期报告状态
                if (frame % reportInterval == 0)
                {
                    DisplayGameState($"=== 第 {frame/60 + 1} 秒 ===", players);
                }
                
                // 检查游戏是否结束
                if (IsBattleFinished(players))
                {
                    Console.WriteLine($"\n战斗在第 {frame} 帧结束!");
                    break;
                }
                
                // 短暂延迟以便观察
                await Task.Delay(50);
            }
        }
        
        static void GenerateAIInputs(Entity[] players, int frame)
        {
            foreach (var player in players)
            {
                var inputComponent = player.GetComponent<LSInputComponent>();
                if (inputComponent == null) continue;
                
                var input = new LSInput
                {
                    PlayerId = inputComponent.PlayerId,
                    Frame = frame
                };
                
                // 简单的AI逻辑
                var position = player.GetComponent<PositionComponent>();
                var health = player.GetComponent<HealthComponent>();
                
                if (position != null && health != null)
                {
                    // 低血量时后退，否则接近敌人
                    if (health.HealthPercentage < 0.3f)
                    {
                        // 后退
                        input.MoveX = position.X > 0 ? 1.0f : -1.0f;
                        input.MoveY = random.NextSingle() * 2 - 1; // 随机Y方向移动
                    }
                    else
                    {
                        // 接近敌人
                        input.MoveX = position.X > 0 ? -0.8f : 0.8f;
                        input.MoveY = (random.NextSingle() - 0.5f) * 0.5f;
                    }
                    
                    // 随机攻击
                    input.Attack = random.Next(0, 100) < 15; // 15%概率攻击
                }
                
                inputSystem!.CollectInput(inputComponent.PlayerId, input);
            }
        }
        
        static void SimulateCombatDamage(Entity[] players, int frame)
        {
            // 简单的战斗伤害模拟
            foreach (var attacker in players)
            {
                var attackerInput = attacker.GetComponent<LSInputComponent>();
                var attackerPosition = attacker.GetComponent<PositionComponent>();
                var attackerHealth = attacker.GetComponent<HealthComponent>();
                
                if (attackerInput?.CurrentInput?.Attack == true && 
                    attackerPosition != null && 
                    attackerHealth != null && 
                    !attackerHealth.IsDead)
                {
                    // 寻找攻击范围内的敌人
                    foreach (var target in players)
                    {
                        if (target == attacker) continue;
                        
                        var targetPosition = target.GetComponent<PositionComponent>();
                        var targetHealth = target.GetComponent<HealthComponent>();
                        
                        if (targetPosition != null && targetHealth != null && !targetHealth.IsDead)
                        {
                            // 计算距离
                            float distance = MathF.Sqrt(
                                MathF.Pow(attackerPosition.X - targetPosition.X, 2) +
                                MathF.Pow(attackerPosition.Y - targetPosition.Y, 2)
                            );
                            
                            // 攻击范围为3单位
                            if (distance <= 3.0f)
                            {
                                int damage = random.Next(15, 25); // 随机伤害15-25
                                int actualDamage = targetHealth.TakeDamage(damage);
                                
                                Console.WriteLine($"  🗡️ {attacker.Name} 对 {target.Name} 造成了 {actualDamage} 点伤害! (距离: {distance:F1})");
                                
                                if (targetHealth.IsDead)
                                {
                                    Console.WriteLine($"  💀 {target.Name} 被击败了!");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        static bool IsBattleFinished(Entity[] players)
        {
            int alivePlayers = 0;
            foreach (var player in players)
            {
                var health = player.GetComponent<HealthComponent>();
                if (health != null && !health.IsDead)
                {
                    alivePlayers++;
                }
            }
            
            return alivePlayers <= 1;
        }
        
        static void DisplayGameState(string title, Entity[] players)
        {
            Console.WriteLine(title);
            Console.WriteLine();
            
            foreach (var player in players)
            {
                var position = player.GetComponent<PositionComponent>();
                var health = player.GetComponent<HealthComponent>();
                var velocity = player.GetComponent<VelocityComponent>();
                var inputComp = player.GetComponent<LSInputComponent>();
                
                string status = health?.IsDead == true ? "💀 已死亡" : "✅ 存活";
                string healthBar = CreateHealthBar(health);
                
                Console.WriteLine($"👤 {player.Name}: {status}");
                Console.WriteLine($"   生命值: {healthBar} ({health?.CurrentHealth}/{health?.MaxHealth})");
                Console.WriteLine($"   位置: ({position?.X:F1}, {position?.Y:F1}, {position?.Z:F1})");
                Console.WriteLine($"   速度: ({velocity?.VX:F1}, {velocity?.VY:F1})");
                
                if (inputComp?.CurrentInput != null)
                {
                    var input = inputComp.CurrentInput;
                    Console.WriteLine($"   输入: 移动({input.MoveX:F1}, {input.MoveY:F1}) 攻击:{(input.Attack ? "是" : "否")}");
                }
                
                Console.WriteLine();
            }
        }
        
        static string CreateHealthBar(HealthComponent? health)
        {
            if (health == null) return "未知";
            
            int barLength = 20;
            int filledLength = (int)(health.HealthPercentage * barLength);
            
            string bar = "[";
            for (int i = 0; i < barLength; i++)
            {
                if (i < filledLength)
                    bar += "█";
                else
                    bar += "░";
            }
            bar += "]";
            
            return bar;
        }
        
        static void DisplayFinalResults(Entity[] players)
        {
            Console.WriteLine();
            Console.WriteLine("=== 战斗结果 ===");
            Console.WriteLine();
            
            Entity? winner = null;
            foreach (var player in players)
            {
                var health = player.GetComponent<HealthComponent>();
                if (health != null && !health.IsDead)
                {
                    winner = player;
                    break;
                }
            }
            
            if (winner != null)
            {
                Console.WriteLine($"🏆 胜利者: {winner.Name}!");
                var winnerHealth = winner.GetComponent<HealthComponent>();
                Console.WriteLine($"   剩余生命值: {winnerHealth?.CurrentHealth}/{winnerHealth?.MaxHealth}");
            }
            else
            {
                Console.WriteLine("🤝 平局 - 所有玩家都被击败了!");
            }
            
            Console.WriteLine();
            Console.WriteLine("详细统计:");
            foreach (var player in players)
            {
                var health = player.GetComponent<HealthComponent>();
                var position = player.GetComponent<PositionComponent>();
                
                Console.WriteLine($"  {player.Name}:");
                Console.WriteLine($"    最终生命值: {health?.CurrentHealth}/{health?.MaxHealth}");
                Console.WriteLine($"    最终位置: ({position?.X:F1}, {position?.Y:F1}, {position?.Z:F1})");
                Console.WriteLine($"    状态: {(health?.IsDead == true ? "阵亡" : "存活")}");
            }
        }
    }
}
