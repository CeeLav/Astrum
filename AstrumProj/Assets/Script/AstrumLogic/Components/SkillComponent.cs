using Astrum.LogicCore.SkillSystem;
using Astrum.LogicCore.Capabilities;
using MemoryPack;
using System.Collections.Generic;

namespace Astrum.LogicCore.Components
{
	/// <summary>
	/// 技能组件 - 存储实体已学习的技能数据
	/// 组件应保持轻逻辑：仅负责数据的持有与基本查询/修改
	/// </summary>
	[MemoryPackable]
	public partial class SkillComponent : BaseComponent
	{
		/// <summary>
		/// 组件类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
		/// </summary>
		public static readonly int ComponentTypeId = TypeHash<SkillComponent>.GetHash();
		
		/// <summary>
		/// 获取组件的类型 ID
		/// </summary>
		public override int GetComponentTypeId() => ComponentTypeId;
		/// <summary>
		/// 已学习的技能字典 <SkillId, SkillInfo>
		/// 同一技能的不同等级应由 SkillInfo.CurrentLevel 体现
		/// </summary>
		[MemoryPackAllowSerialize]
		public Dictionary<int, SkillInfo> LearnedSkills { get; set; } = new();

		[MemoryPackConstructor]
		public SkillComponent() : base() { }

		/// <summary>
		/// 是否已学习指定技能
		/// </summary>
		public bool HasSkill(int skillId)
		{
			return LearnedSkills.ContainsKey(skillId);
		}

		/// <summary>
		/// 获取已学习的技能，没有则返回 null
		/// </summary>
		public SkillInfo GetSkill(int skillId)
		{
			return LearnedSkills.TryGetValue(skillId, out var info) ? info : null;
		}

		/// <summary>
		/// 添加或覆盖已学习的技能
		/// </summary>
		public void AddSkill(SkillInfo skill)
		{
			if (skill == null) return;
			LearnedSkills[skill.SkillId] = skill;
		}

		/// <summary>
		/// 移除已学习的技能
		/// </summary>
		public bool RemoveSkill(int skillId)
		{
			return LearnedSkills.Remove(skillId);
		}
	}
}


