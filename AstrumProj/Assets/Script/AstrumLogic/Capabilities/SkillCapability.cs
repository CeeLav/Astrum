using Astrum.CommonBase;
using Astrum.LogicCore.ActionSystem;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.SkillSystem;
using MemoryPack;
using System.Collections.Generic;

namespace Astrum.LogicCore.Capabilities
{
	/// <summary>
	/// 技能能力 - 管理实体的技能学习与动作注册
	/// </summary>
	[MemoryPackable]
	public partial class SkillCapability : Capability
	{
		public override void Initialize()
		{
			base.Initialize();

			var skillComponent = GetOwnerComponent<SkillComponent>();
			if (skillComponent == null)
			{
				ASLogger.Instance.Error($"SkillCapability.Initialize: SkillComponent not found on entity {OwnerEntity?.UniqueId}");
				return;
			}

			var actionComponent = GetOwnerComponent<ActionComponent>();
			if (actionComponent == null)
			{
				ASLogger.Instance.Error($"SkillCapability.Initialize: ActionComponent not found on entity {OwnerEntity?.UniqueId}");
				return;
			}

			LoadInitialSkills();
			RegisterAllSkillActions();
		}

		public override void Tick()
		{
			if (!CanExecute()) return;
			// 当前阶段无每帧逻辑（预留冷却/触发）
		}

		public bool LearnSkill(int skillId, int level = 1)
		{
			var skillComponent = GetOwnerComponent<SkillComponent>();
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

			var skillInfo = SkillConfigManager.Instance.GetSkillInfo(skillId, level);
			if (skillInfo == null)
			{
				ASLogger.Instance.Error($"SkillCapability.LearnSkill: Skill {skillId} not found in config");
				return false;
			}

			skillComponent.AddSkill(skillInfo);
			RegisterSkillActions(skillInfo);

			ASLogger.Instance.Info($"SkillCapability.LearnSkill: Entity {OwnerEntity?.UniqueId} learned skill {skillId} (Lv{level})");
			return true;
		}

		public bool ForgetSkill(int skillId)
		{
			var skillComponent = GetOwnerComponent<SkillComponent>();
			if (skillComponent == null) return false;

			var skill = skillComponent.GetSkill(skillId);
			if (skill == null) return false;

			UnregisterSkillActions(skill);
			bool removed = skillComponent.RemoveSkill(skillId);
			if (removed)
			{
				ASLogger.Instance.Info($"SkillCapability.ForgetSkill: Entity {OwnerEntity?.UniqueId} forgot skill {skillId}");
			}
			return removed;
		}

		private void LoadInitialSkills()
		{
			var roleInfo = GetOwnerComponent<RoleInfoComponent>();
			if (roleInfo?.RoleConfig == null) return;

			var cfg = roleInfo.RoleConfig;
			// 轻击/重击（也作为技能处理）
			if (cfg.LightAttackSkillId > 0) { LearnSkill(cfg.LightAttackSkillId, 1); }
			if (cfg.HeavyAttackSkillId > 0) { LearnSkill(cfg.HeavyAttackSkillId, 1); }
			// 其它两个配置技能位
			if (cfg.Skill1Id > 0) { LearnSkill(cfg.Skill1Id, 1); }
			if (cfg.Skill2Id > 0) { LearnSkill(cfg.Skill2Id, 1); }
		}

		private void RegisterAllSkillActions()
		{
			var skillComponent = GetOwnerComponent<SkillComponent>();
			if (skillComponent == null) return;

			foreach (var kv in skillComponent.LearnedSkills)
			{
				RegisterSkillActions(kv.Value);
			}
		}

		private void RegisterSkillActions(SkillInfo skill)
		{
			var actionComponent = GetOwnerComponent<ActionComponent>();
			if (actionComponent == null) return;

			foreach (var action in skill.SkillActions)
			{
				if (!actionComponent.AvailableActions.Exists(a => a.Id == action.Id))
				{
					actionComponent.AvailableActions.Add(action);
					ASLogger.Instance.Debug($"SkillCapability: Registered action {action.Id} for skill {skill.SkillId}");
				}
			}
		}

		private void UnregisterSkillActions(SkillInfo skill)
		{
			var actionComponent = GetOwnerComponent<ActionComponent>();
			if (actionComponent == null) return;

			foreach (var action in skill.SkillActions)
			{
				actionComponent.AvailableActions.RemoveAll(a => a.Id == action.Id);
				ASLogger.Instance.Debug($"SkillCapability: Unregistered action {action.Id} for skill {skill.SkillId}");
			}
		}
	}
}


