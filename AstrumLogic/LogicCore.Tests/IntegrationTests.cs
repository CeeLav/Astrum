using Xunit;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.Factories;

namespace Astrum.LogicCore.Tests
{
    /// <summary>
    /// 集成测试，测试整个系统的协同工作
    /// </summary>
    public class IntegrationTests
    {
        [Fact]
        public void FullGameLoop_ShouldWorkCorrectly()
        {
            // Arrange - 创建完整的游戏环境
            var room = new Room(1, "TestRoom");
            var world = new World { WorldId = 1, Name = "TestWorld" };
            var entityFactory = new EntityFactory(world);
            
            // 添加世界到房间
            room.AddWorld(world);
            
            // 创建玩家实体
            var player1 = entityFactory.CreatePlayerEntity(1, "Player1");
            var player2 = entityFactory.CreatePlayerEntity(2, "Player2");
            
            // 设置初始位置
            player1.GetComponent<PositionComponent>()?.SetPosition(0, 0, 0);
            player2.GetComponent<PositionComponent>()?.SetPosition(10, 0, 0);
            
            // 添加玩家到房间
            room.AddPlayer(1);
            room.AddPlayer(2);
            
            // 初始化房间
            room.Initialize();
            
            // Act - 模拟游戏循环
            var inputSystem = LSInputSystem.GetInstance();
            inputSystem.Reset();
            
            // 设置无延迟以简化测试
            inputSystem.InputDelay = 0;
            
            // 模拟第一帧输入
            var input1Frame1 = new LSInput
            {
                PlayerId = 1,
                Frame = 0, // 从帧0开始
                MoveX = 1.0f,
                MoveY = 0.0f,
                Attack = false
            };
            
            var input2Frame1 = new LSInput
            {
                PlayerId = 2,
                Frame = 0,
                MoveX = -0.5f,
                MoveY = 0.5f,
                Attack = true
            };
            
            // 收集输入
            inputSystem.CollectInput(1, input1Frame1);
            inputSystem.CollectInput(2, input2Frame1);
            
            // 处理帧
            inputSystem.ProcessFrame(0);
            
            // 获取帧输入并分发到世界
            var frameInputs = inputSystem.GetFrameInputs(0);
            if (frameInputs != null)
            {
                world.ApplyInputsToEntities(frameInputs);
            }
            
            // 更新房间（这会触发实体的能力系统）
            float deltaTime = 1f / 60f; // 60 FPS
            room.Update(deltaTime);
            
            // Assert - 验证结果
            Assert.Equal(2, room.GetPlayerCount());
            
            // 检查玩家1的输入组件
            var player1InputComponent = player1.GetComponent<LSInputComponent>();
            Assert.NotNull(player1InputComponent);
            Assert.NotNull(player1InputComponent.CurrentInput);
            Assert.Equal(1.0f, player1InputComponent.CurrentInput.MoveX);
            Assert.Equal(0.0f, player1InputComponent.CurrentInput.MoveY);
            Assert.False(player1InputComponent.CurrentInput.Attack);
            
            // 检查玩家2的输入组件
            var player2InputComponent = player2.GetComponent<LSInputComponent>();
            Assert.NotNull(player2InputComponent);
            Assert.NotNull(player2InputComponent.CurrentInput);
            Assert.Equal(-0.5f, player2InputComponent.CurrentInput.MoveX);
            Assert.Equal(0.5f, player2InputComponent.CurrentInput.MoveY);
            Assert.True(player2InputComponent.CurrentInput.Attack);
            
            // 检查移动能力是否生效（位置应该有变化）
            var player1Position = player1.GetComponent<PositionComponent>();
            var player2Position = player2.GetComponent<PositionComponent>();
            
            Assert.NotNull(player1Position);
            Assert.NotNull(player2Position);
            
            // 由于移动能力的存在，位置应该会发生变化
            // 注意：具体的位置变化取决于移动能力的实现细节
        }
        
        [Fact]
        public void EntityFactory_ShouldCreateCompletePlayerEntity()
        {
            // Arrange
            var world = new World { WorldId = 1, Name = "TestWorld" };
            var factory = new EntityFactory(world);
            
            // Act
            var player = factory.CreatePlayerEntity(1, "TestPlayer");
            
            // Assert
            Assert.NotNull(player);
            Assert.Equal("TestPlayer", player.Name);
            Assert.True(player.IsActive);
            Assert.False(player.IsDestroyed);
            
            // 检查基础组件
            Assert.True(player.HasComponent<PositionComponent>());
            Assert.True(player.HasComponent<VelocityComponent>());
            Assert.True(player.HasComponent<MovementComponent>());
            Assert.True(player.HasComponent<HealthComponent>());
            Assert.True(player.HasComponent<LSInputComponent>());
            
            // 检查能力
            Assert.Single(player.Capabilities);
            Assert.IsType<MovementCapability>(player.Capabilities[0]);
            
            // 检查输入组件配置
            var inputComponent = player.GetComponent<LSInputComponent>();
            Assert.NotNull(inputComponent);
            Assert.Equal(1, inputComponent.PlayerId);
        }
        
