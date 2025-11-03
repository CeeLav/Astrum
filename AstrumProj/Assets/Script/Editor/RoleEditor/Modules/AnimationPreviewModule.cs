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
        
        // === 碰撞盒显示 ===
        private string _currentFrameCollisionInfo = null; // 当前帧的碰撞盒信息
        
        // === 网格显示 ===
        private bool _showGrid = true;                  // 是否显示网格
        private Material _gridMaterial;                 // 网格绘制材质
        
        // === 根节点位移数据 ===
        private System.Collections.Generic.List<int> _rootMotionDataArray = null; // 位移数据数组
        private Vector3 _initialPosition = Vector3.zero; // 模型的初始位置（用于重置）
        
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
            
            // 手动更新Animancer
            if (_animancer != null)
            {
                AnimationHelper.EvaluateAnimancer(_animancer, 0);
            }
            
            // 根据位移数据手动累加位移（RootMotion关闭时需要手动应用）
            ApplyAccumulatedDisplacement(_currentFrame);
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
            
            // 重置模型位置到初始位置
            if (_previewInstance != null)
            {
                _previewInstance.transform.position = _initialPosition;
            }
        }
        
        /// <summary>
        /// 根据帧索引计算累积位移并应用到模型
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
            
            // 渲染
            _previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            _previewRenderUtility.camera.Render();
            
            // 在相机渲染后，绘制网格（在角色脚底下）
            if (_showGrid)
            {
                DrawGrid(_previewRenderUtility.camera);
            }
            
            // 在相机渲染后，绘制碰撞盒（模型在原点，相对偏移直接绘制）
            if (!string.IsNullOrEmpty(_currentFrameCollisionInfo))
            {
                Services.CollisionShapePreview.DrawCollisionInfo(
                    _currentFrameCollisionInfo, 
                    _previewRenderUtility.camera
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
                    
                    // 根据位移数据手动累加位移（RootMotion关闭时需要手动应用）
                    ApplyAccumulatedDisplacement(_currentFrame);
                }
                
                // 手动更新Animancer（不传入deltaTime，因为我们直接设置了Time）
                AnimationHelper.EvaluateAnimancer(_animancer, 0);
                
                // 确保当前帧的位移也被应用（在非逐帧更新时）
                ApplyAccumulatedDisplacement(_currentFrame);
            }
        }
        
        // === 清理资源 ===
        
        public override void Cleanup()
        {
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
