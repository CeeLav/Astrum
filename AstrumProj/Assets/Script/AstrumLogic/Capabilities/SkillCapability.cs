using Astrum.CommonBase;
using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.SkillSystem;
using System.Collections.Generic;

namespace Astrum.LogicCore.Capabilities
{
	/// <summary>
	/// 技能能力（新架构，基于 Capability&lt;T&gt;）
	/// 管理实体的技能学习与动作注册
	/// </summary>
	public class SkillCapability : Capability<SkillCapability>
	{
		// ====== 元数据 ======
		public override int Priority => 300; // 技能系统优先级较高
		
		public override IReadOnlyCollection<CapabilityTag> Tags => _tags;
		private static readonly HashSet<CapabilityTag> _tags = new HashSet<CapabilityTag> 
		{ 
			CapabilityTag.Skill, 
			CapabilityTag.Combat 
		};
		
		// ====== 生命周期 ======
		
		public override void OnAttached(Entity entity)
		{
			base.OnAttached(entity);

			var skillComponent = GetComponent<SkillComponent>(entity);
			if (skillComponent == null)
			{
				ASLogger.Instance.Error($"SkillCapability.OnAttached: SkillComponent not found on entity {entity.UniqueId}");
				return;
			}

			var actionComponent = GetComponent<ActionComponent>(entity);
			if (actionComponent == null)
			{
				ASLogger.Instance.Error($"SkillCapability.OnAttached: ActionComponent not found on entity {entity.UniqueId}");
				return;
			}

			LoadInitialSkills(entity);
			RegisterAllSkillActions(entity);
		}
		
		public override bool ShouldActivate(Entity entity)
		{
			// 检查必需组件是否存在
			return base.ShouldActivate(entity) &&
				   HasComponent<SkillComponent>(entity) &&
				   HasComponent<ActionComponent>(entity);
		}
		
		public override bool ShouldDeactivate(Entity entity)
		{
			// 缺少任何必需组件则停用
			return base.ShouldDeactivate(entity) ||
				   !HasComponent<SkillComponent>(entity) ||
				   !HasComponent<ActionComponent>(entity);
		}

		// ====== 每帧逻辑 ======
		
		public override void Tick(Entity entity)
		{
			// 当前阶段无每帧逻辑（预留冷却/触发）
			// 技能学习/注销在 OnAttached 和公共方法中完成
		}

		// ====== 公共方法 ======
		
		public bool LearnSkill(Entity entity, int skillId, int level = 1)
		{
			var skillComponent = GetComponent<SkillComponent>(entity);
			if (skillComponent == null)
			{
				ASLogger.Instance.Error("SkillCapability.LearnSkill: SkillComponent not found");
				return false;
			}

			if (skillComponent.HasSkill(skillId))
			{
				ASLogger.Instance.Warning($"SkillCapability.LearnSkill: Skill {skillId} already learned");
				return false;
			}

			var skillInfo = SkillConfig.Instance.GetSkillInfo(skillId, level);
			if (skillInfo == null)
			{
				ASLogger.Instance.Error($"SkillCapability.LearnSkill: Skill {skillId} not found in config");
				return false;
			}

			skillComponent.AddSkill(skillInfo);
			RegisterSkillActions(entity, skillInfo);

			ASLogger.Instance.Info($"SkillCapability.LearnSkill: Entity {entity.UniqueId} learned skill {skillId} (Lv{level})");
			return true;
		}

		public bool ForgetSkill(Entity entity, int skillId)
		{
			var skillComponent = GetComponent<SkillComponent>(entity);
			if (skillComponent == null) return false;

			var skill = skillComponent.GetSkill(skillId);
			if (skill == null) return false;

			UnregisterSkillActions(entity, skill);
			bool removed = skillComponent.RemoveSkill(skillId);
			if (removed)
			{
				ASLogger.Instance.Info($"SkillCapability.ForgetSkill: Entity {entity.UniqueId} forgot skill {skillId}");
			}
			return removed;
		}

		// ====== 辅助方法 ======
		
		private void LoadInitialSkills(Entity entity)
		{
			var roleInfo = GetComponent<RoleInfoComponent>(entity);
			if (roleInfo?.RoleConfig == null) return;

			var cfg = roleInfo.RoleConfig;
			if (cfg.DefaultSkillIds == null || cfg.DefaultSkillIds.Length == 0)
			{
				return;
			}

			foreach (var skillId in cfg.DefaultSkillIds)
			{
				if (skillId > 0)
				{
					LearnSkill(entity, skillId, 1);
				}
			}
		}

		private void RegisterAllSkillActions(Entity entity)
		{
			var skillComponent = GetComponent<SkillComponent>(entity);
			if (skillComponent == null) return;

			foreach (var kv in skillComponent.LearnedSkills)
			{
				RegisterSkillActions(entity, kv.Value);
			}
		}

		private void RegisterSkillActions(Entity entity, SkillInfo skill)
		{
			var actionComponent = GetComponent<ActionComponent>(entity);
			if (actionComponent == null) return;

			// 基于 SkillActionIds 动态构造新的 SkillActionInfo 实例
			foreach (var actionId in skill.SkillActionIds)
			{
				var action = SkillConfig.Instance.CreateSkillActionInstance(actionId, skill.CurrentLevel);
				if (action == null) continue;
				if (!actionComponent.AvailableActions.ContainsKey(action.Id))
				{
					actionComponent.AvailableActions[action.Id] = action;
					ASLogger.Instance.Debug($"SkillCapability: Registered action {action.Id} for skill");
				}
			}
		}

		private void UnregisterSkillActions(Entity entity, SkillInfo skill)
		{
			var actionComponent = GetComponent<ActionComponent>(entity);
			if (actionComponent == null) return;
			// TODO: 实现技能动作注销逻辑
		}
	}
}
