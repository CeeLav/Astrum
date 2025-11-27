using System;
using System.IO;
using Astrum.CommonBase;
using Astrum.Client.Core;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using Astrum.View.Core;
using Astrum.View.Managers;
using AstrumClient.MonitorTools;
using Astrum.Client.Managers;
using Astrum.Client.UI.Generated;
using Astrum.Client.UI.Core;
using UnityEngine;

namespace Astrum.Client.Managers.GameModes
{
    /// <summary>
    /// 回放游戏模式 - 用于播放战斗回放
    /// </summary>
    [MonitorTarget]
    public class ReplayGameMode : BaseGameMode
    {
        private const int HubSceneId = 1; // 回放使用 Hub 场景
        private const int DungeonsGameSceneId = 2;
        
        // 核心属性
        public override Room MainRoom { get; set; }
        public override Stage MainStage { get; set; }
        public override long PlayerId { get; set; }
        public override string ModeName => "Replay";
        public override bool IsRunning { get; set; }
        
        // 回放相关
        private ReplayTimeline _timeline;
        private ReplayLSController _lsController;
        private string _replayFilePath;
        private bool _isPlaying = false;
        private int _currentFrame = 0;

        /// <summary>
        /// 当前进度（0~1）
        /// </summary>
        public float Progress => _timeline != null && _timeline.TotalFrames > 0 
            ? _currentFrame / (float)_timeline.TotalFrames 
            : 0f;

        /// <summary>
        /// 总时长（秒）
        /// </summary>
        public float DurationSeconds => _timeline != null 
            ? _timeline.TotalFrames / (float)_timeline.TickRate 
            : 0f;

        /// <summary>
        /// 当前帧
        /// </summary>
        public int CurrentFrame => _currentFrame;

        /// <summary>
        /// 总帧数
        /// </summary>
        public int TotalFrames => _timeline?.TotalFrames ?? 0;

        /// <summary>
        /// 当前时间（秒）
        /// </summary>
        public float CurrentTimeSeconds => _timeline != null && _timeline.TickRate > 0
            ? _currentFrame / (float)_timeline.TickRate
            : 0f;

        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 初始化回放游戏模式
        /// </summary>
        public override void Initialize()
        {
            ASLogger.Instance.Info("ReplayGameMode: 初始化回放游戏模式");
            ChangeState(GameModeState.Initializing);
            
            ChangeState(GameModeState.Ready);
            MonitorManager.Register(this);
        }

