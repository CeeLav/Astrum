using System;
using System.Collections.Generic;
using UnityEngine;
using Astrum.CommonBase;
using Astrum.View.Managers;

namespace Astrum.View.Core
{
    /// <summary>
    /// Stage类 - 游戏场景/阶段的表现层映射，不执行游戏逻辑
    /// </summary>
    public class Stage
    {
        // Stage基本信息
        protected string _stageId;
        protected string _stageName;
        protected long _roomId;
        
        // Stage状态
        protected bool _isActive;
        protected bool _isLoaded;
        protected DateTime _lastSyncTime;
        
        // 视图组件
        protected Dictionary<long, EntityView> _entityViews;
        protected Dictionary<long, GameObject> _unitViews;
        
        // Unity组件
        protected Camera _camera;
        protected Environment _environment;
        protected GameObject _stageRoot;
        
        // 公共属性
        public string StageId => _stageId;
        public string StageName => _stageName;
        public long RoomId => _roomId;
        public bool IsActive => _isActive;
        public bool IsLoaded => _isLoaded;
        public DateTime LastSyncTime => _lastSyncTime;
        public Dictionary<long, EntityView> EntityViews => _entityViews;
        public Camera Camera => _camera;
        public Environment Environment => _environment;
        
        // 事件
        public event Action OnStageActivated;
        public event Action OnStageDeactivated;
        public event Action OnStageLoaded;
        public event Action OnStageUnloaded;
        public event Action<EntityView> OnEntityViewAdded;
        public event Action<long> OnEntityViewRemoved;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public Stage(string stageId, string stageName = null)
        {
            _stageId = stageId;
            _stageName = stageName ?? stageId;
            _roomId = 0;
            _isActive = false;
            _isLoaded = false;
            _lastSyncTime = DateTime.MinValue;
            _entityViews = new Dictionary<long, EntityView>();
            _unitViews = new Dictionary<long, GameObject>();
        }
        
        /// <summary>
        /// 初始化Stage
        /// </summary>
        public void Initialize()
        {
            if (_isLoaded) return;
            
            ASLogger.Instance.Info($"Stage: 初始化 {_stageName}");
            
            // 创建Stage根对象
            CreateStageRoot();
            
            // 设置摄像机
            SetupCamera();
            
            // 设置环境
            SetupEnvironment();
            
            // 调用子类的初始化方法
            OnInitialize();
            
            _isLoaded = true;
            OnStageLoaded?.Invoke();
        }
        
        /// <summary>
        /// 子类可重写的初始化方法
        /// </summary>
        protected virtual void OnInitialize()
        {
        }
        
        /// <summary>
        /// 子类可重写的加载方法
        /// </summary>
        protected virtual void OnLoad()
        {
        }
        
        /// <summary>
        /// 子类可重写的卸载方法
        /// </summary>
        protected virtual void OnUnload()
        {
        }
        
        /// <summary>
        /// 子类可重写的更新方法
        /// </summary>
        protected virtual void OnUpdate(float deltaTime)
        {
        }
        
        /// <summary>
        /// 子类可重写的渲染方法
        /// </summary>
        protected virtual void OnRender()
        {
        }
        
        /// <summary>
        /// 子类可重写的Stage进入方法
        /// </summary>
        protected virtual void OnStageEnter()
        {
        }
        
        /// <summary>
        /// 子类可重写的Stage退出方法
        /// </summary>
        protected virtual void OnStageExit()
        {
        }
        
        /// <summary>
        /// 子类可重写的激活方法
        /// </summary>
        protected virtual void OnActivate()
        {
        }
        
        /// <summary>
        /// 子类可重写的停用方法
        /// </summary>
        protected virtual void OnDeactivate()
        {
        }
        
        /// <summary>
        /// 子类可重写的房间同步方法
        /// </summary>
        /// <param name="roomData">房间数据</param>
        protected virtual void OnSyncWithRoom(RoomData roomData)
        {
        }
        
        /// <summary>
        /// 子类可重写的实体视图创建方法
        /// </summary>
        /// <param name="entityId">实体UniqueId</param>
        /// <returns>创建的实体视图</returns>
        protected virtual EntityView CreateEntityView(long entityId)
        {
            return null;
        }
        
        /// <summary>
        /// 创建Stage根对象
        /// </summary>
        private void CreateStageRoot()
        {
            _stageRoot = new GameObject($"Stage_{_stageId}");
            ASLogger.Instance.Info($"Stage: 创建Stage根对象 {_stageRoot.name}");
        }
        
        /// <summary>
        /// 设置摄像机
        /// </summary>
        private void SetupCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                GameObject cameraObj = new GameObject("Stage Camera");
                cameraObj.transform.SetParent(_stageRoot.transform);
                _camera = cameraObj.AddComponent<Camera>();
                _camera.tag = "MainCamera";
                _camera.transform.position = new Vector3(0, 10, -10);
                _camera.transform.rotation = Quaternion.Euler(30, 0, 0);
            }
            
