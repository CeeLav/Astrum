using System;
using System.Collections;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.LogicCore.Core;
using Astrum.View.Core;
using Astrum.View.Stages;
using Vector3 = UnityEngine.Vector3;

namespace Astrum.Client.Managers
{

    /// <summary>
    /// 游戏玩法管理器 - 负责管理一局游戏的内容
    /// </summary>
    public class GamePlayManager : Singleton<GamePlayManager>
    {
        
        public Stage MainStage { get; private set; }
        public Room MainRoom { get; private set; }
        
        
        public void Initialize()
        {
            //throw new NotImplementedException();
        }

        public void Update(float deltaTime)
        {
            if (MainRoom != null)
            {
                MainRoom.Update(deltaTime);
            }

            if (MainStage != null)
            {
                MainStage.Update(deltaTime);
            }
            
            //throw new NotImplementedException();
        }
        
        public void Shutdown()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 启动单机游戏
        /// </summary>
        /// <param name="gameSceneName">游戏场景名称</param>
        public void StartSinglePlayerGame(string gameSceneName)
        {
            ASLogger.Instance.Info("GameLauncher: 启动游戏");
            
            try
            {
                // 1. 创建Room (这里模拟逻辑层的Room)
                var room = CreateRoom();
                var world = new World();
                room.AddWorld(world);
                room.Initialize();
                // 2. 创建Stage
                Stage gameStage = CreateStage(room);
                
                // 3. 创建Player并加入
                CreatePlayerAndJoin(gameStage, room);
                
                // 4. 切换场景
                SwitchToGameScene(gameSceneName, gameStage);
                
                ASLogger.Instance.Info("GameLauncher: 游戏启动成功");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GameLauncher: 启动游戏失败 - {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 创建Room - 模拟逻辑层的Room创建
        /// </summary>
        private Room CreateRoom()
        {
            // 这里只是模拟创建Room，实际的Room逻辑在逻辑层
            var room = new Room(1, "SinglePlayerRoom");
            ASLogger.Instance.Info($"GameLauncher: 创建Room，ID: {room.RoomId}");
            return room;
        }
        
        /// <summary>
        /// 创建Stage
        /// </summary>
        private Stage CreateStage(Room room)
        {
            ASLogger.Instance.Info("GameLauncher: 创建Stage");
            
            // 创建游戏Stage
            Stage gameStage = new Stage("GameStage", "游戏场景");
            
            // 初始化Stage
            gameStage.Initialize();
            
            // 设置Room ID
            gameStage.SetRoom(room);
            
            return gameStage;
        }
        
        /// <summary>
        /// 创建Player并加入Stage
        /// </summary>
        private void CreatePlayerAndJoin(Stage gameStage, Room room)
        {
            ASLogger.Instance.Info("GameLauncher: 创建Player并加入Stage");
            
            
            long playerId = room.AddPlayer();
            Vector3 playerPosition = new Vector3(-5f, 0.5f, 0f);

            
            ASLogger.Instance.Info($"GameLauncher: Player创建完成，ID: {playerId}");
        }
        
        /// <summary>
        /// 切换到游戏场景
        /// </summary>
        private void SwitchToGameScene(string gameSceneName, Stage gameStage)
        {
            ASLogger.Instance.Info($"GameLauncher: 切换到游戏场景 {gameSceneName}");
            
            var sceneManager = GameApplication.Instance?.SceneManager;
            if (sceneManager == null)
            {
                throw new System.Exception("SceneManager不存在");
            }
            
            // 设置当前Stage
            StageManager.Instance.SetCurrentStage(gameStage);
            
            // 异步加载游戏场景
            sceneManager.LoadSceneAsync(gameSceneName, () =>
            {
                OnGameSceneLoaded(gameStage);
            });
        }
        
        /// <summary>
        /// 游戏场景加载完成回调
        /// </summary>
        private void OnGameSceneLoaded(Stage gameStage)
        {
            ASLogger.Instance.Info("GameLauncher: 游戏场景加载完成");
            
            // 激活Stage
            gameStage.SetActive(true);
            gameStage.OnEnter();
            
            ASLogger.Instance.Info("GameLauncher: 游戏准备完成");
        }
    }
} 