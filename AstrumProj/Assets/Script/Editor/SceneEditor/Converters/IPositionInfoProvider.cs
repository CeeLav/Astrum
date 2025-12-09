using UnityEngine;

namespace Astrum.Editor.SceneEditor.Converters
{
    /// <summary>
    /// 位置信息提供接口
    /// </summary>
    public interface IPositionInfoProvider
    {
        PositionInfo GetPositionInfo();
    }
    
    public class PositionInfo
    {
        public PositionType Type { get; set; }
        public Vector3? Point { get; set; }
        public Bounds? Range { get; set; }
    }
    
    public enum PositionType { None, Point, Range }
}

