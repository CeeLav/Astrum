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

