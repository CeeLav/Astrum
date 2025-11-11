using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Mappings;
using Astrum.Editor.RoleEditor.Services;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    internal static class SkillEffectEditorRegistry
    {
        private static readonly Dictionary<string, ISkillEffectEditorPanel> Panels = new Dictionary<string, ISkillEffectEditorPanel>(System.StringComparer.OrdinalIgnoreCase);
        private static readonly ISkillEffectEditorPanel GenericPanel = new GenericSkillEffectEditorPanel();

        static SkillEffectEditorRegistry()
        {
            Register(new DamageEffectEditorPanel());
            Register(new HealEffectEditorPanel());
            Register(new KnockbackEffectEditorPanel());
            Register(new StatusEffectEditorPanel());
            Register(new TeleportEffectEditorPanel());
            Register(new ProjectileEffectEditorPanel());
        }

        private static void Register(ISkillEffectEditorPanel panel)
        {
            Panels[panel.EffectType] = panel;
        }

        public static ISkillEffectEditorPanel GetPanel(string effectType)
        {
            if (string.IsNullOrEmpty(effectType))
                return GenericPanel;

            return Panels.TryGetValue(effectType, out var panel) ? panel : GenericPanel;
        }

        public static IReadOnlyList<string> GetKnownEffectTypes()
        {
            var list = new List<string>(Panels.Keys);
            list.Sort(System.StringComparer.OrdinalIgnoreCase);
            return list;
        }
        
        /// <summary>
        /// 保存技能效果数据
        /// </summary>
        public static void SaveEffect(SkillEffectTableData data)
        {
            if (data == null) return;
            
            // 调用数据写入服务
            SkillEffectDataWriter.SaveEffect(data);
        }
    }
}

