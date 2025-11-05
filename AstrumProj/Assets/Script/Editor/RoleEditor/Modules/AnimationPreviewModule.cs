using UnityEngine;
using UnityEditor;
using Animancer;
using Astrum.Editor.RoleEditor.Services;
using Astrum.Editor.RoleEditor.Persistence;

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
        private double _playStartTime = 0.0;             // 开始播放的时间戳（用于基于时间戳的播放）
        
        // === 碰撞盒显示 ===
        private string _currentFrameCollisionInfo = null; // 当前帧的碰撞盒信息
        private bool _showCollision = true;              // 是否显示攻击碰撞盒
        
        // === 网格显示 ===
        private bool _showGrid = true;                  // 是否显示网格
        private Material _gridMaterial;                 // 网格绘制材质
        
        // === 根节点位移数据 ===
        private System.Collections.Generic.List<int> _rootMotionDataArray = null; // 位移数据数组
        private Vector3 _initialPosition = Vector3.zero; // 模型的初始位置（用于重置）
        
        // === 播放时长限制 ===
        private float _maxPlaybackTime = -1f; // 最大播放时长（秒），-1表示不限制（播放完整动画）
        
        // === 循环播放 ===
        private bool _isLooping = false; // 是否循环播放
        
        // === 模型缩放 ===
        private float _modelScale = 1.0f; // 模型缩放比例
        
        // === 特效预览 ===
        private VFXPreviewManager _vfxPreviewManager;
        
        protected override string LogPrefix => "[AnimationPreviewModule]";
        
        // === 实体管理 ===
        
        /// <summary>
        /// 设置预览的实体（加载模型）
        /// </summary>
        public void SetEntity(int entityId)
        {
            if (_currentEntityId == entityId) return;
            
            _currentEntityId = entityId;
            
            // 从EntityBaseTable获取模型ID
            var entityData = ConfigTableHelper.GetEntityById(entityId);
            if (entityData == null || entityData.ModelId == 0)
            {
                Debug.LogWarning($"{LogPrefix} No model for entity {entityId}");
                CleanupPreviewInstance();
                return;
            }
            
            // 从 EntityModelTable 获取模型路径
            var modelData = EntityModelDataReader.ReadById(entityData.ModelId);
            if (modelData == null || string.IsNullOrEmpty(modelData.ModelPath))
            {
                Debug.LogWarning($"{LogPrefix} No model path for modelId {entityData.ModelId}");
                CleanupPreviewInstance();
                return;
            }
            
            // 加载模型（使用基类方法）
            LoadModel(modelData.ModelPath);
            
            // 保存初始位置（在计算边界后，模型位置应该在原点）
            if (_previewInstance != null)
            {
                _initialPosition = _previewInstance.transform.position;
                // 应用缩放
                _previewInstance.transform.localScale = Vector3.one * _modelScale;
            }
            
            // 初始化特效预览管理器
            if (_vfxPreviewManager == null)
            {
                _vfxPreviewManager = new VFXPreviewManager();
            }
            
            // 设置父对象和预览渲染工具
            if (_previewInstance != null)
            {
                _vfxPreviewManager.SetParent(_previewInstance);
                _vfxPreviewManager.SetPreviewRenderUtility(_previewRenderUtility);
            }
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
            // 检查前置条件
            if (_previewInstance == null)
            {
                Debug.LogWarning($"{LogPrefix} Cannot play: Preview instance is null. Please select an entity first.");
                return;
            }
            
            if (_animancer == null)
            {
                Debug.LogWarning($"{LogPrefix} Cannot play: Animancer is null. Please select an entity first.");
                return;
            }
            
            if (_currentClip == null)
            {
                Debug.LogWarning($"{LogPrefix} Cannot play: Animation clip is null. Please load an animation first.");
                return;
            }
            
            // 如果动画状态不存在，尝试重新加载动画
            if (_currentAnimState == null)
            {
                Debug.LogWarning($"{LogPrefix} Animation state is null, attempting to reload animation...");
                LoadAnimation(_currentClip, _currentAnimationPath);
                
                if (_currentAnimState == null)
                {
                    Debug.LogError($"{LogPrefix} Failed to reload animation. Cannot play.");
                    return;
                }
            }
            
            _isPlaying = true;
            _currentAnimState.Speed = 0; // 保持Speed=0，完全由我们手动控制Time
            
            // 重置所有时间相关状态
            double currentTimestamp = UnityEditor.EditorApplication.timeSinceStartup;
            _playStartTime = currentTimestamp;
            _accumulatedTime = 0f;
            
            // 仅在调试时输出日志
            // Debug.Log($"{LogPrefix} Playing animation: {_currentClip.name}");
        }
        
        public override void Pause()
        {
            if (_currentAnimState != null)
            {
                _isPlaying = false;
                _currentAnimState.Speed = 0;
                
                // 仅在调试时输出日志
                // Debug.Log($"{LogPrefix} Paused animation");
            }
        }
        
        public override void Stop()
        {
            base.Stop();
            _currentFrame = 0;
            _accumulatedTime = 0f;
            _playStartTime = 0.0;
            
            // 清理所有特效
            if (_vfxPreviewManager != null)
            {
                _vfxPreviewManager.ClearAll();
            }
            
            // 重置模型位置到初始位置
            if (_previewInstance != null)
            {
                _previewInstance.transform.position = _initialPosition;
                _previewInstance.transform.rotation = Quaternion.identity;
            }
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
            
            // 如果正在播放，需要同步更新 _playStartTime，避免时间计算错误
            if (_isPlaying)
            {
                // 重新计算播放起点时间，使当前时间戳对应当前帧
                double currentTimestamp = UnityEditor.EditorApplication.timeSinceStartup;
                _playStartTime = currentTimestamp - time / _animationSpeed;
            }
            
            // 手动更新Animancer
            if (_animancer != null)
            {
                AnimationHelper.EvaluateAnimancer(_animancer, 0);
            }
            
            // 根据位移数据手动累加位移（RootMotion关闭时需要手动应用）
            ApplyAccumulatedDisplacement(_currentFrame);
            
            // 更新特效预览
            if (_vfxPreviewManager != null)
            {
                _vfxPreviewManager.UpdateFrame(_currentFrame, _isPlaying);
                _vfxPreviewManager.UpdateVFXPositions();
            }
        }
        
        /// <summary>
        /// 设置 VFX 事件列表（用于特效预览）
        /// </summary>
        public void SetVFXEvents(System.Collections.Generic.List<Timeline.TimelineEvent> events)
        {
            if (_vfxPreviewManager == null)
            {
                _vfxPreviewManager = new VFXPreviewManager();
                if (_previewInstance != null)
                {
                    _vfxPreviewManager.SetParent(_previewInstance);
                }
            }
            
            _vfxPreviewManager.SetVFXEvents(events);
            _vfxPreviewManager.SetPreviewRenderUtility(_previewRenderUtility);
        }
        
        /// <summary>
        /// 更新指定特效的参数（用于实时编辑）
        /// </summary>
        public void UpdateVFXParameters(string eventId)
        {
            if (_vfxPreviewManager != null)
            {
                _vfxPreviewManager.UpdateVFXParameters(eventId, _currentFrame);
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
        
        /// <summary>
        /// 获取预览模型实例（用于提取Hips位移）
        /// </summary>
        public GameObject GetPreviewModel()
        {
            return _previewInstance;
        }
        
        /// <summary>
        /// 设置根节点位移数据（用于手动累加位移）
        /// </summary>
        public void SetRootMotionData(System.Collections.Generic.List<int> rootMotionDataArray)
        {
            _rootMotionDataArray = rootMotionDataArray;
            
            // 重新应用当前帧的位移（而不是直接重置到初始位置）
            // 这样可以保持动画的当前状态
            if (_previewInstance != null && _rootMotionDataArray != null && _rootMotionDataArray.Count > 0)
            {
                // 重新应用当前帧的位移
                ApplyAccumulatedDisplacement(_currentFrame);
            }
            else if (_previewInstance != null)
            {
                // 如果没有位移数据，才重置到初始位置
                _previewInstance.transform.position = _initialPosition;
                _previewInstance.transform.rotation = Quaternion.identity;
            }
        }
        
        /// <summary>
        /// 设置最大播放时长（基于Duration帧数）
        /// </summary>
        /// <param name="durationFrames">持续时间（逻辑帧数），-1表示不限制（播放完整动画）</param>
        public void SetMaxPlaybackDuration(int durationFrames)
        {
            if (durationFrames <= 0)
            {
                // 不限制，播放完整动画
                _maxPlaybackTime = -1f;
            }
            else
            {
                // 转换为时间（秒）
                _maxPlaybackTime = durationFrames * FRAME_TIME;
            }
        }
        
        /// <summary>
        /// 根据浮点时间计算插值后的累积位移并应用到模型
        /// 使用线性插值在逻辑帧之间平滑过渡
        /// </summary>
        private void ApplyInterpolatedDisplacement(float time)
        {
            if (_previewInstance == null || _rootMotionDataArray == null || _rootMotionDataArray.Count == 0)
            {
                return;
            }
            
            int frameCount = _rootMotionDataArray[0];
            if (frameCount <= 0)
            {
                return;
            }
            
            // 将时间转换为逻辑帧索引（浮点）
            float frameFloat = time * LOGIC_FRAME_RATE;
            
            // 确保在有效范围内（处理循环）
            frameFloat = frameFloat % frameCount;
            if (frameFloat < 0)
            {
                frameFloat += frameCount;
            }
            
            // 计算当前所在的逻辑帧索引和下帧索引
            int frameIndex0 = Mathf.FloorToInt(frameFloat);
            int frameIndex1 = (frameIndex0 + 1) % frameCount;
            
            // 计算插值因子 [0, 1)
            float t = frameFloat - frameIndex0;
            
            const float SCALE = 1000f; // 与提取时使用的缩放因子相同
            
            // 计算累积位移的辅助函数
            Vector3 GetAccumulatedPosition(int endFrame)
            {
                Vector3 pos = Vector3.zero;
                for (int f = 1; f <= endFrame && f < frameCount; f++)
                {
                    int baseIndex = 1 + f * 7;
                    if (baseIndex + 6 >= _rootMotionDataArray.Count)
                    {
                        break;
                    }
                    float dx = _rootMotionDataArray[baseIndex] / SCALE;
                    float dy = _rootMotionDataArray[baseIndex + 1] / SCALE;
                    float dz = _rootMotionDataArray[baseIndex + 2] / SCALE;
                    pos += new Vector3(dx, dy, dz);
                }
                return pos;
            }
            
            Quaternion GetAccumulatedRotation(int endFrame)
            {
                Quaternion rot = Quaternion.identity;
                for (int f = 1; f <= endFrame && f < frameCount; f++)
                {
                    int baseIndex = 1 + f * 7;
                    if (baseIndex + 6 >= _rootMotionDataArray.Count)
                    {
                        break;
                    }
                    float rx = _rootMotionDataArray[baseIndex + 3] / SCALE;
                    float ry = _rootMotionDataArray[baseIndex + 4] / SCALE;
                    float rz = _rootMotionDataArray[baseIndex + 5] / SCALE;
                    float rw = _rootMotionDataArray[baseIndex + 6] / SCALE;
                    Quaternion deltaRot = new Quaternion(rx, ry, rz, rw);
                    rot = rot * deltaRot;
                }
                return rot;
            }
            
            // 计算两个逻辑帧的累积位移
            Vector3 accumulatedPos0 = GetAccumulatedPosition(frameIndex0);
            Quaternion accumulatedRot0 = GetAccumulatedRotation(frameIndex0);
            
            Vector3 accumulatedPos1 = GetAccumulatedPosition(frameIndex1);
            Quaternion accumulatedRot1 = GetAccumulatedRotation(frameIndex1);
            
            // 在两个逻辑帧之间进行线性插值
            Vector3 interpolatedPosition = Vector3.Lerp(accumulatedPos0, accumulatedPos1, t);
            Quaternion interpolatedRotation = Quaternion.Lerp(accumulatedRot0, accumulatedRot1, t);
            
            // 应用到模型位置（初始位置 + 插值后的累积位移）
            _previewInstance.transform.position = _initialPosition + interpolatedPosition;
            
            // 应用插值后的旋转
            _previewInstance.transform.rotation = interpolatedRotation;
        }
        
        /// <summary>
        /// 根据帧索引计算累积位移并应用到模型（用于SetFrame，不使用插值）
        /// </summary>
        private void ApplyAccumulatedDisplacement(int frame)
        {
            if (_previewInstance == null || _rootMotionDataArray == null || _rootMotionDataArray.Count == 0)
            {
                return;
            }
            
            int frameCount = _rootMotionDataArray[0];
            if (frameCount <= 0 || frame < 0 || frame >= frameCount)
            {
                return;
            }
            
            // 计算累积位移：从第0帧到当前帧的所有增量位移之和
            Vector3 accumulatedPosition = Vector3.zero;
            Quaternion accumulatedRotation = Quaternion.identity;
            
            const float SCALE = 1000f; // 与提取时使用的缩放因子相同
            
            // 累加从第1帧到当前帧的所有增量位移（第0帧的delta是0）
            for (int f = 1; f <= frame && f < frameCount; f++)
            {
                int baseIndex = 1 + f * 7; // 跳过 frameCount，每帧7个值
                if (baseIndex + 6 >= _rootMotionDataArray.Count)
                {
                    break;
                }
                
                // 读取增量位移（整数，需要除以1000）
                float dx = _rootMotionDataArray[baseIndex] / SCALE;
                float dy = _rootMotionDataArray[baseIndex + 1] / SCALE;
                float dz = _rootMotionDataArray[baseIndex + 2] / SCALE;
                float rx = _rootMotionDataArray[baseIndex + 3] / SCALE;
                float ry = _rootMotionDataArray[baseIndex + 4] / SCALE;
                float rz = _rootMotionDataArray[baseIndex + 5] / SCALE;
                float rw = _rootMotionDataArray[baseIndex + 6] / SCALE;
                
                // 累加位置
                accumulatedPosition += new Vector3(dx, dy, dz);
                
                // 累加旋转（四元数乘法）
                Quaternion deltaRot = new Quaternion(rx, ry, rz, rw);
                accumulatedRotation = accumulatedRotation * deltaRot;
            }
            
            // 应用到模型位置（初始位置 + 累积位移）
            _previewInstance.transform.position = _initialPosition + accumulatedPosition;
            
            // 应用累积旋转（初始旋转 * 累积旋转）
            _previewInstance.transform.rotation = accumulatedRotation;
        }
        
        /// <summary>
        /// 设置当前帧的碰撞盒信息（从编辑器传入）
        /// </summary>
        /// <param name="collisionInfo">碰撞盒信息字符串，格式：Box:5x2x1, Sphere:3.0, Capsule:2x5, Point</param>
        public void SetFrameCollisionInfo(string collisionInfo)
        {
            _currentFrameCollisionInfo = collisionInfo;
        }
        
        /// <summary>
        /// 清除碰撞盒显示
        /// </summary>
        public void ClearCollisionInfo()
        {
            _currentFrameCollisionInfo = null;
        }
        
        // === 绘制 ===
        
        public override void DrawPreview(Rect rect)
        {
            if (_previewRenderUtility == null)
            {
                Initialize();
            }
            
            // 使用自定义的渲染方法（包含碰撞盒绘制）
            RenderPreviewWithCollision(rect);
            
            // 处理输入
            HandleInput(rect);
        }
        
        /// <summary>
        /// 渲染预览场景（包含碰撞盒和网格）
        /// </summary>
        private void RenderPreviewWithCollision(Rect rect)
        {
            if (_previewRenderUtility == null || _previewInstance == null)
            {
                DrawEmptyPreview(rect);
                return;
            }
            
            // 更新相机位置（球面坐标）
            UpdateCamera();
            
            // 更新动画
            UpdateAnimation();
            
            // 更新特效粒子系统（PreviewRenderUtility 不会自动更新）
            if (_vfxPreviewManager != null)
            {
                // 使用固定时间步（PreviewRenderUtility 每帧都会调用，使用固定时间步更稳定）
                // 约60fps的时间步
                float deltaTime = 0.016f;
                _vfxPreviewManager.UpdateParticleSystems(deltaTime);
            }
            
            // 渲染
            _previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            _previewRenderUtility.camera.Render();
            
            // 在相机渲染后，绘制网格（在角色脚底下）
            if (_showGrid)
            {
                DrawGrid(_previewRenderUtility.camera);
            }
            
            // 在相机渲染后，绘制碰撞盒（跟随模型的位移和旋转）
            if (_showCollision && !string.IsNullOrEmpty(_currentFrameCollisionInfo))
            {
                // 获取模型当前的位置和旋转（考虑根节点位移）
                Vector3 modelPosition = _previewInstance != null ? _previewInstance.transform.position : Vector3.zero;
                Quaternion modelRotation = _previewInstance != null ? _previewInstance.transform.rotation : Quaternion.identity;
                
                Services.CollisionShapePreview.DrawCollisionInfo(
                    _currentFrameCollisionInfo, 
                    _previewRenderUtility.camera,
                    null, // 使用默认颜色
                    modelPosition,
                    modelRotation
                );
            }
            
            Texture texture = _previewRenderUtility.EndPreview();
            GUI.DrawTexture(rect, texture);
        }
        
        // === 网格绘制 ===
        
        /// <summary>
        /// 获取网格绘制材质
        /// </summary>
        private Material GetGridMaterial()
        {
            if (_gridMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _gridMaterial = new Material(shader);
                _gridMaterial.hideFlags = HideFlags.HideAndDontSave;
                _gridMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _gridMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _gridMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _gridMaterial.SetInt("_ZWrite", 0);
                _gridMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
            return _gridMaterial;
        }
        
        /// <summary>
        /// 绘制地面网格（在 Y=0 平面上）
        /// </summary>
        private void DrawGrid(Camera camera)
        {
            if (camera == null) return;
            
            const float gridSize = 10f;        // 网格大小（10x10单位）
            Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);  // 半透明灰色
            Color axisColorX = new Color(1f, 0.3f, 0.3f, 0.8f);   // X轴红色
            Color axisColorZ = new Color(0.3f, 0.3f, 1f, 0.8f);   // Z轴蓝色
            
            Material mat = GetGridMaterial();
            if (mat == null) return;
            
            mat.SetPass(0);
            
            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.LoadIdentity();
            GL.MultMatrix(camera.worldToCameraMatrix);
            
            GL.Begin(GL.LINES);
            
            // 绘制网格线
            int halfGridSize = Mathf.RoundToInt(gridSize * 0.5f);
            
            // X方向网格线（平行于X轴，沿着Z轴分布）
            for (int z = -halfGridSize; z <= halfGridSize; z++)
            {
                bool isAxisLine = (z == 0);
                GL.Color(isAxisLine ? axisColorZ : gridColor);
                
                Vector3 start = new Vector3(-halfGridSize, 0, z);
                Vector3 end = new Vector3(halfGridSize, 0, z);
                
                GL.Vertex(start);
                GL.Vertex(end);
            }
            
            // Z方向网格线（平行于Z轴，沿着X轴分布）
            for (int x = -halfGridSize; x <= halfGridSize; x++)
            {
                bool isAxisLine = (x == 0);
                GL.Color(isAxisLine ? axisColorX : gridColor);
                
                Vector3 start = new Vector3(x, 0, -halfGridSize);
                Vector3 end = new Vector3(x, 0, halfGridSize);
                
                GL.Vertex(start);
                GL.Vertex(end);
            }
            
            GL.End();
            GL.PopMatrix();
        }
        
        // === 更新动画（统一更新逻辑，重写以支持平滑渲染帧更新）===
        
        /// <summary>
        /// 统一动画更新入口 - 确保每帧只更新一次
        /// </summary>
        private void UpdateAnimationInternal()
        {
            // 如果不在播放状态，直接返回
            if (!_isPlaying || _currentAnimState == null || _animancer == null || _currentClip == null)
            {
                return;
            }
            
            // 获取当前时间戳
            double currentRealTime = UnityEditor.EditorApplication.timeSinceStartup;
            
            // 检查时间是否异常（防止编辑器暂停、调试断点等情况）
            if (currentRealTime < _playStartTime)
            {
                // 时间倒流（通常发生在编辑器暂停恢复时），重置播放起点
                _playStartTime = currentRealTime;
                return;
            }
            
            // 计算从播放开始经过的时间
            double elapsedTime = currentRealTime - _playStartTime;
            
            // 如果时间跳跃过大（超过1秒，可能是编辑器暂停后恢复），重置播放起点
            if (elapsedTime > (_currentAnimState.Time + 1.0))
            {
                _playStartTime = currentRealTime - _currentAnimState.Time;
                elapsedTime = _currentAnimState.Time;
            }
            
            // 计算新的动画时间：基于绝对时间戳 * 播放速度（即使多次调用结果也一致）
            float newAnimationTime = (float)elapsedTime * _animationSpeed;
            
            // 检查播放边界
            bool shouldStop = false;
            float maxTime = _maxPlaybackTime > 0f ? _maxPlaybackTime : _currentClip.length;
            
            if (newAnimationTime >= maxTime)
            {
                if (_isLooping)
                {
                    // 循环播放：重置播放时间
                    double currentTimestamp = UnityEditor.EditorApplication.timeSinceStartup;
                    _playStartTime = currentTimestamp; // 重置播放开始时间
                    newAnimationTime = 0f;
                }
                else
                {
                    // 不循环：停在最后一帧
                    newAnimationTime = maxTime;
                    shouldStop = true;
                }
            }
            
            // 确保时间不小于0
            if (newAnimationTime < 0)
            {
                newAnimationTime = 0f;
            }
            
            // 应用新的动画时间
            _currentAnimState.Time = newAnimationTime;
            
            // 更新当前逻辑帧（用于UI显示）
            _currentFrame = Mathf.RoundToInt(newAnimationTime * LOGIC_FRAME_RATE);
            
            // 更新Animancer（传入0，因为我们完全手动控制Time，Speed=0）
            // Evaluate 只会应用当前的Time值，不会累加时间
            AnimationHelper.EvaluateAnimancer(_animancer, 0f);
            
            // 应用位移插值
            ApplyInterpolatedDisplacement(newAnimationTime);
            
            // 更新特效预览
            if (_vfxPreviewManager != null)
            {
                _vfxPreviewManager.UpdateFrame(_currentFrame, _isPlaying);
                _vfxPreviewManager.UpdateVFXPositions();
            }
            
            // 如果应该停止，停止播放
            if (shouldStop)
            {
                Pause();
            }
        }
        
        /// <summary>
        /// 重写基类的UpdateAnimation - 统一调用内部更新逻辑
        /// </summary>
        protected override void UpdateAnimation()
        {
            // 直接调用统一更新逻辑，不调用基类方法（避免基类的自动更新干扰）
            UpdateAnimationInternal();
        }
        
        /// <summary>
        /// 重置动画到第一帧（不播放）
        /// </summary>
        public void Reset()
        {
            if (_currentAnimState != null)
            {
                _currentAnimState.Time = 0f;
                _currentFrame = 0;
                _accumulatedTime = 0f;
                _playStartTime = 0.0;
                _isPlaying = false;
                _currentAnimState.Speed = 0;
                
                // 重置模型位置到初始位置
                if (_previewInstance != null)
                {
                    _previewInstance.transform.position = _initialPosition;
                    _previewInstance.transform.rotation = Quaternion.identity;
                }
                
                // 手动更新Animancer
                if (_animancer != null)
                {
                    AnimationHelper.EvaluateAnimancer(_animancer, 0);
                }
                
                // 重置位移
                ApplyAccumulatedDisplacement(0);
                
                // 清理所有特效
                if (_vfxPreviewManager != null)
                {
                    _vfxPreviewManager.ClearAll();
                }
            }
        }
        
        /// <summary>
        /// 获取是否循环播放
        /// </summary>
        /// <returns>是否循环播放</returns>
        public bool IsLooping()
        {
            return _isLooping;
        }
        
        /// <summary>
        /// 设置是否循环播放
        /// </summary>
        /// <param name="isLooping">是否循环播放</param>
        public void SetLooping(bool isLooping)
        {
            _isLooping = isLooping;
        }
        
        /// <summary>
        /// 设置是否显示攻击碰撞盒
        /// </summary>
        /// <param name="show">是否显示</param>
        public void SetShowCollision(bool show)
        {
            _showCollision = show;
        }
        
        /// <summary>
        /// 获取是否显示攻击碰撞盒
        /// </summary>
        public bool GetShowCollision()
        {
            return _showCollision;
        }
        
        /// <summary>
        /// 设置模型缩放
        /// </summary>
        /// <param name="scale">缩放比例</param>
        public void SetModelScale(float scale)
        {
            _modelScale = Mathf.Max(0.1f, Mathf.Min(10.0f, scale)); // 限制在0.1到10之间
            if (_previewInstance != null)
            {
                _previewInstance.transform.localScale = Vector3.one * _modelScale;
            }
        }
        
        /// <summary>
        /// 获取模型缩放
        /// </summary>
        public float GetModelScale()
        {
            return _modelScale;
        }
        
        // === 清理资源 ===
        
        public override void Cleanup()
        {
            // 清理特效预览管理器
            if (_vfxPreviewManager != null)
            {
                _vfxPreviewManager.Cleanup();
                _vfxPreviewManager = null;
            }
            
            // 清理网格材质
            if (_gridMaterial != null)
            {
                Object.DestroyImmediate(_gridMaterial);
                _gridMaterial = null;
            }
            
            base.Cleanup();
        }
    }
}
