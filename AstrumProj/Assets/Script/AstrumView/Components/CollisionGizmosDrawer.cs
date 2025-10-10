using UnityEngine;

namespace Astrum.View.Components
{
    /// <summary>
    /// Gizmos绘制MonoBehaviour
    /// Unity要求OnDrawGizmos必须在MonoBehaviour中，且类名必须与文件名一致
    /// </summary>
    public class CollisionGizmosDrawer : MonoBehaviour
    {
        public CollisionDebugViewComponent DebugComponent;
        
        private void OnDrawGizmos()
        {
            if (DebugComponent == null) return;
            
            // 绘制实体碰撞盒
            DebugComponent.DrawEntityCollisionGizmos();
            
            // 绘制攻击碰撞盒
            DebugComponent.DrawAttackBoxGizmos();
        }
        
        private void OnDrawGizmosSelected()
        {
            // 当选中GameObject时也绘制
            OnDrawGizmos();
        }
    }
}

