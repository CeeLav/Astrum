using System;
using UnityEngine;
using Astrum.CommonBase;
using Astrum.View.Components;
using Astrum.View.Core;

namespace Astrum.View.EntityViews
{
    /// <summary>
    /// 单位视图类，继承自EntityView，代表游戏中可移动单位的视觉表现
    /// </summary>
    public class UnitView : EntityView
    {
        private static readonly Type[] _requiredViewComponents = new Type[]
        {
            typeof(TransViewComponent),
            typeof(HealthViewComponent),
            typeof(ModelViewComponent),
        };
        
        // 单位特定属性
        private string _unitType = "default";
        private float _moveSpeed = 5f;
        private float _rotationSpeed = 180f;
        
        // 渲染组件
        private Renderer _renderer;
        private Animator _animator;
        private Collider _collider;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public UnitView() : base("unit")
        {
        }
        
        /// <summary>
        /// 获取UnitView需要的视图组件类型列表（静态缓存，避免重复分配）
        /// </summary>
        /// <returns>视图组件类型列表</returns>
        public override Type[] GetRequiredViewComponentTypes()
        {
            return _requiredViewComponents;
        }
        
        protected override void OnInitialize()
        {
            ASLogger.Instance.Info($"UnitView: 初始化单位视图，ID: {_entityId}");
            
            // 设置单位特定的初始化
            SetupUnitComponents();
            
            // 设置初始位置
            SetInitialPosition(Vector3.zero);
            
            ASLogger.Instance.Info($"UnitView: 单位视图初始化完成，ID: {_entityId}");
        }
        
        protected override void OnUpdateView(float deltaTime)
        {
            // 单位特定的更新逻辑
            UpdateUnitVisuals(deltaTime);
        }
        
        protected override void OnSyncWithEntity(long entityId)
        {
            // 与逻辑层Unit同步数据
            SyncUnitData(entityId);
        }
        
        /// <summary>
        /// 设置单位组件
        /// </summary>
        private void SetupUnitComponents()
        {
            if (_gameObject == null) return;
            
            // 获取或添加渲染器
            _renderer = _gameObject.GetComponent<Renderer>();
            if (_renderer == null)
            {
                _renderer = _gameObject.AddComponent<MeshRenderer>();
            }
            
            // 获取或添加动画器
            _animator = _gameObject.GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = _gameObject.AddComponent<Animator>();
            }
            
            // 获取或添加碰撞器
            _collider = _gameObject.GetComponent<Collider>();
            if (_collider == null)
            {
                _collider = _gameObject.AddComponent<CapsuleCollider>();
            }
            
            // 设置默认材质
            SetupDefaultMaterial();
        }
        
        /// <summary>
        /// 设置默认材质
        /// </summary>
        private void SetupDefaultMaterial()
        {
            if (_renderer != null)
            {
                // 创建默认材质
                Material defaultMaterial = new Material(Shader.Find("Standard"));
                defaultMaterial.color = Color.blue; // 默认蓝色
                _renderer.material = defaultMaterial;
            }
        }
        
        /// <summary>
        /// 设置单位初始位置
        /// </summary>
        /// <param name="position">初始位置</param>
        public void SetInitialPosition(Vector3 position)
        {
            if (_transform != null)
            {
                _transform.position = position;
                ASLogger.Instance.Info($"UnitView: 设置初始位置 {position}");
            }
        }
        
        /// <summary>
        /// 更新单位视觉效果
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateUnitVisuals(float deltaTime)
        {
            // 更新动画
            UpdateAnimations(deltaTime);
            
            // 更新特效
            UpdateEffects(deltaTime);
        }
        
        /// <summary>
        /// 更新动画
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateAnimations(float deltaTime)
        {
            if (_animator == null) return;
            
            // 获取移动组件
            var movementComponent = GetViewComponent<TransViewComponent>();
            if (movementComponent != null)
            {
                // 根据移动状态更新动画
                bool isMoving = movementComponent.IsMoving();
                float moveSpeed = movementComponent.GetCurrentSpeed();
                
                // 设置动画参数
                _animator.SetBool("IsMoving", isMoving);
                _animator.SetFloat("MoveSpeed", moveSpeed);
            }
        }
        
        /// <summary>
        /// 更新特效
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        private void UpdateEffects(float deltaTime)
        {
            // TODO: 实现特效更新逻辑
        }
        
        /// <summary>
        /// 与逻辑层Unit同步数据
        /// </summary>
        /// <param name="entityId">实体ID</param>
        private void SyncUnitData(long entityId)
        {
            // TODO: 从逻辑层获取Unit数据并同步
            // 这里需要实现与逻辑层的通信接口
            
            // 示例：同步位置数据
            SyncPositionData(entityId);
            
            // 示例：同步血量数据
            SyncHealthData(entityId);
        }
        
