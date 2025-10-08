using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// 技能编辑器数据模型（合并 SkillTable、SkillActionTable 和 SkillEffectTable）
    /// 使用 Odin Inspector 特性
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "Astrum/Skill Editor Data")]
    public class SkillEditorData : ScriptableObject
    {
        // ===== SkillTable 数据 =====
        
        [TitleGroup("技能基本信息")]
        [LabelText("技能ID"), ReadOnly]
        [InfoBox("技能ID是唯一标识符，不可修改", InfoMessageType.Info)]
        public int SkillId;
        
        [TitleGroup("技能基本信息")]
        [LabelText("技能名称")]
        [ValidateInput("ValidateSkillName", "技能名称不能为空")]
        public string SkillName = "";
        
        [TitleGroup("技能基本信息")]
        [LabelText("技能描述"), TextArea(2, 4)]
        public string SkillDescription = "";
        
        [TitleGroup("技能基本信息")]
        [LabelText("技能类型"), EnumToggleButtons]
        [InfoBox("1=攻击, 2=控制, 3=位移, 4=buff", InfoMessageType.None)]
        public SkillTypeEnum SkillType = SkillTypeEnum.攻击;
        
        [TitleGroup("技能配置/等级与消耗")]
        [LabelText("学习所需等级"), Range(1, 100)]
        public int RequiredLevel = 1;
        
        [TitleGroup("技能配置/等级与消耗")]
        [LabelText("技能最大等级"), Range(1, 100)]
        public int MaxLevel = 10;
        
        [TitleGroup("技能配置/等级与消耗")]
        [LabelText("展示冷却时间(秒)"), Range(0, 300)]
        [InfoBox("用于UI显示的冷却时间", InfoMessageType.None)]
        public float DisplayCooldown = 5.0f;
        
        [TitleGroup("技能配置/等级与消耗")]
        [LabelText("展示法力消耗"), Range(0, 1000)]
        [InfoBox("用于UI显示的法力消耗", InfoMessageType.None)]
        public int DisplayCost = 20;
        
        [TitleGroup("技能配置/视觉效果")]
        [LabelText("图标ID")]
        public int IconId = 0;
        
        // ===== 技能动作列表 (关联 SkillActionTable 和 ActionTable) =====
        
        [TitleGroup("技能动作")]
        [InfoBox("技能动作对应ActionTable中的动作，每个ActionId必须在ActionTable中存在", InfoMessageType.Warning)]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "ActionDisplayName")]
        [OnValueChanged("OnSkillActionsChanged")]
        public List<SkillActionData> SkillActions = new List<SkillActionData>();
        
        // ===== 编辑器辅助字段 =====
        
        [HideInInspector]
        public bool IsNew = false;
        
        [HideInInspector]
        public bool IsDirty = false;
        
        [HideInInspector]
        public List<string> ValidationErrors = new List<string>();
        
        // ===== 方法 =====
        
        /// <summary>
        /// 创建默认技能数据
        /// </summary>
        public static SkillEditorData CreateDefault(int id)
        {
            var data = CreateInstance<SkillEditorData>();
            data.SkillId = id;
            data.SkillName = $"NewSkill_{id}";
            data.SkillDescription = "新技能";
            data.SkillType = SkillTypeEnum.攻击;
            data.RequiredLevel = 1;
            data.MaxLevel = 10;
            data.DisplayCooldown = 5.0f;
            data.DisplayCost = 20;
            data.IconId = 0;
            data.SkillActions = new List<SkillActionData>();
            data.IsNew = true;
            data.IsDirty = true;
            return data;
        }
        
        /// <summary>
        /// 克隆技能数据
        /// </summary>
        public SkillEditorData Clone()
        {
            var clone = CreateInstance<SkillEditorData>();
            
            // 复制技能基本信息
            clone.SkillId = this.SkillId;
            clone.SkillName = this.SkillName;
            clone.SkillDescription = this.SkillDescription;
            clone.SkillType = this.SkillType;
            clone.RequiredLevel = this.RequiredLevel;
            clone.MaxLevel = this.MaxLevel;
            clone.DisplayCooldown = this.DisplayCooldown;
            clone.DisplayCost = this.DisplayCost;
            clone.IconId = this.IconId;
            
            // 深拷贝技能动作列表
            clone.SkillActions = new List<SkillActionData>();
            foreach (var action in this.SkillActions)
            {
                clone.SkillActions.Add(action.Clone());
            }
            
            // 编辑器状态
            clone.ValidationErrors = new List<string>();
            clone.IsNew = true;
            clone.IsDirty = true;
            
            return clone;
        }
        
        /// <summary>
        /// 标记为已修改
        /// </summary>
        public void MarkDirty()
        {
            IsDirty = true;
        }
        
        /// <summary>
        /// 清除修改标记
        /// </summary>
        public void ClearDirty()
        {
            IsDirty = false;
            IsNew = false;
        }
        
        /// <summary>
        /// 获取所有技能动作的ActionId列表
        /// </summary>
        public List<int> GetSkillActionIds()
        {
            return SkillActions.Select(a => a.ActionId).ToList();
        }
        
        /// <summary>
        /// 添加技能动作
        /// </summary>
        public void AddSkillAction(int actionId)
        {
            // 检查是否已存在
            if (SkillActions.Any(a => a.ActionId == actionId))
            {
                Debug.LogWarning($"[SkillEditorData] ActionId {actionId} 已存在于技能动作列表中");
                return;
            }
            
            var actionData = new SkillActionData
            {
                ActionId = actionId,
                SkillId = this.SkillId,
                AttackBoxInfo = "",
                ActualCost = this.DisplayCost,
                ActualCooldown = (int)(this.DisplayCooldown * 60), // 转换为帧数
                TriggerFrames = new List<TriggerFrameInfo>()
            };
            
            SkillActions.Add(actionData);
            MarkDirty();
        }
        
        /// <summary>
        /// 移除技能动作
        /// </summary>
        public void RemoveSkillAction(int actionId)
        {
            var removed = SkillActions.RemoveAll(a => a.ActionId == actionId);
            if (removed > 0)
            {
                MarkDirty();
            }
        }
        
        // ===== Odin 回调 =====
        
        private bool ValidateSkillName(string name)
        {
            return !string.IsNullOrWhiteSpace(name);
        }
        
        private void OnSkillActionsChanged()
        {
            MarkDirty();
        }
    }
    
    /// <summary>
    /// 技能动作数据（对应 SkillActionTable，同时关联 ActionTable）
    /// </summary>
    [Serializable]
    public class SkillActionData
    {
        [TitleGroup("动作关联")]
        [LabelText("动作ID")]
        [InfoBox("必须对应ActionTable中的动作ID", InfoMessageType.Warning)]
        [OnValueChanged("OnActionIdChanged")]
        public int ActionId;
        
        [TitleGroup("动作关联")]
        [LabelText("所属技能ID"), ReadOnly]
        public int SkillId;
        
        [TitleGroup("动作关联")]
        [LabelText("动作名称"), ReadOnly]
        [ShowInInspector]
        public string ActionName => GetActionName();
        
        [TitleGroup("技能专用配置/攻击配置")]
        [LabelText("攻击碰撞盒信息")]
        [InfoBox("格式: Box1:5x2x1,Box2:3x3x1", InfoMessageType.None)]
        public string AttackBoxInfo = "";
        
        [TitleGroup("技能专用配置/消耗与冷却")]
        [LabelText("实际法力消耗"), Range(0, 1000)]
        [InfoBox("实际释放时的法力消耗（可与展示值不同）", InfoMessageType.None)]
        public int ActualCost = 15;
        
        [TitleGroup("技能专用配置/消耗与冷却")]
        [LabelText("实际冷却时间(帧)"), Range(0, 3600)]
        [InfoBox("实际冷却时间，单位：帧（60帧=1秒）", InfoMessageType.None)]
        public int ActualCooldown = 270;
        
        [TitleGroup("技能专用配置/触发帧配置")]
        [LabelText("触发帧列表")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "DisplayName")]
        [InfoBox("配置技能效果在哪些帧触发", InfoMessageType.Info)]
        public List<TriggerFrameInfo> TriggerFrames = new List<TriggerFrameInfo>();
        
        /// <summary>
        /// 用于Odin显示的名称
        /// </summary>
        [HideInInspector]
        public string ActionDisplayName => $"动作 {ActionId}: {ActionName}";
        
        /// <summary>
        /// 克隆动作数据
        /// </summary>
        public SkillActionData Clone()
        {
            var clone = new SkillActionData
            {
                ActionId = this.ActionId,
                SkillId = this.SkillId,
                AttackBoxInfo = this.AttackBoxInfo,
                ActualCost = this.ActualCost,
                ActualCooldown = this.ActualCooldown,
                TriggerFrames = new List<TriggerFrameInfo>()
            };
            
            // 深拷贝触发帧列表
            foreach (var trigger in this.TriggerFrames)
            {
                clone.TriggerFrames.Add(new TriggerFrameInfo
                {
                    Frame = trigger.Frame,
                    TriggerType = trigger.TriggerType,
                    EffectId = trigger.EffectId
                });
            }
            
            return clone;
        }
        
        /// <summary>
        /// 获取动作名称（从ActionTable查询）
        /// </summary>
        private string GetActionName()
        {
            // TODO: 从ConfigTableHelper查询ActionTable获取动作名称
            // 这里先返回占位符，后续在集成时实现
            return $"Action_{ActionId}";
        }
        
        private void OnActionIdChanged()
        {
            // 当ActionId改变时，可以自动更新关联信息
        }
    }
    
    /// <summary>
    /// 技能类型枚举
    /// </summary>
    public enum SkillTypeEnum
    {
        攻击 = 1,
        控制 = 2,
        位移 = 3,
        Buff = 4
    }
}

