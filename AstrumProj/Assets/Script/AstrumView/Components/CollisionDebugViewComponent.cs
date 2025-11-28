using UnityEngine;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.Physics;
using Astrum.CommonBase;
using TrueSync;

namespace Astrum.View.Components
{
    /// <summary>
    /// 碰撞盒调试可视化组件
    /// 在Scene视图中通过Gizmos显示实体碰撞盒、技能攻击盒和子弹轨迹
    /// </summary>
    public class CollisionDebugViewComponent : ViewComponent
    {
        // 可视化开关
        public bool ShowEntityCollision = true;      // 显示实体碰撞盒
        public bool ShowAttackBox = true;            // 显示攻击碰撞盒
        public bool ShowProjectileTrajectory = true; // 显示子弹轨迹
        public bool ShowLabels = true;               // 显示文本标签
        
        // 颜色配置
        public Color EntityCollisionColor = new Color(0f, 1f, 0f, 0.3f);  // 绿色半透明
        public Color AttackBoxColor = new Color(1f, 0f, 0f, 0.5f);        // 红色半透明
        public Color ProjectileTrajectoryColor = new Color(1f, 1f, 0f, 0.8f); // 黄色
        public Color ProjectileRaycastColor = new Color(0f, 1f, 1f, 0.8f);    // 青色
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
            
            // 尝试从物理世界获取实际位置（碰撞体中心）
            var world = OwnerEntity.World;
            var physicsPos = world?.HitSystem?.PhysicsWorld?.GetPhysicsPosition(OwnerEntity);
            var physicsRot = world?.HitSystem?.PhysicsWorld?.GetPhysicsRotation(OwnerEntity);
            var physicsBounds = world?.HitSystem?.PhysicsWorld?.GetPhysicsBoundingBox(OwnerEntity);

            if (physicsBounds.HasValue)
            {
                DrawPhysicsBoundingBoxGizmo(physicsBounds.Value.Min, physicsBounds.Value.Max);
            }
            
            // 如果物理世界位置与逻辑位置不一致，显示警告
            if (physicsPos.HasValue && collisionComp.Shapes.Count > 0)
            {
                // 计算逻辑层的碰撞体中心位置（应用偏移）
                var shape = collisionComp.Shapes[0];
                var logicTransform = shape.ToWorldTransform(positionComp.Position, positionComp.Rotation);
                var logicCollisionCenter = logicTransform.WorldCenter;
                
                // 对比物理世界的碰撞体中心 vs 逻辑层的碰撞体中心
                var posDiff = (physicsPos.Value - logicCollisionCenter).magnitude;
                if (posDiff > TrueSync.FP.FromFloat(0.01f))
                {
                    // 绘制物理世界的碰撞体（红色，表示不同步）
                    Gizmos.color = Color.red;
                    Vector3 physicsCenter = TSVectorToVector3(physicsPos.Value);
                    Gizmos.DrawWireSphere(physicsCenter, 0.15f);
                    Gizmos.DrawSphere(physicsCenter, 0.05f);
                    
                    // 绘制逻辑层的碰撞体（黄色半透明，表示预期位置）
                    Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                    foreach (var s in collisionComp.Shapes)
                    {
                        DrawCollisionShape(s, positionComp.Position, positionComp.Rotation);
                    }
                    
#if UNITY_EDITOR
                    // 显示警告标签
                    Vector3 logicPos = TSVectorToVector3(logicCollisionCenter);
                    UnityEditor.Handles.Label(logicPos + Vector3.up * 0.5f, 
                        $"⚠ Physics Desync: {(float)posDiff:F3}m\nPhysics: {physicsCenter:F2}\nLogic: {logicPos:F2}", 
                        new GUIStyle { normal = new GUIStyleState { textColor = Color.red }, fontSize = 12, fontStyle = FontStyle.Bold });
#endif
                    return;
                }
            }
            
            // 正常绘制（使用逻辑层位置）
            Gizmos.color = EntityCollisionColor;
            foreach (var shape in collisionComp.Shapes)
            {
                // 使用物理世界的旋转（如果可用），否则使用逻辑层旋转
                var rotation = physicsRot ?? positionComp.Rotation;
                DrawCollisionShape(shape, positionComp.Position, rotation);
            }
        }

