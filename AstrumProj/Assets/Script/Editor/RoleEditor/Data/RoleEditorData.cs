using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// 角色编辑器数据模型（合并 EntityBaseTable 和 RoleBaseTable）
    /// 使用 Odin Inspector 特性
    /// </summary>
    [CreateAssetMenu(fileName = "RoleData", menuName = "Astrum/Role Editor Data")]
    public class RoleEditorData : ScriptableObject
    {
        // ===== EntityBaseTable 数据 =====
        
        [TitleGroup("实体配置")]
        [LabelText("实体ID"), ReadOnly]
        [InfoBox("实体ID和角色ID保持一致", InfoMessageType.Info)]
        public int EntityId;
        
        [TitleGroup("实体配置")]
        [LabelText("原型名称")]
        [ValueDropdown("GetArchetypeOptions")]
        public string ArchetypeName = "RoleArchetype";
        
        [TitleGroup("实体配置/模型配置")]
        [LabelText("模型名称")]
        public string ModelName = "";
        
        [TitleGroup("实体配置/模型配置")]
        [LabelText("模型路径")]
        [FolderPath(AbsolutePath = false, ParentFolder = "Assets/ArtRes")]
        [OnValueChanged("OnModelPathChanged")]
        public string ModelPath = "";
        
        [TitleGroup("实体配置/模型配置")]
        [LabelText("模型预览"), ReadOnly]
        [PreviewField(100, ObjectFieldAlignment.Center)]
        [HideLabel]
        public GameObject ModelPrefab;
        
        [TitleGroup("实体配置/基础动作配置")]
        [LabelText("静止动作ID")]
        public int IdleAction = 0;
        
        [TitleGroup("实体配置/基础动作配置")]
        [LabelText("行走动作ID")]
        public int WalkAction = 0;
        
        [TitleGroup("实体配置/基础动作配置")]
        [LabelText("奔跑动作ID")]
        public int RunAction = 0;
        
        [TitleGroup("实体配置/基础动作配置")]
        [LabelText("跳跃动作ID")]
        public int JumpAction = 0;
        
        [TitleGroup("实体配置/基础动作配置")]
        [LabelText("出生动作ID")]
        public int BirthAction = 0;
        
        [TitleGroup("实体配置/基础动作配置")]
        [LabelText("死亡动作ID")]
        public int DeathAction = 0;
        
        [TitleGroup("实体配置/物理碰撞")]
        [LabelText("碰撞盒数据")]
        [MultiLineProperty(3)]
        [InfoBox("格式: Capsule:偏移x,y,z:旋转x,y,z,w:半径:高度\n例如: Capsule:0,1,0:0,0,0,1:0.4:2.0", InfoMessageType.None)]
        public string CollisionData = "";
        
        [TitleGroup("实体配置/物理碰撞")]
        [LabelText("半径缩放"), Range(0.3f, 1.0f)]
        [InfoBox("调整碰撞盒半径：0.3=极窄 | 0.5=标准 | 0.7=宽松 | 1.0=完全包裹", InfoMessageType.None)]
        [OnValueChanged("OnRadiusScaleChanged")]
        public float RadiusScale = 0.5f;
        
        [TitleGroup("实体配置/物理碰撞")]
        [Button("生成胶囊体碰撞盒", ButtonSizes.Medium)]
        [EnableIf("@ModelPrefab != null")]
        [InfoBox("必须先配置模型", InfoMessageType.Warning, "@ModelPrefab == null")]
        private void GenerateCapsuleCollision()
        {
            if (ModelPrefab == null)
            {
                UnityEngine.Debug.LogWarning("请先配置模型！");
                return;
            }
            
            CollisionData = Services.CollisionShapeGenerator.GenerateCapsuleFromModel(ModelPrefab, RadiusScale);
            MarkDirty();
            
            if (!string.IsNullOrEmpty(CollisionData))
            {
                UnityEngine.Debug.Log($"[{ModelName}] 已生成碰撞盒: {CollisionData}");
            }
        }
        
        // ===== RoleBaseTable 数据 =====
        
        [TitleGroup("角色配置")]
        [LabelText("角色ID"), ReadOnly]
        public int RoleId;
        
        [TitleGroup("角色配置")]
        [LabelText("角色名称")]
        public string RoleName = "";
        
        [TitleGroup("角色配置")]
        [LabelText("角色描述"), TextArea(2, 3)]
        public string RoleDescription = "";
        
        [TitleGroup("角色配置")]
        [LabelText("角色类型"), EnumToggleButtons]
        public RoleTypeEnum RoleType = RoleTypeEnum.近战平衡;
        
        [TitleGroup("角色配置/基础属性")]
        [LabelText("基础攻击力"), Range(0, 500)]
        public float BaseAttack = 50f;
        
        [TitleGroup("角色配置/基础属性")]
        [LabelText("基础防御力"), Range(0, 500)]
        public float BaseDefense = 50f;
        
        [TitleGroup("角色配置/基础属性")]
        [LabelText("基础生命值"), Range(0, 5000)]
        public float BaseHealth = 1000f;
        
        [TitleGroup("角色配置/基础属性")]
        [LabelText("基础速度"), Range(0, 20)]
        public float BaseSpeed = 5f;
        
        [TitleGroup("角色配置/成长属性")]
        [LabelText("攻击力成长"), Range(0, 50)]
        public float AttackGrowth = 5f;
        
        [TitleGroup("角色配置/成长属性")]
        [LabelText("防御力成长"), Range(0, 50)]
        public float DefenseGrowth = 5f;
        
        [TitleGroup("角色配置/成长属性")]
        [LabelText("生命值成长"), Range(0, 500)]
        public float HealthGrowth = 100f;
        
        [TitleGroup("角色配置/成长属性")]
        [LabelText("速度成长"), Range(0, 5)]
        public float SpeedGrowth = 0.1f;
        
        [TitleGroup("角色配置/技能槽位")]
        [LabelText("轻击技能ID")]
        public int LightAttackSkillId = 0;
        
        [TitleGroup("角色配置/技能槽位")]
        [LabelText("重击技能ID")]
        public int HeavyAttackSkillId = 0;
        
        [TitleGroup("角色配置/技能槽位")]
        [LabelText("技能槽1 ID")]
        public int Skill1Id = 0;
        
        [TitleGroup("角色配置/技能槽位")]
        [LabelText("技能槽2 ID")]
        public int Skill2Id = 0;
        
        // ===== 编辑器辅助字段 =====
        
        [HideInInspector]
        public bool IsNew = false;
        
        [HideInInspector]
        public bool IsDirty = false;
        
        [HideInInspector]
        public List<string> ValidationErrors = new List<string>();
        
        // ===== 方法 =====
        
        public static RoleEditorData CreateDefault(int id)
        {
            var data = CreateInstance<RoleEditorData>();
            data.EntityId = id;
            data.RoleId = id;
            data.ArchetypeName = "RoleArchetype";
            data.ModelName = $"Role_{id}";
            data.RoleName = $"NewRole_{id}";
            data.RoleDescription = "新角色";
            data.IsNew = true;
            data.IsDirty = true;
            return data;
        }
        
        public RoleEditorData Clone()
        {
            var clone = CreateInstance<RoleEditorData>();
            
            // 复制EntityBaseTable字段
            clone.EntityId = this.EntityId;
            clone.ArchetypeName = this.ArchetypeName;
            clone.ModelName = this.ModelName;
            clone.ModelPath = this.ModelPath;
            clone.ModelPrefab = this.ModelPrefab;
            clone.IdleAction = this.IdleAction;
            clone.WalkAction = this.WalkAction;
            clone.RunAction = this.RunAction;
            clone.JumpAction = this.JumpAction;
            clone.BirthAction = this.BirthAction;
            clone.DeathAction = this.DeathAction;
            clone.CollisionData = this.CollisionData;
            clone.RadiusScale = this.RadiusScale;
            
            // 复制RoleBaseTable字段
            clone.RoleId = this.RoleId;
            clone.RoleName = this.RoleName;
            clone.RoleDescription = this.RoleDescription;
            clone.RoleType = this.RoleType;
            clone.BaseAttack = this.BaseAttack;
            clone.BaseDefense = this.BaseDefense;
            clone.BaseHealth = this.BaseHealth;
            clone.BaseSpeed = this.BaseSpeed;
            clone.AttackGrowth = this.AttackGrowth;
            clone.DefenseGrowth = this.DefenseGrowth;
            clone.HealthGrowth = this.HealthGrowth;
            clone.SpeedGrowth = this.SpeedGrowth;
            clone.LightAttackSkillId = this.LightAttackSkillId;
            clone.HeavyAttackSkillId = this.HeavyAttackSkillId;
            clone.Skill1Id = this.Skill1Id;
            clone.Skill2Id = this.Skill2Id;
            
            // 编辑器状态
            clone.ValidationErrors = new List<string>();
            clone.IsNew = true;
            clone.IsDirty = true;
            
            return clone;
        }
        
        public void MarkDirty()
        {
            IsDirty = true;
        }
        
        public void ClearDirty()
        {
            IsDirty = false;
            IsNew = false;
        }
        
        // ===== Odin 回调 =====
        
        private void OnModelPathChanged()
        {
            if (!string.IsNullOrEmpty(ModelPath))
            {
                ModelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            }
            else
            {
                ModelPrefab = null;
            }
        }
        
        private void OnRadiusScaleChanged()
        {
            // 半径缩放改变时，如果已有碰撞数据，提示重新生成
            if (!string.IsNullOrEmpty(CollisionData) && ModelPrefab != null)
            {
                UnityEngine.Debug.Log($"[{ModelName}] 半径缩放已改变为 {RadiusScale:F2}，建议点击\"生成胶囊体碰撞盒\"按钮重新生成");
            }
        }
        
        private IEnumerable<string> GetArchetypeOptions()
        {
            yield return "RoleArchetype";
            yield return "MonsterArchetype";
            yield return "NPCArchetype";
        }
    }
    
    /// <summary>
    /// 角色类型枚举
    /// </summary>
    public enum RoleTypeEnum
    {
        近战平衡 = 1,
        远程机动 = 2,
        控场AOE = 3,
        近战爆发 = 4,
        高机动连击 = 5
    }
}

