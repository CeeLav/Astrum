using UnityEngine;
using UnityEditor;
using Animancer;
using Astrum.Editor.RoleEditor.Services;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 动画预览模块 - 支持逐帧控制和时间轴同步
    /// </summary>
    public class AnimationPreviewModule : BasePreviewModule
    {
        // === 帧率常量 ===
        private const float LOGIC_FRAME_RATE = 20f;     // 游戏逻辑帧率：20fps = 50ms/帧
        private const float FRAME_TIME = 0.05f;          // 每帧时间：50ms = 0.05秒
        
        // === 当前动画 ===
        private AnimationClip _currentClip;
        private string _currentAnimationPath;
        private int _currentEntityId = 0;
        
        // === 播放状态（特有） ===
        private int _currentFrame = 0;
        private float _accumulatedTime = 0f;             // 累积时间，用于逐帧播放
        
        protected override string LogPrefix => "[AnimationPreviewModule]";
        
        // === 实体管理 ===
        
        /// <summary>
        /// 设置预览的实体（加载模型）
        /// </summary>
        public void SetEntity(int entityId)
        {
            if (_currentEntityId == entityId) return;
            
            _currentEntityId = entityId;
            
            // 从EntityBaseTable获取模型路径
            var entityData = ConfigTableHelper.GetEntityById(entityId);
            if (entityData == null || string.IsNullOrEmpty(entityData.ModelPath))
            {
                Debug.LogWarning($"{LogPrefix} No model path for entity {entityId}");
                CleanupPreviewInstance();
                return;
            }
            
            // 加载模型（使用基类方法）
            LoadModel(entityData.ModelPath);
        }
        
        // === 动画加载 ===
        
        /// <summary>
        /// 加载动画片段
        /// </summary>
        public void LoadAnimation(AnimationClip clip, string animationPath = "")
        {
            _currentClip = clip;
            _currentAnimationPath = animationPath;
            
            if (clip == null)
            {
                Debug.LogWarning($"{LogPrefix} Animation clip is null");
                return;
            }
            
            if (_previewInstance == null || _animancer == null)
            {
                Debug.LogWarning($"{LogPrefix} No model loaded, please select an entity first");
                return;
            }
            
            // 播放动画
            _currentAnimState = _animancer.Play(clip);
            _currentAnimState.Speed = 0; // 默认暂停
            _isPlaying = false;
            
            Debug.Log($"{LogPrefix} Loaded animation: {clip.name}");
        }
        
        /// <summary>
        /// 从路径加载动画
        /// </summary>
        public void LoadAnimationFromPath(string animationPath)
        {
            if (string.IsNullOrEmpty(animationPath))
            {
                Debug.LogWarning($"{LogPrefix} Animation path is empty");
                return;
            }
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationPath);
            
            if (clip == null)
            {
                Debug.LogWarning($"{LogPrefix} Failed to load animation: {animationPath}");
                return;
            }
            
            LoadAnimation(clip, animationPath);
        }
        
        // === 播放控制（重写基类方法，添加帧重置）===
        
        public override void Play()
        {
            if (_currentAnimState != null)
            {
                _isPlaying = true;
                _currentAnimState.Speed = 0; // 保持Speed=0，完全由我们手动控制Time
                _lastUpdateTime = UnityEditor.EditorApplication.timeSinceStartup;
                _accumulatedTime = 0f; // 重置累积时间，防止跳帧
                Debug.Log($"{LogPrefix} Playing (manual frame control)");
            }
        }
        
        public override void Stop()
        {
            base.Stop();
            _currentFrame = 0;
            _accumulatedTime = 0f;
        }
        
        /// <summary>
        /// 跳转到指定帧
        /// </summary>
        public void SetFrame(int frame)
        {
            if (_currentAnimState == null || _currentClip == null)
                return;
            
            _currentFrame = Mathf.Clamp(frame, 0, GetTotalFrames() - 1);
            
            // 计算时间（50ms/帧 = 20fps）
            float time = _currentFrame * FRAME_TIME;
            _currentAnimState.Time = time;
            
            // 手动更新Animancer
            if (_animancer != null)
            {
                AnimationHelper.EvaluateAnimancer(_animancer, 0);
            }
        }
        
        /// <summary>
        /// 获取当前帧
        /// </summary>
        public int GetCurrentFrame()
        {
            if (_currentAnimState == null)
                return 0;
            
            return Mathf.RoundToInt(_currentAnimState.Time * LOGIC_FRAME_RATE);
        }
        
        /// <summary>
        /// 获取总帧数
        /// </summary>
        public int GetTotalFrames()
        {
            if (_currentClip == null)
                return 60;
            
            // 动画总时长(秒) * 逻辑帧率(20fps) = 逻辑帧数
            return Mathf.RoundToInt(_currentClip.length * LOGIC_FRAME_RATE);
        }
        
        // === 绘制 ===
        
        public override void DrawPreview(Rect rect)
        {
            if (_previewRenderUtility == null)
            {
                Initialize();
            }
            
            // 使用基类的渲染方法
            RenderPreview(rect);
            
            // 处理输入
            HandleInput(rect);
        }
        
        // === 更新动画（重写以支持逐帧）===
        
        protected override void UpdateAnimation()
        {
            if (_isPlaying && _currentAnimState != null && _animancer != null)
            {
                double currentTime = UnityEditor.EditorApplication.timeSinceStartup;
                float deltaTime = (float)(currentTime - _lastUpdateTime);
                _lastUpdateTime = currentTime;
                
                // 累积时间
                _accumulatedTime += deltaTime;
                
                // 按照逻辑帧率（20fps）逐帧更新
                while (_accumulatedTime >= FRAME_TIME)
                {
                    _accumulatedTime -= FRAME_TIME;
                    
                    // 前进一帧
                    _currentFrame++;
                    
                    // 检查是否到达结尾
                    int totalFrames = GetTotalFrames();
                    if (_currentFrame >= totalFrames)
                    {
                        // 循环播放
                        _currentFrame = 0;
                    }
                    
                    // 设置动画时间
                    _currentAnimState.Time = _currentFrame * FRAME_TIME;
                }
                
                // 手动更新Animancer（不传入deltaTime，因为我们直接设置了Time）
                AnimationHelper.EvaluateAnimancer(_animancer, 0);
            }
        }
    }
}