        /// <summary>
        /// 加载回放文件
        /// </summary>
        public bool Load(string filePath)
        {
            try
            {
                ASLogger.Instance.Info($"ReplayGameMode: 加载回放文件 - {filePath}");
                
                if (!File.Exists(filePath))
                {
                    ASLogger.Instance.Error($"ReplayGameMode: 回放文件不存在 - {filePath}");
                    return false;
                }

                _replayFilePath = filePath;
                
                // 1. 加载回放文件
                _timeline = new ReplayTimeline();
                if (!_timeline.LoadFromFile(filePath))
                {
                    ASLogger.Instance.Error("ReplayGameMode: 加载回放文件失败");
                    return false;
                }

                // 2. 获取起始快照数据
                var startSnapshot = _timeline.GetNearestSnapshot(0);
                if (startSnapshot == null)
                {
                    ASLogger.Instance.Error("ReplayGameMode: 未找到起始快照（第0帧）");
                    return false;
                }
                
                // 解压缩快照数据
                byte[] worldData = DecompressSnapshot(startSnapshot.WorldData);
                if (worldData == null || worldData.Length == 0)
                {
                    ASLogger.Instance.Error("ReplayGameMode: 快照数据为空");
                    return false;
                }
                
                // 3. 创建 Room 并直接使用快照初始化（这样角色会在初始化时创建）
                CreateRoom(worldData);
                
                // 4. 创建 Stage
                CreateStage();
                
                // 5. 切换到游戏场景
                SwitchToGameScene(DungeonsGameSceneId);
                
                ChangeState(GameModeState.Playing);
                
                IsRunning = true;
                _isPlaying = false; // 初始状态为暂停
                
                // 打开回放UI
                OpenReplayUI();
                
                ASLogger.Instance.Info($"ReplayGameMode: 回放文件加载成功 - 总帧数: {_timeline.TotalFrames}, 帧率: {_timeline.TickRate}");
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ReplayGameMode: 加载回放文件失败 - {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 启动游戏（回放模式不使用此方法，使用 Load()）
        /// </summary>
        public override void StartGame(int sceneId)
        {
            ASLogger.Instance.Warning("ReplayGameMode: 回放模式应使用 Load() 方法，而不是 StartGame()");
        }

        /// <summary>
        /// 更新回放逻辑
        /// </summary>
        public override void Update(float deltaTime)
        {
            if (!IsRunning || CurrentState != GameModeState.Playing) return;
            
            // 更新回放控制器（传入 deltaTime）
            
            if (_isPlaying && _lsController != null)
            {
                // 输入预加载已移至 ReplayLSController 内部处理
                
                // 更新回放（使用 deltaTime）
                //_lsController.Tick(deltaTime);
                _currentFrame = _lsController.AuthorityFrame;
            }
            
            // 更新 Room 和 Stage
            MainRoom?.Update(deltaTime);
            MainStage?.Update(deltaTime);
        }

        /// <summary>
        /// 关闭回放游戏模式
        /// </summary>
        public override void Shutdown()
        {
            ASLogger.Instance.Info("ReplayGameMode: 关闭回放游戏模式");
            ChangeState(GameModeState.Ending);
            
            // 关闭回放UI
            CloseReplayUI();
            
            // 清理资源
            MainStage?.Destroy();
            HUDManager.Instance?.ClearAll();
            MainRoom?.Shutdown();
            
            MainRoom = null;
            MainStage = null;
            _timeline = null;
            _lsController = null;
            PlayerId = -1;
            IsRunning = false;
            _isPlaying = false;
            _currentFrame = 0;
            
            ChangeState(GameModeState.Finished);
        }

        /// <summary>
        /// 播放回放
        /// </summary>
        public void Play()
        {
            if (_lsController != null)
            {
                _lsController.IsPaused = false;
                _isPlaying = true;
                ASLogger.Instance.Info("ReplayGameMode: 开始播放回放");
            }
        }

        /// <summary>
        /// 暂停回放
        /// </summary>
        public void Pause()
        {
            if (_lsController != null)
            {
                _lsController.IsPaused = true;
                _isPlaying = false;
                ASLogger.Instance.Info("ReplayGameMode: 暂停回放");
            }
        }

        /// <summary>
        /// 停止回放
        /// </summary>
        public void Stop()
        {
            Pause();
            Seek(0f); // 跳转到开始
        }

        /// <summary>
        /// 跳转到指定进度（0~1）
        /// </summary>
        public void Seek(float normalizedProgress)
        {
            if (_timeline == null || _lsController == null) return;
            
            int targetFrame = (int)(normalizedProgress * _timeline.TotalFrames);
            targetFrame = Mathf.Clamp(targetFrame, 0, _timeline.TotalFrames);
            
            SeekToFrame(targetFrame);
        }

        /// <summary>
        /// 跳转到指定帧
        /// </summary>
        public void SeekToFrame(int targetFrame)
        {
            if (_timeline == null || _lsController == null) return;
            
            targetFrame = Mathf.Clamp(targetFrame, 0, _timeline.TotalFrames);
            
            ASLogger.Instance.Info($"ReplayGameMode: 跳转到帧 {targetFrame}");
            
            // 使用 FastForwardTo 跳转
            _lsController.FastForwardTo(targetFrame);
            
            _currentFrame = _lsController.AuthorityFrame;
            
            // 重置视图，强制与逻辑层同步
            if (MainStage != null)
            {
                MainStage.ResetReplayViews();
            }
        }

        /// <summary>
        /// 打开回放UI
        /// </summary>
        private void OpenReplayUI()
        {
            try
            {
                var uiManager = UIManager.Instance;
                if (uiManager == null)
                {
                    ASLogger.Instance.Warning("ReplayGameMode: UIManager 未初始化，无法打开回放UI");
                    return;
                }

                // 显示UI（UI会自己通过GameDirector获取GameMode引用）
                uiManager.ShowUI("GameUI/ReplayUI");
                ASLogger.Instance.Info("ReplayGameMode: 回放UI已打开");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ReplayGameMode: 打开回放UI失败 - {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        /// <summary>
        /// 关闭回放UI
        /// </summary>
        private void CloseReplayUI()
        {
            try
            {
                var uiManager = UIManager.Instance;
                if (uiManager != null)
                {
                    uiManager.HideUI("GameUI/ReplayUI");
                    ASLogger.Instance.Info("ReplayGameMode: 回放UI已关闭");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ReplayGameMode: 关闭回放UI失败 - {ex.Message}");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
        }

        #region 私有方法

        /// <summary>
        /// 创建 Room 和 ReplayLSController（使用快照数据初始化）
        /// </summary>
        private void CreateRoom(byte[] worldSnapshot)
        {
            // 创建 Room（使用回放时间线的房间ID）
            int roomId = Math.Abs(_timeline.RoomId.GetHashCode());
            var room = new Room(roomId, _timeline.RoomId);
            
            // 初始化 Room（使用 "replay" 模式，并传入快照数据，这样角色会在初始化时创建）
            room.Initialize("replay", worldSnapshot);
            
            // 获取 Room 创建的 ReplayLSController 并设置回放相关参数
            _lsController = room.LSController as ReplayLSController;
            if (_lsController != null)
            {
                _lsController.TickRate = _timeline.TickRate;
                _lsController.CreationTime = _timeline.StartTimestamp;
                _lsController.AuthorityFrame = 0;
                
                // 设置输入提供者
                _lsController.InputProvider = (frame) => _timeline.GetFrameInputs(frame);
                
                // 设置最近快照提供者
                _lsController.NearestSnapshotProvider = (frame) =>
                {
                    var snapshot = _timeline.GetNearestSnapshot(frame);
                    if (snapshot != null)
                    {
                        var data = _timeline.GetSnapshotWorldData(snapshot.Frame);
                        return (snapshot.Frame, data);
                    }
                    return (-1, null);
                };
                _lsController.Start();
            }
            else
            {
                ASLogger.Instance.Error("ReplayGameMode: Room.Initialize 未能创建 ReplayLSController");
            }
            
            MainRoom = room;
            _currentFrame = 0;
            
            ASLogger.Instance.Info($"ReplayGameMode: 创建 Room 和 ReplayLSController - 房间ID: {_timeline.RoomId}, 帧率: {_timeline.TickRate}, 实体数: {room.MainWorld?.Entities?.Count ?? 0}");
        }

        /// <summary>
        /// 创建 Stage
        /// </summary>
        private void CreateStage()
        {
            if (MainRoom == null) return;
            
            var stage = new Stage("ReplayStage", "回放场景");
            stage.Initialize();
            stage.SetRoom(MainRoom);
            
            MainStage = stage;
            stage.SetActive(true);
            
            View.Managers.VFXManager.Instance.CurrentStage = stage;
            MainStage.SyncEntityViews();
            ASLogger.Instance.Info("ReplayGameMode: 创建 Stage");
        }

        /// <summary>
        /// 切换到游戏场景
        /// </summary>
        private void SwitchToGameScene(int sceneId)
        {
            var sceneManager = SceneManager.Instance;
            if (sceneManager != null)
            {
                sceneManager.LoadSceneAsync(sceneId, () =>
                {
                    ASLogger.Instance.Info($"ReplayGameMode: 场景加载完成 - 场景ID: {sceneId}");
                });
            }
        }

        /// <summary>
        /// 解压缩快照数据（GZip）
        /// </summary>
        private byte[] DecompressSnapshot(byte[] compressedData)
        {
            try
            {
                using (var output = new MemoryStream())
                {
                    using (var input = new MemoryStream(compressedData))
                    using (var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
                    {
                        gzip.CopyTo(output);
                    }
                    return output.ToArray();
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"ReplayGameMode: 解压缩快照数据失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        protected override GameModeConfig CreateDefaultConfig()
        {
            return new GameModeConfig
            {
                ModeName = "Replay",
                AutoSave = false,
                UpdateInterval = 0.016f,
                CustomSettings = new System.Collections.Generic.Dictionary<string, object>()
            };
        }

        #endregion
    }
}

