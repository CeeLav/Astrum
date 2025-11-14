using UnityEngine;
using Astrum.View.Core;
using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Core;
using Astrum.CommonBase;
using Astrum.LogicCore.Managers;
using System.Collections.Generic;
using Astrum.View.Managers;

namespace Astrum.View.Components
{
    /// <summary>
    /// 动画视图组件 - 使用Animancer管理实体动画播放
    /// </summary>
    public class AnimationViewComponent : ViewComponent
    {
        // Animancer相关组件
        private Animancer.AnimancerComponent _animancerComponent;
        private Animancer.AnimancerState _currentAnimationState;
        
        // Animator引用（用于视觉跟随）
        private Animator _animator;
        
        // 动画配置
        private string _currentAnimationName = "";
        private bool _isPlaying = false;
        private float _animationSpeed = 1.0f;
        private bool _loopAnimation = false;
        
        // 动作系统相关

        // 动画状态
        private int _lastActionId = -1;
        private int _lastActionFrame = -1;
        
        // 同步阈值配置
        private const float FRAME_SYNC_THRESHOLD = 1.0f; // 帧数差异阈值，超过此值需要同步
        private const float NORMALIZED_TIME_SYNC_THRESHOLD = 0.1f; // 标准化时间差异阈值
        
        // 帧同步配置（使用LSConstValue）
        private const int FRAME_UPDATE_INTERVAL_MS = LSConstValue.UpdateInterval; // 帧更新间隔（毫秒）
        private const float FRAME_UPDATE_INTERVAL_SECONDS = FRAME_UPDATE_INTERVAL_MS / 1000.0f; // 帧更新间隔（秒）
        private const int FRAME_COUNT_PER_SECOND = LSConstValue.FrameCountPerSecond; // 每秒帧数
        
        /// <summary>
        /// 当前播放的动画名称
        /// </summary>
        public string CurrentAnimationName => _currentAnimationName;
        
        /// <summary>
        /// 是否正在播放动画
        /// </summary>
        public bool IsPlaying => _isPlaying;
        
        /// <summary>
        /// 动画播放速度
        /// </summary>
        public float AnimationSpeed
        {
            get => _animationSpeed;
            set => _animationSpeed = value;
        }
        
        protected override void OnInitialize()
        {
            
            // 初始化Animancer组件
            InitializeAnimancer();
            
            // 设置初始动画
            //SetInitialAnimation();
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isEnabled) return;
            
            // 同步动作系统状态
            SyncWithActionSystem();
            
            // 更新动画状态
            UpdateAnimationState(deltaTime);
        }
        
        protected override void OnDestroy()
        {
            ASLogger.Instance.Debug($"AnimationViewComponent.OnDestroy: 销毁动画组件，EntityId={OwnerEntity?.UniqueId}");
            
            // 停止当前动画
            StopCurrentAnimation();
            
            // 清理Animancer组件
            CleanupAnimancer();
        }
        
        protected override void OnSyncData(object data)
        {
            // TODO: 实现数据同步逻辑
            // 当动作系统数据更新时，同步到动画组件
        }
        