            ASLogger.Instance.Info("Stage: 摄像机设置完成");
        }
        
        /// <summary>
        /// 设置环境
        /// </summary>
        private void SetupEnvironment()
        {
            _environment = UnityEngine.Object.FindObjectOfType<Environment>();
            if (_environment == null)
            {
                GameObject envObj = new GameObject("Environment");
                envObj.transform.SetParent(_stageRoot.transform);
                _environment = envObj.AddComponent<Environment>();
            }
            
            ASLogger.Instance.Info("Stage: 环境设置完成");
        }
        
        /// <summary>
        /// 激活Stage
        /// </summary>
        public void SetActive(bool active)
        {
            if (_isActive == active) return;
            
            _isActive = active;
            
            if (_stageRoot != null)
            {
                _stageRoot.SetActive(active);
            }
            
            if (_isActive)
            {
                OnActivate();
                OnStageActivated?.Invoke();
                ASLogger.Instance.Info($"Stage: 激活 {_stageName}");
            }
            else
            {
                OnDeactivate();
                OnStageDeactivated?.Invoke();
                ASLogger.Instance.Info($"Stage: 停用 {_stageName}");
            }
        }
        
        /// <summary>
        /// 进入Stage
        /// </summary>
        public void OnEnter()
        {
            ASLogger.Instance.Info($"Stage: 进入 {_stageName}");
            
            // 调用子类的进入方法
            OnStageEnter();
            
            // 激活所有实体视图
            foreach (var entityView in _entityViews.Values)
            {
                if (entityView != null)
                {
                    entityView.SetActive(true);
                }
            }
        }
        
        /// <summary>
        /// 退出Stage
        /// </summary>
        public void OnExit()
        {
            ASLogger.Instance.Info($"Stage: 退出 {_stageName}");
            
            // 调用子类的退出方法
            OnStageExit();
            
            // 停用所有实体视图
            foreach (var entityView in _entityViews.Values)
            {
                if (entityView != null)
                {
                    entityView.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// 更新Stage - 只更新表现层内容
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isActive) return;
            
            // 调用子类的更新方法
            OnUpdate(deltaTime);
            
            // 更新所有EntityView
            foreach (var entityView in _entityViews.Values)
            {
                entityView?.UpdateView(deltaTime);
            }
            
            // 更新环境
            _environment?.Update();
        }
        
        /// <summary>
        /// 创建单位视图 - 由逻辑层调用
        /// </summary>
        public GameObject CreateUnitView(long unitId, Vector3 position, string unitType = "default")
        {
            if (_unitViews.ContainsKey(unitId))
            {
                ASLogger.Instance.Warning($"Stage: 单位视图已存在，ID: {unitId}");
                return _unitViews[unitId];
            }
            
            GameObject unitView = CreateUnitGameObject(unitType);
            unitView.transform.SetParent(_stageRoot.transform);
            unitView.transform.position = position;
            unitView.name = $"Unit_{unitId}_{unitType}";
            
            _unitViews[unitId] = unitView;
            
            ASLogger.Instance.Info($"Stage: 创建单位视图，ID: {unitId}, Type: {unitType}");
            return unitView;
        }
        
        /// <summary>
        /// 移除单位视图 - 由逻辑层调用
        /// </summary>
        public void RemoveUnitView(long unitId)
        {
            if (!_unitViews.ContainsKey(unitId))
            {
                ASLogger.Instance.Warning($"Stage: 单位视图不存在，ID: {unitId}");
                return;
            }
            
            GameObject unitView = _unitViews[unitId];
            _unitViews.Remove(unitId);
            
            if (unitView != null)
            {
                UnityEngine.Object.Destroy(unitView);
            }
            
            ASLogger.Instance.Info($"Stage: 移除单位视图，ID: {unitId}");
        }
        
        /// <summary>
        /// 更新单位位置 - 由逻辑层调用
        /// </summary>
        public void UpdateUnitPosition(long unitId, Vector3 position)
        {
            if (_unitViews.TryGetValue(unitId, out GameObject unitView) && unitView != null)
            {
                unitView.transform.position = position;
            }
        }
        
        /// <summary>
        /// 设置Room ID - 由逻辑层调用
        /// </summary>
        public void SetRoomId(long roomId)
        {
            _roomId = roomId;
            _lastSyncTime = DateTime.Now;
            ASLogger.Instance.Info($"Stage: 设置Room ID = {roomId}");
        }
        
        /// <summary>
        /// 创建单位游戏对象
        /// </summary>
        private GameObject CreateUnitGameObject(string unitType)
        {
            GameObject unit;
            
            switch (unitType.ToLower())
            {
                case "player":
                    unit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    unit.GetComponent<Renderer>().material.color = Color.blue;
                    break;
                case "enemy":
                    unit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    unit.GetComponent<Renderer>().material.color = Color.red;
                    break;
                default:
                    unit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    unit.GetComponent<Renderer>().material.color = Color.gray;
                    break;
            }
            
            // 添加简单的碰撞器
            if (unit.GetComponent<Collider>() == null)
            {
                unit.AddComponent<BoxCollider>();
            }
            
            return unit;
        }
        
        /// <summary>
        /// 销毁Stage
        /// </summary>
        public void Destroy()
        {
            ASLogger.Instance.Info($"Stage: 销毁 {_stageName}");
            
            // 清理所有单位视图
            foreach (var unitView in _unitViews.Values)
            {
                if (unitView != null)
                {
                    UnityEngine.Object.Destroy(unitView);
                }
            }
            _unitViews.Clear();
            
            // 清理所有实体视图
            foreach (var entityView in _entityViews.Values)
            {
                if (entityView != null)
                {
                    entityView.Destroy();
                }
            }
            _entityViews.Clear();
            
            // 销毁Stage根对象
            if (_stageRoot != null)
            {
                UnityEngine.Object.Destroy(_stageRoot);
                _stageRoot = null;
            }
            
            _isLoaded = false;
            _isActive = false;
            OnStageUnloaded?.Invoke();
        }
    }
}