        private void DrawPhysicsBoundingBoxGizmo(TSVector min, TSVector max)
        {
            var originalColor = Gizmos.color;
            Vector3 minVec = TSVectorToVector3(min);
            Vector3 maxVec = TSVectorToVector3(max);
            Vector3 center = (minVec + maxVec) * 0.5f;
            Vector3 size = maxVec - minVec;

            Gizmos.color = new Color(0f, 0.6f, 1f, 1f);
            Gizmos.DrawWireCube(center, size);
            Gizmos.color = new Color(0f, 0.6f, 1f, 0.1f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = originalColor;
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
            var actionInfo = actionComp.CurrentAction;
            if (actionInfo?.SkillExtension == null) return;
            
            // 获取当前帧的触发信息
            var triggerEffects = actionInfo.GetTriggerEffects();
            if (triggerEffects == null || triggerEffects.Count == 0) return;
            
            Gizmos.color = AttackBoxColor;
            
            foreach (var trigger in triggerEffects)
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
                
                // 绘制标签（使用统一转换方法）
                if (ShowLabels && isNearTriggerFrame)
                {
                    var worldTransform = shape.ToWorldTransform(positionComp.Position, positionComp.Rotation);
                    Vector3 worldPos = TSVectorToVector3(worldTransform.WorldCenter);
                    string effectText = trigger.EffectIds != null && trigger.EffectIds.Length > 0
                        ? $"Effects [{string.Join(",", trigger.EffectIds)}]"
                        : "No Effects";
                    DrawLabel(worldPos, $"Frame {trigger.Frame}\n{effectText}");
                }
                
                Gizmos.color = AttackBoxColor; // 恢复颜色
            }
        }

        /// <summary>
        /// 绘制子弹轨迹
        /// </summary>
        public void DrawProjectileTrajectoryGizmos()
        {
            if (!ShowProjectileTrajectory) return;
            
            if (OwnerEntity == null)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(Vector3.zero, "OwnerEntity is null", new GUIStyle { normal = new GUIStyleState { textColor = Color.red } });
#endif
                return;
            }

            var projectileComp = OwnerEntity.GetComponent<ProjectileComponent>();
            var transComp = OwnerEntity.GetComponent<TransComponent>();

            if (projectileComp == null)
            {
#if UNITY_EDITOR
                var trans = OwnerEntity.GetComponent<TransComponent>();
                if (trans != null)
                {
                    Vector3 pos = new Vector3((float)trans.Position.x, (float)trans.Position.y, (float)trans.Position.z);
                    UnityEditor.Handles.Label(pos, $"Entity {OwnerEntity.UniqueId}: No ProjectileComponent", 
                        new GUIStyle { normal = new GUIStyleState { textColor = Color.red }, fontSize = 14 });
                }
#endif
                return;
            }
            