        /// <summary>
        /// 初始化Animancer组件
        /// </summary>
        private void InitializeAnimancer()
        {
            if (OwnerEntity == null)
            {
                ASLogger.Instance.Error($"AnimationViewComponent.InitializeAnimancer: OwnerEntity is null on entity {OwnerEntity?.UniqueId}");
                return;
            }

            var modelComp = OwnerEntityView.GetViewComponent<ModelViewComponent>();
            var model = modelComp?.ModelObject;
            
            if (model == null)
            {
                ASLogger.Instance.Error($"AnimationViewComponent.InitializeAnimancer: ModelViewComponent or ModelObject is null on entity {OwnerEntity?.UniqueId}");
                return;
            }

            _animator = model.GetComponent<Animator>();
            if (_animator == null)
            {
                ASLogger.Instance.Error($"AnimationViewComponent.InitializeAnimancer: Animator is null on entity {OwnerEntity?.UniqueId}");
            }
            else
            {
                // 确保 applyRootMotion = true（逻辑层控制位移，视觉层只采样动画感）
                _animator.applyRootMotion = true;
                ASLogger.Instance.Debug($"[AnimationViewComponent] Set applyRootMotion=true for entity {OwnerEntity?.UniqueId}");
            }
            
            // 1. 检查GameObject上是否有AnimancerComponent
            _animancerComponent = model.GetComponent<Animancer.AnimancerComponent>();
            
            if (_animancerComponent == null)
            {
                // 2. 如果没有则添加AnimancerComponent
                _animancerComponent = model.AddComponent<Animancer.AnimancerComponent>();
                ASLogger.Instance.Debug($"AnimationViewComponent.InitializeAnimancer: Added AnimancerComponent to entity {OwnerEntity?.UniqueId}");
            }
            else
            {
                ASLogger.Instance.Debug($"AnimationViewComponent.InitializeAnimancer: Found existing AnimancerComponent on entity {OwnerEntity?.UniqueId}");
            }

            // 3. 配置Animancer设置
            if (_animancerComponent != null)
            {
                // 确保AnimancerComponent已初始化
                if (!_animancerComponent.IsGraphInitialized)
                {
                    _animancerComponent.InitializeGraph();
                    ASLogger.Instance.Debug($"AnimationViewComponent.InitializeAnimancer: Initialized AnimancerGraph for entity {OwnerEntity?.UniqueId}");
                }

                // 检查是否有Animator组件
                if (_animancerComponent.Animator == null)
                {
                    ASLogger.Instance.Warning($"AnimationViewComponent.InitializeAnimancer: No Animator found on entity {OwnerEntity?.UniqueId}. " +
                        "Animancer requires an Animator component to work properly.");
                }
                else
                {
                    ASLogger.Instance.Debug($"AnimationViewComponent.InitializeAnimancer: Animancer setup complete for entity {OwnerEntity?.UniqueId} " +
                        $"with Animator: {_animancerComponent.Animator.name}");
                }

                // 4. 预加载动画资源
                PreloadAnimations();
            }
            else
            {
                ASLogger.Instance.Error($"AnimationViewComponent.InitializeAnimancer: Failed to create AnimancerComponent for entity {OwnerEntity?.UniqueId}");
            }
        }

