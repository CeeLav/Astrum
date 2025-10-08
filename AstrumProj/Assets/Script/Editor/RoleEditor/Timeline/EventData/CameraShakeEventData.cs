using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Timeline.EventData
{
    /// <summary>
    /// 相机震动事件数据
    /// </summary>
    [Serializable]
    public class CameraShakeEventData
    {
        /// <summary>震动强度 (0-1)</summary>
        public float Intensity = 0.5f;
        
        /// <summary>震动频率 (Hz)</summary>
        public float Frequency = 10f;
        
        /// <summary>震动方向</summary>
        public Vector3 Direction = Vector3.one;
        
        /// <summary>衰减曲线（暂不支持，后续版本）</summary>
        public string FalloffCurve = "";
        
        /// <summary>备注</summary>
        public string Note = "";
        
        // === 工厂方法 ===
        
        /// <summary>
        /// 创建默认数据
        /// </summary>
        public static CameraShakeEventData CreateDefault()
        {
            return new CameraShakeEventData
            {
                Intensity = 0.5f,
                Frequency = 10f,
                Direction = Vector3.one,
                FalloffCurve = "",
                Note = ""
            };
        }
        
        /// <summary>
        /// 克隆数据
        /// </summary>
        public CameraShakeEventData Clone()
        {
            return new CameraShakeEventData
            {
                Intensity = this.Intensity,
                Frequency = this.Frequency,
                Direction = this.Direction,
                FalloffCurve = this.FalloffCurve,
                Note = this.Note
            };
        }
        
        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            string intensityDesc = Intensity < 0.3f ? "轻微" : Intensity < 0.7f ? "中等" : "强烈";
            return $"震动:{intensityDesc}";
        }
        
        /// <summary>
        /// 验证数据有效性
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            
            if (Intensity < 0 || Intensity > 1)
            {
                errors.Add("强度必须在0-1之间");
            }
            
            if (Frequency <= 0)
            {
                errors.Add("频率必须大于0");
            }
            
            return errors.Count == 0;
        }
    }
}
