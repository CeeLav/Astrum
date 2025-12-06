using UnityEditor;
using Astrum.LogicCore.Components;

namespace Astrum.Editor.EntityRuntimeInspector
{
    /// <summary>
    /// 组件显示器接口
    /// 每个组件类型可以实现此接口来提供专门的显示逻辑
    /// </summary>
    public interface IComponentViewer
    {
        /// <summary>
        /// 绘制组件数据
        /// </summary>
        /// <param name="component">要显示的组件</param>
        void DrawComponent(BaseComponent component);
    }
}

