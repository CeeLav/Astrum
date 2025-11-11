using UnityEngine;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 手动子弹预览服务：供编辑器界面触发弹道预览
    /// </summary>
    internal static class ProjectileManualPreviewService
    {
        public static void Fire(SkillEffectTableData data)
        {
            var manager = ProjectilePreviewManager.ActiveInstance;
            if (manager == null)
            {
                Debug.LogWarning("[ProjectilePreview] 暂无法预览弹道：预览模块未初始化");
                return;
            }

            manager.FireManualProjectile(data);
        }

        public static void Stop()
        {
            ProjectilePreviewManager.ActiveInstance?.ClearManualProjectiles();
        }
    }
}
