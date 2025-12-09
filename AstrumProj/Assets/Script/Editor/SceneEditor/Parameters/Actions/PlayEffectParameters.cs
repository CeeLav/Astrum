using Astrum.Editor.SceneEditor.Converters;
using UnityEngine;

namespace Astrum.Editor.SceneEditor.Parameters.Actions
{
    public class PlayEffectParameters : IPositionInfoProvider
    {
        public int EffectId { get; set; }
        public float Duration { get; set; }
        public Vector3 Position { get; set; }
        
        public PositionInfo GetPositionInfo()
        {
            return new PositionInfo { Type = PositionType.Point, Point = Position };
        }
    }
}

