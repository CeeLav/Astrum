using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    public interface ISkillEffectEditorPanel
    {
        string EffectType { get; }
        bool DrawContent(SkillEffectTableData data);
    }
}

