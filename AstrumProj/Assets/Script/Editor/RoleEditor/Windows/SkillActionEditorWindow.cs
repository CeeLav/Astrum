using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Modules;
using Astrum.Editor.RoleEditor.Layout;
using Astrum.Editor.RoleEditor.Persistence;
using Astrum.Editor.RoleEditor.Services;
using Astrum.Editor.RoleEditor.Timeline;

namespace Astrum.Editor.RoleEditor.Windows
{
    /// <summary>
    /// 技能动作编辑器主窗口
    /// 继承自动作编辑器，扩展技能系统专属功能
    /// </summary>
    public class SkillActionEditorWindow : ActionEditorWindow
    {
        // === 技能专属模块 ===
        // 这些模块将在后续 Phase 中实现
        // private SkillEffectModule _skillEffectModule;
        
        // === Unity生命周期 ===
        
        [MenuItem("Tools/Astrum/Editors/Skill Action Editor")]
        public static new void ShowWindow()
        {
            var window = GetWindow<SkillActionEditorWindow>("技能动作编辑器");
            window.minSize = new Vector2(1200f, 800f);
            window.Show();
            
            // 窗口打开后执行扩展初始化
            window.OnEnableExtension();
        }
        
        // 不重写 OnEnable，使用基类的即可
        // 如果需要额外初始化，在窗口打开后执行
        private void OnEnableExtension()
        {
            // 注册技能效果轨道
            RegisterSkillEffectTrack();
            
            Debug.Log("[SkillActionEditor] Skill Action Editor Window opened");
        }
        
        
        // === 技能专属轨道注册 ===
        
        /// <summary>
        /// 注册技能效果轨道
        /// </summary>
        private void RegisterSkillEffectTrack()
        {
            // 检查轨道是否已注册
            bool hasTrack = TimelineTrackRegistry.HasTrack("SkillEffects");
            Debug.Log($"[SkillActionEditor] 技能效果轨道注册检查: {(hasTrack ? "已存在" : "需要注册")}");
            
            if (!hasTrack)
            {
                // TODO: Phase 3 - 实现技能效果轨道注册
                // TimelineTrackRegistry.RegisterTrack(new TimelineTrackConfig
                // {
                //     TrackType = "SkillEffects",
                //     TrackName = "技能效果",
                //     TrackIcon = "◆",
                //     TrackColor = new Color(1f, 0.3f, 0.3f),
                //     TrackHeight = 45f,
                //     IsVisible = true,
                //     IsLocked = false,
                //     SortOrder = 10,
                //     AllowOverlap = true,
                //     EventRenderer = Timeline.Renderers.SkillEffectEventRenderer.RenderEvent,
                //     EventEditor = Timeline.Renderers.SkillEffectEventRenderer.EditEvent
                // });
                
                Debug.Log("[SkillActionEditor] 技能效果轨道注册完成（待 Phase 3 实现）");
            }
        }
        
        // === 数据加载和保存（new 关键字隐藏基类方法） ===
        
        /// <summary>
        /// 加载技能动作数据
        /// 注意：使用 new 关键字隐藏基类的 LoadData 方法
        /// </summary>
        private new void LoadData()
        {
            try
            {
                // 使用技能动作数据读取器
                var skillActions = SkillActionDataReader.ReadSkillActionData();
                
                // TODO: 将数据设置到列表模块
                // _listModule.SetData(skillActions);
                
                Debug.Log($"[SkillActionEditor] Loaded {skillActions.Count} skill actions");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SkillActionEditor] Failed to load skill action data: {ex}");
                UnityEditor.EditorUtility.DisplayDialog("加载失败", $"加载技能动作数据失败：{ex.Message}", "确定");
            }
        }
        
        /// <summary>
        /// 保存技能动作数据
        /// 注意：使用 new 关键字隐藏基类的 SaveData 方法
        /// </summary>
        private new void SaveData()
        {
            try
            {
                // TODO: 从列表模块获取数据
                // var skillActions = _listModule.GetAllData();
                
                // 暂时创建一个空列表用于测试
                var skillActions = new List<SkillActionEditorData>();
                
                // 写入CSV文件
                if (SkillActionDataWriter.WriteSkillActionData(skillActions))
                {
                    Debug.Log($"[SkillActionEditor] Successfully saved {skillActions.Count} skill actions");
                    UnityEditor.EditorUtility.DisplayDialog("保存成功", $"成功保存 {skillActions.Count} 个技能动作", "确定");
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog("保存失败", "保存技能动作数据失败", "确定");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SkillActionEditor] Failed to save skill action data: {ex}");
                UnityEditor.EditorUtility.DisplayDialog("保存失败", $"保存技能动作数据失败：{ex.Message}", "确定");
            }
        }
    }
}

