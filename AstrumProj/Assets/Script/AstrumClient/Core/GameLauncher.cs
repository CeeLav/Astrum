using UnityEngine;
using Astrum.Client.Core;
using Astrum.View.Core;
using Astrum.CommonBase;

namespace Astrum.Client.Core
{
    /// <summary>
    /// 游戏启动器 - 负责启动游戏，创建Room、Stage和Player
    /// </summary>
    public class GameLauncher
    {
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
                long roomId = CreateRoom();
                
                // 2. 创建Stage
                Stage gameStage = CreateStage(roomId);
                
                // 3. 创建Player并加入
                CreatePlayerAndJoin(gameStage);
                
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
        private long CreateRoom()
        {
            // 这里只是模拟创建Room，实际的Room逻辑在逻辑层
            long roomId = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
            ASLogger.Instance.Info($"GameLauncher: 创建Room，ID: {roomId}");
            return roomId;
        }
        
        /// <summary>
        /// 创建Stage
        /// </summary>
        private Stage CreateStage(long roomId)
        {
            ASLogger.Instance.Info("GameLauncher: 创建Stage");
            
            // 创建游戏Stage
            Stage gameStage = new Stage("GameStage", "游戏场景");
            
            // 初始化Stage
            gameStage.Initialize();
            
            // 设置Room ID
            gameStage.SetRoomId(roomId);
            
            return gameStage;
        }
        
        /// <summary>
        /// 创建Player并加入Stage
        /// </summary>
        private void CreatePlayerAndJoin(Stage gameStage)
        {
            ASLogger.Instance.Info("GameLauncher: 创建Player并加入Stage");
            
            // 创建Player (这里只是创建表现层的Player视图)
            long playerId = 1001; // 模拟Player ID
            Vector3 playerPosition = new Vector3(-5f, 0.5f, 0f);
            
            // 在Stage中创建Player视图
            gameStage.CreateUnitView(playerId, playerPosition, "player");
            
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