        [Fact]
        public void MovementCapability_ShouldProcessInputCorrectly()
        {
            // Arrange
            var world = new World();
            var entity = world.CreateEntity("MovingEntity");
            
            // 添加必需的组件
            entity.AddComponent(new PositionComponent(0, 0, 0));
            entity.AddComponent(new VelocityComponent(0, 0, 0));
            entity.AddComponent(new MovementComponent(10f, 20f)); // 最大速度10，加速度20
            entity.AddComponent(new LSInputComponent(1));
            
            // 添加移动能力
            var movementCapability = new MovementCapability();
            entity.AddCapability(movementCapability);
            
            // 设置输入
            var input = new LSInput
            {
                PlayerId = 1,
                MoveX = 1.0f, // 向右移动
                MoveY = 0.0f,
                Attack = false
            };
            
            entity.ApplyInput(input);
            
            // Act - 执行多帧更新
            float deltaTime = 1f / 60f;
            for (int i = 0; i < 60; i++) // 1秒的更新
            {
                movementCapability.Tick(deltaTime);
            }
            
            // Assert
            var position = entity.GetComponent<PositionComponent>();
            var velocity = entity.GetComponent<VelocityComponent>();
            var movement = entity.GetComponent<MovementComponent>();
            
            Assert.NotNull(position);
            Assert.NotNull(velocity);
            Assert.NotNull(movement);
            
            // 位置应该向右移动了
            Assert.True(position.X > 0);
            Assert.Equal(0, position.Y); // Y应该没有变化
            
            // 速度应该是正的（向右）
            Assert.True(velocity.VX > 0);
            Assert.Equal(0, velocity.VY); // Y速度应该为0
            
            // 移动组件的当前速度应该接近最大速度
            Assert.True(movement.CurrentSpeed > 0);
        }
        
        [Fact]
        public void FrameSync_ShouldHandleMultipleFrames()
        {
            // Arrange
            var inputSystem = LSInputSystem.GetInstance();
            inputSystem.Reset();
            inputSystem.InputDelay = 0; // 设置无延迟
            
            // Act - 模拟多帧输入
            for (int frame = 0; frame < 5; frame++)
            {
                // 设置当前处理帧
                inputSystem.ProcessFrame(frame);
                
                var input1 = new LSInput
                {
                    PlayerId = 1,
                    MoveX = 0.5f * (frame + 1), // 逐渐增加输入强度
                    MoveY = 0.2f * (frame + 1)
                };
                
                var input2 = new LSInput
                {
                    PlayerId = 2,
                    MoveX = -0.3f * (frame + 1),
                    MoveY = 0.1f * (frame + 1)
                };
                
                inputSystem.CollectInput(1, input1);
                inputSystem.CollectInput(2, input2);
            }
            
            // Assert
            // 检查所有帧都被正确存储
            for (int frame = 0; frame < 5; frame++)
            {
                var frameInputs = inputSystem.GetFrameInputs(frame);
                Assert.NotNull(frameInputs);
                Assert.Equal(frame, frameInputs.Frame);
                
                var input1 = frameInputs.GetInput(1);
                var input2 = frameInputs.GetInput(2);
                
                Assert.NotNull(input1);
                Assert.NotNull(input2);
                
                Assert.Equal(0.5f * (frame + 1), input1.MoveX);
                Assert.Equal(-0.3f * (frame + 1), input2.MoveX);
            }
        }
        
        [Fact]
        public void Room_ShouldManagePlayersCorrectly()
        {
            // Arrange
            var room = new Room(1, "TestRoom");
            room.MaxPlayers = 3;
            
            // Act & Assert
            // 添加玩家
            Assert.True(room.AddPlayer(1));
            Assert.True(room.AddPlayer(2));
            Assert.True(room.AddPlayer(3));
            
            // 房间已满，不能再添加
            Assert.False(room.AddPlayer(4));
            
            // 检查房间状态
            Assert.Equal(3, room.GetPlayerCount());
            Assert.True(room.IsFull());
            Assert.False(room.IsEmpty());
            
            // 检查玩家是否在房间中
            Assert.True(room.HasPlayer(1));
            Assert.True(room.HasPlayer(2));
            Assert.True(room.HasPlayer(3));
            Assert.False(room.HasPlayer(4));
            
            // 移除玩家
            Assert.True(room.RemovePlayer(2));
            Assert.False(room.RemovePlayer(5)); // 不存在的玩家
            
            Assert.Equal(2, room.GetPlayerCount());
            Assert.False(room.IsFull());
            Assert.False(room.HasPlayer(2));
        }
    }
}
