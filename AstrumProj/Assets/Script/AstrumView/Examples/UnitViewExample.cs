using UnityEngine;
using Astrum.CommonBase;
using Astrum.View.Core;
using Astrum.View.EntityViews;
using Astrum.View.Components;

namespace Astrum.View.Examples
{
    /// <summary>
    /// UnitView示例
    /// 展示UnitView如何使用_requiredComponents配置自动挂载视图组件
    /// </summary>
    public class UnitViewExample : MonoBehaviour
    {
        [Header("示例设置")]
        [SerializeField] private bool autoStart = false;
        [SerializeField] private string unitType = "player";
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;
        
        private UnitView testUnitView;
        
        private void Start()
        {
            if (autoStart)
            {
                StartExample();
            }
        }
        
        private void OnDestroy()
        {
            CleanupExample();
        }
        
        /// <summary>
        /// 开始示例
        /// </summary>
        [ContextMenu("开始示例")]
        public void StartExample()
        {
            ASLogger.Instance.Info("UnitViewExample: 开始UnitView示例");
            
            try
            {
                // 创建测试UnitView
                CreateTestUnitView();
                
                // 测试组件功能
                TestComponentFunctionality();
                
                ASLogger.Instance.Info("UnitViewExample: 示例设置完成");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"UnitViewExample: 示例启动失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建测试UnitView
        /// </summary>
        private void CreateTestUnitView()
        {
            ASLogger.Instance.Info("UnitViewExample: 创建测试UnitView");
            
            // 使用EntityViewFactory创建UnitView
            testUnitView = EntityViewFactory.Instance.CreateEntityView("unit", 1) as UnitView;
            
            if (testUnitView != null)
            {
                // 设置单位类型
                testUnitView.SetUnitType(unitType);
                
                // 设置初始位置
                testUnitView.SetInitialPosition(spawnPosition);
                
                // 设置移动速度
                testUnitView.SetMoveSpeed(5f);
                testUnitView.SetRotationSpeed(180f);
                
                ASLogger.Instance.Info($"UnitViewExample: UnitView创建成功，ID: {testUnitView.EntityId}");
            }
            else
            {
                ASLogger.Instance.Error("UnitViewExample: UnitView创建失败");
            }
        }
        
        /// <summary>
        /// 测试组件功能
        /// </summary>
        private void TestComponentFunctionality()
        {
            if (testUnitView == null) return;
            
            ASLogger.Instance.Info("UnitViewExample: 测试组件功能");
            
            // 检查自动挂载的组件
            CheckRequiredComponents();
            
            // 测试移动组件
            TestMovementComponent();
            
            // 测试血量组件
            TestHealthComponent();
            
            // 测试动画功能
            TestAnimationFunctionality();
        }
        
        /// <summary>
        /// 检查必需的组件
        /// </summary>
        private void CheckRequiredComponents()
        {
            ASLogger.Instance.Info("UnitViewExample: 检查必需的组件");
            
            // 获取UnitView需要的组件类型
            var requiredComponents = testUnitView.GetRequiredViewComponentTypes();
            ASLogger.Instance.Info($"UnitView需要的组件类型数量: {requiredComponents.Length}");
            
            foreach (var componentType in requiredComponents)
            {
                ASLogger.Instance.Info($"  - {componentType.Name}");
            }
            
            // 检查实际挂载的组件
            var viewComponents = testUnitView.ViewComponents;
            ASLogger.Instance.Info($"实际挂载的组件数量: {viewComponents.Count}");
            
            foreach (var component in viewComponents)
            {
                ASLogger.Instance.Info($"  - {component.GetComponentTypeName()} (ID: {component.ComponentId})");
            }
        }
        
        /// <summary>
        /// 测试移动组件
        /// </summary>
        private void TestMovementComponent()
        {
            ASLogger.Instance.Info("UnitViewExample: 测试移动组件");
            
            var movementComponent = testUnitView.GetViewComponent<TransViewComponent>();
            if (movementComponent != null)
            {
                // 设置目标位置
                Vector3 targetPosition = spawnPosition + new Vector3(5f, 0f, 5f);
                movementComponent.SetTargetPosition(targetPosition);
                
                ASLogger.Instance.Info($"UnitViewExample: 设置移动目标位置 {targetPosition}");
                
                // 延迟停止移动
                Invoke(nameof(StopMovement), 3f);
            }
            else
            {
                ASLogger.Instance.Warning("UnitViewExample: 未找到移动组件");
            }
        }
        
        /// <summary>
        /// 停止移动
        /// </summary>
        private void StopMovement()
        {
            var movementComponent = testUnitView.GetViewComponent<TransViewComponent>();
            if (movementComponent != null)
            {
                movementComponent.StopMovement();
                ASLogger.Instance.Info("UnitViewExample: 停止移动");
            }
        }
        
        /// <summary>
        /// 测试血量组件
        /// </summary>
        private void TestHealthComponent()
        {
            ASLogger.Instance.Info("UnitViewExample: 测试血量组件");
            
            var healthComponent = testUnitView.GetViewComponent<HealthViewComponent>();
            if (healthComponent != null)
            {
                // 设置初始血量
                healthComponent.SetHealth(100f, 100f);
                
                // 模拟受到伤害
                Invoke(nameof(SimulateDamage), 1f);
                
                // 模拟恢复血量
                Invoke(nameof(SimulateHeal), 4f);
                
                ASLogger.Instance.Info("UnitViewExample: 血量组件测试设置完成");
            }
            else
            {
                ASLogger.Instance.Warning("UnitViewExample: 未找到血量组件");
            }
        }
        
        /// <summary>
        /// 模拟受到伤害
        /// </summary>
        private void SimulateDamage()
        {
            var healthComponent = testUnitView.GetViewComponent<HealthViewComponent>();
            if (healthComponent != null)
            {
                float currentHealth = healthComponent.GetCurrentHealth();
                float damage = 30f;
                healthComponent.SetHealth(currentHealth - damage, healthComponent.GetMaxHealth());
                
                ASLogger.Instance.Info($"UnitViewExample: 模拟受到伤害 {damage}，当前血量: {healthComponent.GetCurrentHealth()}");
            }
        }
        
        /// <summary>
        /// 模拟恢复血量
        /// </summary>
        private void SimulateHeal()
        {
            var healthComponent = testUnitView.GetViewComponent<HealthViewComponent>();
            if (healthComponent != null)
            {
                float currentHealth = healthComponent.GetCurrentHealth();
                float healAmount = 20f;
                healthComponent.SetHealth(currentHealth + healAmount, healthComponent.GetMaxHealth());
                
                ASLogger.Instance.Info($"UnitViewExample: 模拟恢复血量 {healAmount}，当前血量: {healthComponent.GetCurrentHealth()}");
            }
        }
        
        /// <summary>
        /// 测试动画功能
        /// </summary>
        private void TestAnimationFunctionality()
        {
            ASLogger.Instance.Info("UnitViewExample: 测试动画功能");
            
            // 播放动画
            testUnitView.PlayAnimation("Idle");
            
            // 设置动画参数
            testUnitView.SetAnimationParameter("IsMoving", true);
            testUnitView.SetAnimationParameter("MoveSpeed", 1.0f);
            
            ASLogger.Instance.Info("UnitViewExample: 动画功能测试完成");
        }
        
        /// <summary>
        /// 显示当前状态
        /// </summary>
        [ContextMenu("显示当前状态")]
        public void ShowCurrentStatus()
        {
            ASLogger.Instance.Info("=== UnitView示例状态 ===");
            
            if (testUnitView != null)
            {
                ASLogger.Instance.Info($"UnitView: ID: {testUnitView.EntityId}, 类型: {testUnitView.GetUnitType()}");
                ASLogger.Instance.Info($"位置: {testUnitView.GetWorldPosition()}");
                ASLogger.Instance.Info($"移动速度: {testUnitView.GetMoveSpeed()}");
                ASLogger.Instance.Info($"旋转速度: {testUnitView.GetRotationSpeed()}");
                ASLogger.Instance.Info($"视图组件数量: {testUnitView.ViewComponents.Count}");
                
                // 显示移动组件状态
                var movementComponent = testUnitView.GetViewComponent<TransViewComponent>();
                if (movementComponent != null)
                {
                    ASLogger.Instance.Info($"移动组件: 正在移动: {movementComponent.IsMoving()}, 当前速度: {movementComponent.GetCurrentSpeed()}");
                }
                
                // 显示血量组件状态
                var healthComponent = testUnitView.GetViewComponent<HealthViewComponent>();
                if (healthComponent != null)
                {
                    ASLogger.Instance.Info($"血量组件: 当前血量: {healthComponent.GetCurrentHealth()}/{healthComponent.GetMaxHealth()}, 存活: {healthComponent.IsAlive()}");
                }
            }
            else
            {
                ASLogger.Instance.Info("UnitView: 未创建");
            }
        }
        
        /// <summary>
        /// 清理示例
        /// </summary>
        private void CleanupExample()
        {
            ASLogger.Instance.Info("UnitViewExample: 清理示例");
            
            if (testUnitView != null)
            {
                testUnitView.Destroy();
                testUnitView = null;
            }
        }
        
        /// <summary>
        /// 测试组件同步
        /// </summary>
        [ContextMenu("测试组件同步")]
        public void TestComponentSync()
        {
            if (testUnitView == null) return;
            
            ASLogger.Instance.Info("UnitViewExample: 测试组件同步");
            
            // 同步移动数据
            var movementComponent = testUnitView.GetViewComponent<TransViewComponent>();
            if (movementComponent != null)
            {
                var movementData = new MovementData(
                    new Vector3(10f, 0f, 10f),
                    Quaternion.Euler(0f, 45f, 0f),
                    true,
                    5f
                );
                movementComponent.SyncData(movementData);
                ASLogger.Instance.Info("UnitViewExample: 同步移动数据");
            }
            
            // 同步血量数据
            var healthComponent = testUnitView.GetViewComponent<HealthViewComponent>();
            if (healthComponent != null)
            {
                var healthData = new HealthData(80f, 100f, true);
                healthComponent.SyncData(healthData);
                ASLogger.Instance.Info("UnitViewExample: 同步血量数据");
            }
        }
        
        /// <summary>
        /// 测试动态添加组件
        /// </summary>
        [ContextMenu("测试动态添加组件")]
        public void TestDynamicAddComponent()
        {
            if (testUnitView == null) return;
            
            ASLogger.Instance.Info("UnitViewExample: 测试动态添加组件");
            
            // 动态添加一个自定义组件
            var customComponent = new CustomViewComponent();
            testUnitView.AddViewComponent(customComponent);
            
            ASLogger.Instance.Info($"UnitViewExample: 动态添加组件完成，当前组件数量: {testUnitView.ViewComponents.Count}");
        }
    }
    
    /// <summary>
    /// 自定义视图组件示例
    /// </summary>
    public class CustomViewComponent : ViewComponent
    {
        protected override void OnInitialize()
        {
            ASLogger.Instance.Info($"CustomViewComponent: 初始化自定义组件，ID: {_componentId}");
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            // 自定义更新逻辑
        }
        
        protected override void OnDestroy()
        {
            ASLogger.Instance.Info($"CustomViewComponent: 销毁自定义组件，ID: {_componentId}");
        }
        
        protected override void OnSyncData(object data)
        {
            // 自定义数据同步逻辑
        }
    }
} 