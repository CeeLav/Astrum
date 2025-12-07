using UnityEditor;
using Astrum.LogicCore.Components;
using Astrum.Generated;
using TrueSync;

namespace Astrum.Editor.EntityRuntimeInspector.ComponentViewers
{
    /// <summary>
    /// LSInputComponent 显示器
    /// </summary>
    public class LSInputComponentViewer : IComponentViewer
    {
        public void DrawComponent(BaseComponent component)
        {
            if (component is not Astrum.LogicCore.FrameSync.LSInputComponent input) return;

            EditorGUILayout.LabelField($"玩家ID: {input.PlayerId}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"输入历史数: {input.InputHistory?.Count ?? 0}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"最大历史数: {input.MaxHistoryCount}", EditorStyles.miniLabel);

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("当前输入 (CurrentInput):", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            DrawLSInput(input.CurrentInput);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("上一帧输入 (PreviousInput):", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            DrawLSInput(input.PreviousInput);
            EditorGUI.indentLevel--;
        }

        private void DrawLSInput(LSInput input)
        {
            if (input == null)
            {
                EditorGUILayout.LabelField("(null)", EditorStyles.miniLabel);
                return;
            }

            // 只显示有值的字段
            if (input.Frame != 0)
            {
                EditorGUILayout.LabelField($"帧号: {input.Frame}", EditorStyles.miniLabel);
            }

            // 转换定点数为浮点数
            var moveX = FP.FromRaw(input.MoveX);
            var moveY = FP.FromRaw(input.MoveY);
            if (moveX != 0 || moveY != 0)
            {
                EditorGUILayout.LabelField($"移动: ({moveX.AsFloat():F3}, {moveY.AsFloat():F3})", EditorStyles.miniLabel);
            }

            if (input.Attack)
            {
                EditorGUILayout.LabelField("攻击: ✓", EditorStyles.miniLabel);
            }

            if (input.Skill1)
            {
                EditorGUILayout.LabelField("技能1: ✓", EditorStyles.miniLabel);
            }

            if (input.Skill2)
            {
                EditorGUILayout.LabelField("技能2: ✓", EditorStyles.miniLabel);
            }

            if (input.Roll)
            {
                EditorGUILayout.LabelField("翻滚: ✓", EditorStyles.miniLabel);
            }

            if (input.Dash)
            {
                EditorGUILayout.LabelField("冲刺: ✓", EditorStyles.miniLabel);
            }

            if (input.BornInfo != 0)
            {
                EditorGUILayout.LabelField($"出生信息: {input.BornInfo}", EditorStyles.miniLabel);
            }

            var mouseX = FP.FromRaw(input.MouseWorldX);
            var mouseZ = FP.FromRaw(input.MouseWorldZ);
            if (mouseX != 0 || mouseZ != 0)
            {
                EditorGUILayout.LabelField($"鼠标世界坐标: ({mouseX.AsFloat():F2}, {mouseZ.AsFloat():F2})", EditorStyles.miniLabel);
            }

            if (input.Timestamp != 0)
            {
                EditorGUILayout.LabelField($"时间戳: {input.Timestamp}", EditorStyles.miniLabel);
            }
        }
    }
}