            if (transComp == null)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(Vector3.zero, $"Entity {OwnerEntity.UniqueId}: No TransComponent", 
                    new GUIStyle { normal = new GUIStyleState { textColor = Color.red }, fontSize = 14 });
#endif
                return;
            }

            Vector3 currentPos = TSVectorToVector3(transComp.Position);
            Vector3 lastPos = TSVectorToVector3(projectileComp.LastPosition);
            Vector3 initialPos = TSVectorToVector3(projectileComp.InitialPosition);
            Vector3 velocity = TSVectorToVector3(projectileComp.CurrentVelocity);
            Vector3 launchDir = TSVectorToVector3(projectileComp.LaunchDirection);

            // 绘制完整轨迹：从初始位置到当前位置
            float totalDistance = Vector3.Distance(initialPos, currentPos);
            if (totalDistance > 0.0001f)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f); // 橙色半透明
                Gizmos.DrawLine(initialPos, currentPos);
                
                // 绘制初始发射位置标记
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(initialPos, 0.12f);
            }

            // 绘制当前位置标记（最显眼的，确保能看到）
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentPos, 0.15f);
            Gizmos.DrawSphere(currentPos, 0.05f);

            // 绘制当前帧检测射线（上一帧到当前帧）
            float frameDistance = Vector3.Distance(currentPos, lastPos);
            if (frameDistance > 0.0001f)
            {
                Gizmos.color = ProjectileRaycastColor; // 青色
                Gizmos.DrawLine(lastPos, currentPos);
                
                // 在射线中点绘制一个小球
                Vector3 midPoint = (lastPos + currentPos) * 0.5f;
                Gizmos.DrawWireSphere(midPoint, 0.08f);
                
                // 绘制上一帧位置标记
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Gizmos.DrawWireSphere(lastPos, 0.1f);
            }

            // 绘制当前速度方向
            if (velocity.sqrMagnitude > 0.0001f)
            {
                Gizmos.color = ProjectileTrajectoryColor;
                Vector3 velocityEnd = currentPos + velocity.normalized * 0.8f;
                Gizmos.DrawLine(currentPos, velocityEnd);
                
                // 绘制箭头
                DrawArrow(currentPos, velocityEnd, 0.15f);
            }
            else if (launchDir.sqrMagnitude > 0.0001f)
            {
                // 如果没有速度，显示发射方向
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // 橙色
                Vector3 dirEnd = currentPos + launchDir.normalized * 0.8f;
                Gizmos.DrawLine(currentPos, dirEnd);
                DrawArrow(currentPos, dirEnd, 0.15f);
            }

            // 绘制标签
            if (ShowLabels)
            {
                string info = $"Projectile {projectileComp.ProjectileId}\n" +
                             $"Frame: {projectileComp.ElapsedFrames}/{projectileComp.LifeTime}\n" +
                             $"Pos: {currentPos:F2}\n" +
                             $"Speed: {velocity.magnitude:F2}\n" +
                             $"Total Dist: {totalDistance:F2}\n" +
                             $"Frame Dist: {frameDistance:F3}\n" +
                             $"Pierce: {projectileComp.PiercedCount}/{projectileComp.PierceCount}";
                DrawLabel(currentPos + Vector3.up * 0.3f, info);
            }
        }
        
        /// <summary>
        /// 绘制碰撞形状
        /// </summary>
        private void DrawCollisionShape(CollisionShape shape, TrueSync.TSVector entityPos, TrueSync.TSQuaternion entityRot)
        {
            // 【统一坐标转换】使用 CollisionShape.ToWorldTransform
            var worldTransform = shape.ToWorldTransform(entityPos, entityRot);
            Vector3 worldCenter = TSVectorToVector3(worldTransform.WorldCenter);
            Quaternion worldRotation = TSQuaternionToQuaternion(worldTransform.WorldRotation);
            
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

                case HitBoxShape.Cylinder:
                    DrawCylinder(worldCenter, worldRotation, (float)shape.Radius, (float)shape.Height);
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
        /// 绘制圆柱体
        /// </summary>
        private void DrawCylinder(Vector3 center, Quaternion rotation, float radius, float height)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);

            float halfHeight = height * 0.5f;
            const int segments = 24;

            Vector3 prevTop = Vector3.zero;
            Vector3 prevBottom = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                Vector3 top = new Vector3(x, halfHeight, z);
                Vector3 bottom = new Vector3(x, -halfHeight, z);

                if (i > 0)
                {
                    Gizmos.DrawLine(prevTop, top);
                    Gizmos.DrawLine(prevBottom, bottom);
                }

                    Gizmos.DrawLine(top, bottom);

                prevTop = top;
                prevBottom = bottom;
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

        /// <summary>
        /// 绘制箭头（用于表示方向）
        /// </summary>
        private void DrawArrow(Vector3 start, Vector3 end, float arrowHeadLength = 0.1f)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            if (right.sqrMagnitude < 0.01f) // 如果方向接近垂直，使用另一个轴
            {
                right = Vector3.Cross(Vector3.right, direction).normalized;
            }

            float arrowAngle = 20f;
            Vector3 arrowTip1 = end - Quaternion.AngleAxis(arrowAngle, right) * direction * arrowHeadLength;
            Vector3 arrowTip2 = end - Quaternion.AngleAxis(-arrowAngle, right) * direction * arrowHeadLength;

            Gizmos.DrawLine(end, arrowTip1);
            Gizmos.DrawLine(end, arrowTip2);
        }
    }
}

