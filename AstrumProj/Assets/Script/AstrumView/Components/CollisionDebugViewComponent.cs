using UnityEngine;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.Physics;
using Astrum.CommonBase;

namespace Astrum.View.Components
{
    /// <summary>
    /// 碰撞盒调试可视化组件
    /// 在Scene视图中通过Gizmos显示实体碰撞盒和技能攻击盒
    /// </summary>
    public class CollisionDebugViewComponent : ViewComponent
    {
        // 可视化开关
        public bool ShowEntityCollision = true;      // 显示实体碰撞盒
        public bool ShowAttackBox = true;            // 显示攻击碰撞盒
        public bool ShowLabels = true;               // 显示文本标签
        
        // 颜色配置
        public Color EntityCollisionColor = new Color(0f, 1f, 0f, 0.3f);  // 绿色半透明
        public Color AttackBoxColor = new Color(1f, 0f, 0f, 0.5f);        // 红色半透明
        public Color LabelColor = Color.white;
        
        private MonoBehaviour _monoBehaviour;
        
        protected override void OnInitialize()
        {
            ASLogger.Instance.Debug($"CollisionDebugViewComponent.OnInitialize: EntityId={OwnerEntity?.UniqueId}");
            
            // 添加MonoBehaviour用于Gizmos绘制
            if (_gameObject != null)
            {
                _monoBehaviour = _gameObject.AddComponent<CollisionGizmosDrawer>();
                var drawer = _monoBehaviour as CollisionGizmosDrawer;
                if (drawer != null)
                {
                    drawer.DebugComponent = this;
                }
            }
        }
        
        protected override void OnDestroy()
        {
            ASLogger.Instance.Debug($"CollisionDebugViewComponent.OnDestroy: EntityId={OwnerEntity?.UniqueId}");
            
            if (_monoBehaviour != null)
            {
                UnityEngine.Object.Destroy(_monoBehaviour);
            }
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            // 不需要每帧更新，Gizmos在OnDrawGizmos中绘制
        }
        
        protected override void OnSyncData(object data)
        {
            // 碰撞盒可视化不需要同步数据
        }
        
        /// <summary>
        /// 绘制实体碰撞盒
        /// </summary>
        public void DrawEntityCollisionGizmos()
        {
            if (!ShowEntityCollision || OwnerEntity == null) return;
            
            var collisionComp = OwnerEntity.GetComponent<CollisionComponent>();
            var positionComp = OwnerEntity.GetComponent<TransComponent>();
            
            if (collisionComp == null || positionComp == null || collisionComp.Shapes == null) return;
            
            Gizmos.color = EntityCollisionColor;
            
            foreach (var shape in collisionComp.Shapes)
            {
                DrawCollisionShape(shape, positionComp.Position, positionComp.Rotation);
            }
        }
        
        /// <summary>
        /// 绘制攻击碰撞盒
        /// </summary>
        public void DrawAttackBoxGizmos()
        {
            if (!ShowAttackBox || OwnerEntity == null) return;
            
            var actionComp = OwnerEntity.GetComponent<ActionComponent>();
            var positionComp = OwnerEntity.GetComponent<TransComponent>();
            
            if (actionComp == null || positionComp == null) return;
            
            // 检查当前动作是否是技能动作
            if (!(actionComp.CurrentAction is SkillActionInfo skillAction)) return;
            
            // 获取当前帧的触发信息
            if (skillAction.TriggerEffects == null) return;
            
            Gizmos.color = AttackBoxColor;
            
            foreach (var trigger in skillAction.TriggerEffects)
            {
                // 只显示碰撞类型的触发帧
                if (trigger.TriggerType != "Collision") continue;
                if (!trigger.CollisionShape.HasValue) continue;
                
                var shape = trigger.CollisionShape.Value;
                
                // 如果当前帧接近触发帧（前后2帧），高亮显示
                bool isNearTriggerFrame = Mathf.Abs(actionComp.CurrentFrame - trigger.Frame) <= 2;
                if (isNearTriggerFrame)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, 0.8f); // 黄色高亮
                }
                
                DrawCollisionShape(shape, positionComp.Position, positionComp.Rotation);
                
                // 绘制标签（考虑旋转后的世界位置）
                if (ShowLabels && isNearTriggerFrame)
                {
                    Vector3 worldPos = TSVectorToVector3(positionComp.Position + positionComp.Rotation * shape.LocalOffset);
                    DrawLabel(worldPos, $"Frame {trigger.Frame}\nEffect {trigger.EffectId}");
                }
                
                Gizmos.color = AttackBoxColor; // 恢复颜色
            }
        }
        
        /// <summary>
        /// 绘制碰撞形状
        /// </summary>
        private void DrawCollisionShape(CollisionShape shape, TrueSync.TSVector entityPos, TrueSync.TSQuaternion entityRot)
        {
            Vector3 worldCenter = TSVectorToVector3(entityPos + shape.LocalOffset);
            Quaternion worldRotation = TSQuaternionToQuaternion(entityRot * shape.LocalRotation);
            
            switch (shape.ShapeType)
            {
                case HitBoxShape.Box:
                    DrawBox(worldCenter, worldRotation, TSVectorToVector3(shape.HalfSize) * 2f);
                    break;
                    
                case HitBoxShape.Sphere:
                    Gizmos.DrawWireSphere(worldCenter, (float)shape.Radius);
                    break;
                    
                case HitBoxShape.Capsule:
                    DrawCapsule(worldCenter, worldRotation, (float)shape.Radius, (float)shape.Height);
                    break;
            }
        }
        
        /// <summary>
        /// 绘制盒体
        /// </summary>
        private void DrawBox(Vector3 center, Quaternion rotation, Vector3 size)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = oldMatrix;
        }
        
        /// <summary>
        /// 绘制胶囊体
        /// </summary>
        private void DrawCapsule(Vector3 center, Quaternion rotation, float radius, float height)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            
            float cylinderHeight = Mathf.Max(0, height - radius * 2);
            Vector3 top = Vector3.up * (cylinderHeight * 0.5f + radius);
            Vector3 bottom = Vector3.down * (cylinderHeight * 0.5f + radius);
            
            // 绘制上下半球
            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(bottom, radius);
            
            // 绘制连接线
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(bottom + offset, top + offset);
            }
            
            Gizmos.matrix = oldMatrix;
        }
        
        /// <summary>
        /// 绘制标签
        /// </summary>
        private void DrawLabel(Vector3 position, string text)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(position, text, new GUIStyle
            {
                normal = new GUIStyleState { textColor = LabelColor },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            });
#endif
        }
        
        /// <summary>
        /// TSVector转换为Vector3
        /// </summary>
        private Vector3 TSVectorToVector3(TrueSync.TSVector v)
        {
            return new Vector3((float)v.x, (float)v.y, (float)v.z);
        }
        
        /// <summary>
        /// TSQuaternion转换为Quaternion
        /// </summary>
        private Quaternion TSQuaternionToQuaternion(TrueSync.TSQuaternion q)
        {
            return new Quaternion((float)q.x, (float)q.y, (float)q.z, (float)q.w);
        }
    }
}

