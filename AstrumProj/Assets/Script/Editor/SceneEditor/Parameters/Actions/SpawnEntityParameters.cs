using Astrum.Editor.SceneEditor.Converters;
using UnityEngine;

namespace Astrum.Editor.SceneEditor.Parameters.Actions
{
    public class SpawnEntityParameters : IPositionInfoProvider
    {
        public int EntityId { get; set; }
        public int Count { get; set; }
        public Vector3 Position { get; set; }
        public Bounds? Range { get; set; }
        
        public PositionInfo GetPositionInfo()
        {
            if (Range.HasValue)
                return new PositionInfo { Type = PositionType.Range, Range = Range };
            else
                return new PositionInfo { Type = PositionType.Point, Point = Position };
        }
    }
}

