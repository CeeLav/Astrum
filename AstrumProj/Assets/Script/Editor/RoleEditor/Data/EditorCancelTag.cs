using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// 编辑器专用的取消标签结构 - 用于 Unity 编辑器序列化
    /// </summary>
    [Serializable]
    public struct EditorCancelTag
    {
        [Tooltip("取消标签名称")]
        public string tag;
        
        [FormerlySerializedAs("startFromFrames")]
        [Tooltip("起始帧（帧数）")]
        public int startFrame;
        
        [FormerlySerializedAs("blendInFrames")]
        [Tooltip("融合时间（帧）")]
        public int blendFrame;
        
        [Tooltip("优先级变化")]
        public int priority;
        
        public EditorCancelTag(string tag, int startFrame, int blendFrame, int priority)
        {
            this.tag = tag;
            this.startFrame = startFrame;
            this.blendFrame = blendFrame;
            this.priority = priority;
        }
        
        /// <summary>
        /// 转换为运行时 CancelTag
        /// </summary>
        public Astrum.LogicCore.ActionSystem.CancelTag ToRuntimeCancelTag()
        {
            return new Astrum.LogicCore.ActionSystem.CancelTag(tag, startFrame, blendFrame, priority);
        }
        
        /// <summary>
        /// 从运行时 CancelTag 创建编辑器结构
        /// </summary>
        public static EditorCancelTag FromRuntimeCancelTag(Astrum.LogicCore.ActionSystem.CancelTag runtimeTag)
        {
            return new EditorCancelTag(
                runtimeTag.Tag,
                runtimeTag.StartFromFrames,
                runtimeTag.BlendInFrames,
                runtimeTag.Priority
            );
        }
    }
}