        /// <summary>
        /// 同步位置数据
        /// </summary>
        /// <param name="entityId">实体ID</param>
        private void SyncPositionData(long entityId)
        {
            // TODO: 从逻辑层获取位置数据
            // 示例代码：
            /*
            var positionComponent = LogicCore.EntityManager.GetComponent<PositionComponent>(entityId);
            if (positionComponent != null)
            {
                var movementComponent = GetViewComponent<MovementViewComponent>();
                if (movementComponent != null)
                {
                    Vector3 position = new Vector3(positionComponent.X, positionComponent.Y, positionComponent.Z);
                    movementComponent.SetTargetPosition(position);
                }
            }
            */
        }
        
        /// <summary>
        /// 同步血量数据
        /// </summary>
        /// <param name="entityId">实体ID</param>
        private void SyncHealthData(long entityId)
        {
            // TODO: 从逻辑层获取血量数据
            // 示例代码：
            /*
            var healthComponent = LogicCore.EntityManager.GetComponent<HealthComponent>(entityId);
            if (healthComponent != null)
            {
                var healthViewComponent = GetViewComponent<HealthViewComponent>();
                if (healthViewComponent != null)
                {
                    var healthData = new HealthData(healthComponent.CurrentHealth, healthComponent.MaxHealth, healthComponent.IsAlive);
                    healthViewComponent.SyncData(healthData);
                }
            }
            */
        }
        
        /// <summary>
        /// 设置单位类型
        /// </summary>
        /// <param name="unitType">单位类型</param>
        public void SetUnitType(string unitType)
        {
            _unitType = unitType;
            
            // 根据单位类型设置不同的外观
            SetupUnitAppearance(unitType);
        }
        
        /// <summary>
        /// 设置单位外观
        /// </summary>
        /// <param name="unitType">单位类型</param>
        private void SetupUnitAppearance(string unitType)
        {
            if (_renderer == null) return;
            
            Color unitColor = Color.blue; // 默认蓝色
            
            switch (unitType.ToLower())
            {
                case "player":
                    unitColor = Color.blue;
                    break;
                case "enemy":
                    unitColor = Color.red;
                    break;
                case "ally":
                    unitColor = Color.green;
                    break;
                case "neutral":
                    unitColor = Color.yellow;
                    break;
                default:
                    unitColor = Color.gray;
                    break;
            }
            
            _renderer.material.color = unitColor;
            ASLogger.Instance.Info($"UnitView: 设置单位外观，类型: {unitType}，颜色: {unitColor}");
        }
        
        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="animationName">动画名称</param>
        public void PlayAnimation(string animationName)
        {
            if (_animator != null)
            {
                _animator.Play(animationName);
                ASLogger.Instance.Info($"UnitView: 播放动画 {animationName}");
            }
        }
        
        /// <summary>
        /// 设置动画参数
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">参数值</param>
        public void SetAnimationParameter(string parameterName, float value)
        {
            if (_animator != null)
            {
                _animator.SetFloat(parameterName, value);
            }
        }
        
        /// <summary>
        /// 设置动画参数
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">参数值</param>
        public void SetAnimationParameter(string parameterName, bool value)
        {
            if (_animator != null)
            {
                _animator.SetBool(parameterName, value);
            }
        }
        
        /// <summary>
        /// 获取单位类型
        /// </summary>
        /// <returns>单位类型</returns>
        public string GetUnitType()
        {
            return _unitType;
        }
        
        /// <summary>
        /// 获取移动速度
        /// </summary>
        /// <returns>移动速度</returns>
        public float GetMoveSpeed()
        {
            return _moveSpeed;
        }
        
        /// <summary>
        /// 设置移动速度
        /// </summary>
        /// <param name="speed">移动速度</param>
        public void SetMoveSpeed(float speed)
        {
            _moveSpeed = speed;
            
            // 更新移动组件
            var movementComponent = GetViewComponent<TransViewComponent>();
            if (movementComponent != null)
            {
                // 这里可以通过反射或其他方式设置移动组件的速度
            }
        }
        
        /// <summary>
        /// 获取旋转速度
        /// </summary>
        /// <returns>旋转速度</returns>
        public float GetRotationSpeed()
        {
            return _rotationSpeed;
        }
        
        /// <summary>
        /// 设置旋转速度
        /// </summary>
        /// <param name="speed">旋转速度</param>
        public void SetRotationSpeed(float speed)
        {
            _rotationSpeed = speed;
        }
    }
}
