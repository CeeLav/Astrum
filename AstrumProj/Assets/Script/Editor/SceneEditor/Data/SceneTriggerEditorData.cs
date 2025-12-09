using System.Collections.Generic;
using cfg;
using UnityEngine;

namespace Astrum.Editor.SceneEditor.Data
{
    public class SceneTriggerEditorData
    {
        public int TriggerId { get; set; }
        public List<ConditionEditorData> Conditions { get; set; } = new List<ConditionEditorData>();
        public List<ActionEditorData> Actions { get; set; } = new List<ActionEditorData>();
        public string ScenePath { get; set; }
    }
    
    public class ConditionEditorData
    {
        public int ConditionId { get; set; }
        public TriggerConditionType ConditionType { get; set; }
        public object Parameters { get; set; } // 强类型参数对象
    }
    
    public class ActionEditorData
    {
        public int ActionId { get; set; }
        public TriggerActionType ActionType { get; set; }
        public object Parameters { get; set; } // 强类型参数对象
        public GameObject VisualizerObject { get; set; } // 场景中的可视化对象
    }
}

