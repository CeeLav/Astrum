using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.SkillEffectEditors
{
    public interface ISkillEffectEditorPanel
    {
        string EffectType { get; }
        bool DrawContent(SkillEffectTableData data);
        
        /// <summary>
        /// 是否支持嵌入编辑（在时间轴事件面板中直接编辑）
        /// </summary>
        bool SupportsInlineEditing { get; }
    }
}