        /// <summary>
        /// 预加载动画资源到Animancer Graph
        /// </summary>
        private void PreloadAnimations()
        {
            if (_animancerComponent?.Graph == null)
            {
                ASLogger.Instance.Error("AnimationViewComponent.PreloadAnimations: AnimancerComponent or Graph is null");
                return;
            }

            // 获取ActionComponent的可用动作ID
            var actionComponent = OwnerEntity?.GetComponent<ActionComponent>();
            if (actionComponent == null)
            {
                ASLogger.Instance.Warning($"AnimationViewComponent.PreloadAnimations: ActionComponent not found for entity {OwnerEntity?.UniqueId}");
                return;
            }

            // 从ActionComponent的AvailableActions中获取动画路径
            var animationNames = GetAnimationNamesFromActionComponent(actionComponent);
            
            foreach (var animationName in animationNames)
            {
                try
                {
                    // 尝试通过ResourceManager加载动画
                    var animationClip = ResourceManager.Instance.LoadResource<AnimationClip>(animationName);
                    
                    if (animationClip != null)
                    {
                        // 在Animancer Graph中注册动画，使用完整路径作为状态名称
                        _animancerComponent.States.Create(animationName, animationClip);
                        ASLogger.Instance.Debug($"AnimationViewComponent.PreloadAnimations: Loaded animation '{animationName}' for entity {OwnerEntity?.UniqueId}");
                    }
                    else
                    {
                        ASLogger.Instance.Warning($"AnimationViewComponent.PreloadAnimations: Animation clip not found for '{animationName}' at path '{animationName}'");
                    }
                }
                catch (System.Exception ex)
                {
                    ASLogger.Instance.Error($"AnimationViewComponent.PreloadAnimations: Failed to load animation '{animationName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 从ActionComponent获取动画路径
        /// </summary>
        /// <param name="actionComponent">动作组件</param>
        /// <returns>动画路径列表</returns>
        private List<string> GetAnimationNamesFromActionComponent(ActionComponent actionComponent)
        {
            var animationNames = new List<string>();
            
            if (actionComponent?.AvailableActions == null)
            {
                ASLogger.Instance.Warning("AnimationViewComponent.GetAnimationNamesFromActionComponent: ActionComponent.AvailableActions is null");
                return animationNames;
            }

            try
            {
                // 从ActionComponent的AvailableActions字典中收集动画名称
                foreach (var actionInfo in actionComponent.AvailableActions.Values)
                {
                    if (actionInfo?.Id > 0)
                    {
                        string animationName = GetAnimationNameByActionId(actionInfo.Id);
                        if (!string.IsNullOrEmpty(animationName) && !animationNames.Contains(animationName))
                        {
                            animationNames.Add(animationName);
                        }
                    }
                }
                
                ASLogger.Instance.Debug($"AnimationViewComponent.GetAnimationNamesFromActionComponent: Found {animationNames.Count} unique animation paths for entity {OwnerEntity?.UniqueId}");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"AnimationViewComponent.GetAnimationNamesFromActionComponent: Exception while collecting animation names: {ex.Message}");
            }

            return animationNames;
        }
        
        /// <summary>
        /// 初始化动作系统相关组件
        /// </summary>
        private void InitializeActionSystemComponents()
        {
            // TODO: 实现动作系统组件获取
            // 1. 从OwnerEntity获取ActionComponent
            // 2. 从OwnerEntity获取ActionCapability
            // 3. 验证组件是否有效
        }
        
        /// <summary>
        /// 设置初始动画
        /// </summary>
        private void SetInitialAnimation()
        {
            // TODO: 实现初始动画设置
            // 1. 根据实体配置获取默认动画
            // 2. 播放默认动画（如Idle动画）
        }
        
        /// <summary>
        /// 同步动作系统状态
        /// </summary>
        private void SyncWithActionSystem()
        {
            if (_animancerComponent == null || OwnerEntity == null) return;
            
            // 获取动作组件
            var actionComponent = OwnerEntity.GetComponent<ActionComponent>();
            if (actionComponent == null) return;
            
            var currentAction = actionComponent.CurrentAction;
            var currentFrame = actionComponent.CurrentFrame;
            
            // 1. 检查当前动作是否发生变化
            if (currentAction == null)
            {
                // 动作为空，停止动画
                if (_isPlaying)
                {
                    StopCurrentAnimation();
                    ASLogger.Instance.Debug($"AnimationViewComponent.SyncWithActionSystem: Stopped animation due to null action on entity {OwnerEntity.UniqueId}");
                }
                ResetAnimationSpeed();
                return;
            }
            
            // 2. 检查动作ID是否发生变化
            if (_lastActionId != currentAction.Id)
            {
                // 动作切换，播放新动画
                PlayAnimationByActionId(currentAction.Id, 0.25f);
                _lastActionId = currentAction.Id;
                _lastActionFrame = currentFrame;
                SyncAnimationTime(currentAction, currentFrame);
                ApplyAnimationSpeed(currentAction);
                
                ASLogger.Instance.Debug($"AnimationViewComponent.SyncWithActionSystem: Action changed to {currentAction.Id} on entity {OwnerEntity.UniqueId}");
                return;
            }
            else
            {
                if (currentAction.KeepPlayingAnim)
                {
                    _lastActionFrame = currentFrame;
                    ApplyAnimationSpeed(currentAction);
                    return;
                }
                
                bool restarted = currentFrame < _lastActionFrame || (currentFrame == 0 && _lastActionFrame > 0);
                if (restarted)
                {
                    PlayAnimationByActionId(currentAction.Id, 0.1f, true);
                    _lastActionFrame = currentFrame;
                    SyncAnimationTime(currentAction, currentFrame);
                    ASLogger.Instance.Debug($"AnimationViewComponent.SyncWithActionSystem: Restarted action {currentAction.Id} on entity {OwnerEntity.UniqueId} (frame reset)");
                    ApplyAnimationSpeed(currentAction);
                    return;
                }
                _lastActionFrame = currentFrame;
            }

            ApplyAnimationSpeed(currentAction);
            /*
            // 3. 检查动作帧数是否发生显著变化
            int frameDifference = Mathf.Abs(currentFrame - _lastActionFrame);
            if (frameDifference > FRAME_SYNC_THRESHOLD)
            {
                // 帧数差异过大，需要同步动画时间
                SyncAnimationTime(currentAction, currentFrame);
                _lastActionFrame = currentFrame;
                ASLogger.Instance.Warning($"AnimationViewComponent.SyncWithActionSystem: Synced animation time due to large frame difference ({frameDifference}) on entity {OwnerEntity.UniqueId}");
            }
            else if (frameDifference > 0)
            {
                // 帧数差异较小，让动画继续播放
                _lastActionFrame = currentFrame;
            }*/
        }
        
        /// <summary>
        /// 更新动画状态
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateAnimationState(float deltaTime)
        {
            if (_animancerComponent == null || _currentAnimationState == null) return;
            
            // 1. 更新动画播放进度
            UpdateAnimationProgress(deltaTime);
            
            // 2. 处理动画结束事件
            CheckAnimationEnd();
            
            // 3. 处理循环动画
            HandleLoopingAnimation();
        }
        
        /// <summary>
        /// 播放指定动画
        /// </summary>
        /// <param name="animationName">动画名称</param>
        /// <param name="fadeTime">过渡时间</param>
        /// <param name="speed">播放速度</param>
        /// <param name="loop">是否循环</param>
        public void PlayAnimation(string animationName, float fadeTime = 0.25f, float speed = 1.0f, bool loop = false)
        {
            // TODO: 实现动画播放逻辑
            // 1. 验证动画名称有效性
            // 2. 停止当前动画
            // 3. 播放新动画
            // 4. 设置动画参数
        }
        
        /// <summary>
        /// 停止当前动画
        /// </summary>
        public void StopCurrentAnimation()
        {
            if (_currentAnimationState != null)
            {
                // 停止当前动画
                _currentAnimationState.Stop();
                
                ASLogger.Instance.Debug($"AnimationViewComponent.StopCurrentAnimation: Stopped animation '{_currentAnimationName}' for entity {OwnerEntity?.UniqueId}");
            }
            
            // 重置动画状态
            _currentAnimationState = null;
            _currentAnimationName = "";
            _isPlaying = false;
        }
        
        /// <summary>
        /// 暂停动画播放
        /// </summary>
        public void PauseAnimation()
        {
            // TODO: 实现暂停动画逻辑
        }
        
        /// <summary>
        /// 恢复动画播放
        /// </summary>
        public void ResumeAnimation()
        {
            // TODO: 实现恢复动画逻辑
        }
        
        /// <summary>
        /// 设置动画播放速度
        /// </summary>
        /// <param name="speed">播放速度</param>
        public void SetAnimationSpeed(float speed)
        {
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0f)
            {
                speed = 1f;
            }

            _animationSpeed = speed;
            if (_currentAnimationState != null)
            {
                _currentAnimationState.Speed = speed;
            }
        }

        /// <summary>
        /// 重置动画播放速度为1
        /// </summary>
        public void ResetAnimationSpeed()
        {
            SetAnimationSpeed(1f);
        }

        private void ApplyAnimationSpeed(ActionInfo actionInfo)
        {
            if (actionInfo == null)
            {
                ResetAnimationSpeed();
                return;
            }

            SetAnimationSpeed(actionInfo.AnimationSpeedMultiplier);
        }
        
        /// <summary>
        /// 设置动画循环
        /// </summary>
        /// <param name="loop">是否循环</param>
        public void SetAnimationLoop(bool loop)
        {
            // TODO: 实现设置动画循环逻辑
        }
        
        /// <summary>
        /// 根据动作ID播放对应动画
        /// </summary>
        /// <param name="actionId">动作ID</param>
        /// <param name="fadeTime">过渡时间</param>
        public void PlayAnimationByActionId(int actionId, float fadeTime = 0.25f, bool forceRestart = false)
        {
            if (_animancerComponent == null)
            {
                ASLogger.Instance.Error($"AnimationViewComponent.PlayAnimationByActionId: AnimancerComponent is null for entity {OwnerEntity?.UniqueId}");
                return;
            }

            // 1. 根据动作ID获取动画名称
            string animationName = GetAnimationNameByActionId(actionId);
            
            // 2. 检查动画是否已经在播放
            bool isSameAnimationPlaying = _currentAnimationName == animationName && _isPlaying;
            if (isSameAnimationPlaying && !forceRestart)
            {
                ASLogger.Instance.Debug($"AnimationViewComponent.PlayAnimationByActionId: Animation {animationName} is already playing for entity {OwnerEntity?.UniqueId}");
                return;
            }
            bool restartRequired = forceRestart;

            // 3. 停止当前动画
            //StopCurrentAnimation();

            // 4. 播放新动画
            try
            {
                // 使用Animancer通过字符串名称播放动画，支持淡入淡出过渡
                _currentAnimationState = _animancerComponent.TryPlay(animationName, fadeTime);
                
                if (_currentAnimationState != null)
                {
                    if (restartRequired)
                    {
                        _currentAnimationState.Time = 0f;
                        _currentAnimationState.Play();
                    }
                    
                    // 设置动画属性
                    _currentAnimationState.Speed = _animationSpeed;
                    // 注意：IsLooping 是只读属性，需要在创建动画状态时设置
                    // 这里我们通过其他方式处理循环逻辑
                    
                    // 更新状态
                    _currentAnimationName = animationName;
                    _isPlaying = true;
                    
                    ASLogger.Instance.Debug($"AnimationViewComponent.PlayAnimationByActionId: { (restartRequired ? "Restarted" : "Started") } animation '{animationName}' " +
                        $"(actionId: {actionId}, fadeTime: {fadeTime:F2}s, forceRestart: {forceRestart}) for entity {OwnerEntity?.UniqueId}");
                }
                else
                {
                    ASLogger.Instance.Warning($"AnimationViewComponent.PlayAnimationByActionId: Failed to play animation '{animationName}' " +
                        $"(actionId: {actionId}) for entity {OwnerEntity?.UniqueId} - Animation not found");
                }
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"AnimationViewComponent.PlayAnimationByActionId: Exception while playing animation '{animationName}' " +
                    $"(actionId: {actionId}) for entity {OwnerEntity?.UniqueId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取动画播放进度
        /// </summary>
        /// <returns>播放进度 (0-1)</returns>
        public float GetAnimationProgress()
        {
            // TODO: 实现获取动画进度逻辑
            return 0.0f;
        }
        
        /// <summary>
        /// 检查动画是否播放完成
        /// </summary>
        /// <returns>是否播放完成</returns>
        public bool IsAnimationFinished()
        {
            // TODO: 实现检查动画完成逻辑
            return false;
        }
        
        /// <summary>
        /// 清理Animancer组件
        /// </summary>
        private void CleanupAnimancer()
        {
            // TODO: 实现Animancer组件清理
            // 1. 停止所有动画
            // 2. 清理动画状态
            // 3. 移除AnimancerComponent（如果需要）
        }
        
        /// <summary>
        /// 获取组件状态信息
        /// </summary>
        /// <returns>状态信息</returns>
        public override string GetComponentStatus()
        {
            return $"AnimationViewComponent - ID: {_componentId}, 启用: {_isEnabled}, " +
                   $"当前动画: {_currentAnimationName}, 播放中: {_isPlaying}, " +
                   $"速度: {_animationSpeed}, 循环: {_loopAnimation}";
        }
        
        /// <summary>
        /// 同步动画时间到指定帧
        /// </summary>
        /// <param name="actionInfo">动作信息</param>
        /// <param name="targetFrame">目标帧数</param>
        private void SyncAnimationTime(ActionInfo actionInfo, int targetFrame)
        {
            if (_currentAnimationState == null || actionInfo == null) return;
            
            // 计算插值后的绝对动画时间（秒）
            float animTime = CalculateActionToAnimTime(targetFrame, actionInfo.Duration);
            
            // 直接设置绝对时间（秒）- Animancer 支持直接使用绝对时间
            _currentAnimationState.Time = animTime;
            
            ASLogger.Instance.Debug($"AnimationViewComponent.SyncAnimationTime: Synced to frame {targetFrame} " +
                $"(animTime: {animTime:F3}s) for action {actionInfo.Id} on entity {OwnerEntity?.UniqueId}");
        }
        
        
        /// <summary>
        /// 根据动作ID获取动画路径
        /// </summary>
        /// <param name="actionId">动作ID</param>
        /// <returns>动画资源路径</returns>
        private string GetAnimationNameByActionId(int actionId)
        {
            // 检查ConfigManager是否已初始化
            if (TableConfig.Instance == null)
            {
                ASLogger.Instance.Error($"AnimationViewComponent.GetAnimationNameByActionId: ConfigManager is not initialized for actionId {actionId}");
                return $"Action_{actionId}"; // 返回默认格式
            }

            try
            {
                // 通过ConfigManager获取ActionTable数据
                var actionTable = TableConfig.Instance.Tables.TbActionTable.Get(actionId);
                
                if (actionTable == null)
                {
                    ASLogger.Instance.Warning($"AnimationViewComponent.GetAnimationNameByActionId: ActionTable not found for actionId {actionId}");
                    return $"Action_{actionId}"; // 返回默认格式
                }

                // 检查AnimationName是否为空
                if (string.IsNullOrEmpty(actionTable.AnimationName))
                {
                    ASLogger.Instance.Warning($"AnimationViewComponent.GetAnimationNameByActionId: AnimationName is empty for actionId {actionId}, using default format");
                    return $"Action_{actionId}"; // 返回默认格式
                }

                ASLogger.Instance.Debug($"AnimationViewComponent.GetAnimationNameByActionId: Found animation path '{actionTable.AnimationName}' for actionId {actionId}");
                return actionTable.AnimationName;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"AnimationViewComponent.GetAnimationNameByActionId: Exception while getting animation name for actionId {actionId}: {ex.Message}");
                return $"Action_{actionId}"; // 返回默认格式
            }
        }
        
        /// <summary>
        /// 更新动画播放进度
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateAnimationProgress(float deltaTime)
        {
            if (_currentAnimationState == null) return;
            
            // 更新动画播放进度（Animancer会自动处理）
            // 这里可以添加额外的进度跟踪逻辑
        }
        
        /// <summary>
        /// 检查动画是否结束
        /// </summary>
        private void CheckAnimationEnd()
        {
            if (_currentAnimationState == null) return;
            
            // 检查动画是否播放完成（NormalizedTime >= 1.0表示动画结束）
            if (_currentAnimationState.NormalizedTime >= 1.0f)
            {
                //ASLogger.Instance.Info($"AnimationViewComponent.CheckAnimationEnd: Animation {_currentAnimationName} finished on entity {OwnerEntity?.UniqueId}");
                
                // 处理动画结束逻辑
                OnAnimationFinished();
            }
        }
        
        /// <summary>
        /// 处理循环动画
        /// </summary>
        private void HandleLoopingAnimation()
        {
            if (_currentAnimationState == null || !_loopAnimation) return;
            
            // 如果动画结束且需要循环，重新开始播放
            if (_currentAnimationState.NormalizedTime >= 1.0f)
            {
                _currentAnimationState.NormalizedTime = 0f;
                ASLogger.Instance.Debug($"AnimationViewComponent.HandleLoopingAnimation: Looped animation {_currentAnimationName} on entity {OwnerEntity?.UniqueId}");
            }
        }
        
        /// <summary>
        /// 动画结束回调
        /// </summary>
        private void OnAnimationFinished()
        {
            // 可以在这里添加动画结束后的处理逻辑
            // 例如：播放下一个动画、触发事件等
        }
        
        /// <summary>
        /// 计算插值后的标准化时间
        /// 根据当前物理时间和预测帧时间的偏移进行插值
        /// </summary>
        /// <param name="actionFrame">动作帧数</param>
        /// <param name="totalFrames">总帧数</param>
        /// <returns>插值后的标准化时间 (0-1)</returns>
        private float CalculateActionToAnimTime(int actionFrame, int totalFrames)
        {
            if (totalFrames <= 0) return 0f;
            
            // 获取当前物理时间
            long currentTime = TimeInfo.Instance.ServerNow();
            
            // 获取帧同步的当前帧（PredictionFrame）
            int currentPredictionFrame = GetCurrentPredictionFrame();
            
            // 计算当前预测帧对应的时间
            long currentFrameTime = CalculatePredictionFrameTime(currentPredictionFrame);
            
            // 计算当前物理时间和预测帧时间的偏移（毫秒）
            long timeOffset = currentTime - currentFrameTime;
            
            // 将时间偏移转换为秒，然后计算动画时间
            float animTime = (actionFrame * FRAME_UPDATE_INTERVAL_MS + timeOffset) / 1000.0f;
            
            return animTime;
        }
        
        /// <summary>
        /// 获取当前帧同步的PredictionFrame
        /// </summary>
        /// <returns>当前PredictionFrame</returns>
        private int GetCurrentPredictionFrame()
        {
            if (_ownerEntityView?.Stage?.Room?.LSController is ClientLSController clientSync)
            {
                return clientSync.PredictionFrame;
            }
            
            // 如果无法获取，返回0作为fallback
            ASLogger.Instance.Warning($"AnimationViewComponent.GetCurrentPredictionFrame: Cannot get current prediction frame for entity {OwnerEntity?.UniqueId}, using 0 as fallback");
            return 0;
        }
        
        /// <summary>
        /// 计算指定帧同步帧对应的时间
        /// </summary>
        /// <param name="predictionFrame">帧同步帧数</param>
        /// <returns>帧同步时间（毫秒）</returns>
        private long CalculatePredictionFrameTime(int predictionFrame)
        {
            if (_ownerEntityView?.Stage?.Room?.LSController is ClientLSController clientSync)
            {
                // 使用 ClientLSController 的方法获取指定帧同步时间
                return clientSync.GetPredictionFrameTime(predictionFrame);
            }
            
            // 如果无法获取 ClientLSController，使用fallback方法
            long baseTime = GetFrameSyncBaseTime();
            return baseTime + (predictionFrame * FRAME_UPDATE_INTERVAL_MS);
        }
        
        /// <summary>
        /// 计算指定帧对应的帧同步时间（保留原方法用于兼容）
        /// </summary>
        /// <param name="frame">帧数</param>
        /// <returns>帧同步时间（毫秒）</returns>
        private long CalculateFrameTime(int frame)
        {
            // 尝试从实体获取帧同步基准时间
            long baseTime = GetFrameSyncBaseTime();
            return baseTime + (frame * FRAME_UPDATE_INTERVAL_MS);
        }
        
        /// <summary>
        /// 获取帧同步基准时间
        /// </summary>
        /// <returns>帧同步基准时间（毫秒）</returns>
        private long GetFrameSyncBaseTime()
        {
            if (_ownerEntityView?.Stage?.Room?.LSController != null)
            {
                // 使用房间的帧同步控制器的创建时间作为基准（基础接口）
                return _ownerEntityView.Stage.Room.LSController.CreationTime;
            }
            
            // 如果无法获取帧同步基准时间，使用当前时间作为参考
            // 这种情况下插值可能不够准确，但至少不会出错
            ASLogger.Instance.Warning($"AnimationViewComponent.GetFrameSyncBaseTime: Cannot get frame sync base time for entity {OwnerEntity?.UniqueId}, using current time as fallback");
            long fallbackTime = TimeInfo.Instance.ServerNow() - LSConstValue.UpdateInterval;
            return fallbackTime < 0 ? 0 : fallbackTime;
        }
        
        /// <summary>
        /// 计算插值因子
        /// </summary>
        /// <param name="currentTime">当前时间</param>
        /// <param name="frameStartTime">帧开始时间</param>
        /// <param name="frameEndTime">帧结束时间</param>
        /// <returns>插值因子 (0-1)</returns>
        private float CalculateInterpolationFactor(long currentTime, long frameStartTime, long frameEndTime)
        {
            if (currentTime <= frameStartTime) return 0f;
            if (currentTime >= frameEndTime) return 1f;
            
            long frameDuration = frameEndTime - frameStartTime;
            if (frameDuration <= 0) return 0f;
            
            long elapsed = currentTime - frameStartTime;
            return (float)elapsed / frameDuration;
        }
        
        /// <summary>
        /// 将帧数转换为标准化时间（不使用插值）
        /// </summary>
        /// <param name="frame">帧数</param>
        /// <param name="totalFrames">总帧数</param>
        /// <returns>标准化时间 (0-1)</returns>
        private float CalculateNormalizedTimeFromFrame(int frame, int totalFrames)
        {
            if (totalFrames <= 0) return 0f;
            
            float normalizedTime = (float)frame / totalFrames;
            return Mathf.Clamp01(normalizedTime);
        }
        
        /// <summary>
        /// 将标准化时间转换为帧数
        /// </summary>
        /// <param name="normalizedTime">标准化时间 (0-1)</param>
        /// <param name="totalFrames">总帧数</param>
        /// <returns>帧数</returns>
        private int CalculateFrameFromNormalizedTime(float normalizedTime, int totalFrames)
        {
            return Mathf.RoundToInt(normalizedTime * totalFrames);
        }
        
        /// <summary>
        /// 获取 Animator 引用（供 TransViewComponent 使用）
        /// </summary>
        /// <returns>Animator 组件引用，如果不存在则返回 null</returns>
        public Animator GetAnimator()
        {
            if (_animator == null)
            {
                // 尝试重新获取（防止初始化顺序问题）
                if (_ownerEntityView?.GameObject != null)
                {
                    var modelComp = _ownerEntityView.GetViewComponent<ModelViewComponent>();
                    var model = modelComp?.ModelObject;
                    if (model != null)
                    {
                        _animator = model.GetComponent<Animator>();
                        if (_animator != null)
                        {
                            _animator.applyRootMotion = true;
                            ASLogger.Instance.Debug($"[AnimationViewComponent] Re-acquired Animator for entity {OwnerEntity?.UniqueId}");
                        }
                    }
                }
            }
            
            return _animator;
        }
    }
}
