using System.Collections.Generic;
using System.Linq;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 技能效果配置写入服务
    /// </summary>
    public static class SkillEffectDataWriter
    {
        private const string LOG_PREFIX = "[SkillEffectDataWriter]";

        /// <summary>
        /// 保存单个技能效果配置
        /// </summary>
        public static bool SaveEffect(SkillEffectTableData updated)
        {
            if (updated == null)
            {
                Debug.LogError($"{LOG_PREFIX} Updated effect data is null");
                return false;
            }

            var config = SkillEffectTableData.GetTableConfig();
            var allEffects = SkillEffectDataReader.ReadAllSkillEffects();
            var index = allEffects.FindIndex(e => e.SkillEffectId == updated.SkillEffectId);

            var workingList = new List<SkillEffectTableData>();
            workingList.AddRange(allEffects.Select(e => e.SkillEffectId == updated.SkillEffectId ? updated.Clone() : e.Clone()));

            if (index < 0)
            {
                workingList.Add(updated.Clone());
            }

            bool success = LubanCSVWriter.WriteTable(config, workingList);
            if (success)
            {
                SkillEffectDataReader.ClearCache();
                Debug.Log($"{LOG_PREFIX} Saved effect {updated.SkillEffectId} successfully");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX} Failed to save effect {updated.SkillEffectId}");
            }

            return success;
        }
    }
}

