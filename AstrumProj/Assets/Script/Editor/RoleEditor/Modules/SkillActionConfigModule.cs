using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence;
using Astrum.Editor.RoleEditor.Services;

namespace Astrum.Editor.RoleEditor.Modules
{
    /// <summary>
    /// 技能动作配置面板模块
    /// 不继承 ActionConfigModule，直接独立实现
    /// </summary>
    public class SkillActionConfigModule
    {
        // === 数据 ===
        private SkillActionEditorData _currentSkillAction;
        private Vector2 _scrollPosition;
        private PropertyTree _propertyTree;
        
        // === 事件编辑 ===
        private Timeline.TimelineEvent _selectedEvent;
        
        // === 折叠状态 ===
        private bool _skillCostFoldout = true;
        private bool _hitDummyFoldout = true;

        // === 木桩模板 ===
        private SkillHitDummyTemplateCollection _hitDummyCollection;
        private int _selectedHitDummyIndex = -1;
        private Vector2 _hitDummyScroll;
        
        // === 事件 ===
        public event Action<ActionEditorData> OnActionModified;
        public event Action OnJumpToTimeline;
        
        // === 预览模块引用 ===
        private AnimationPreviewModule _previewModule;
        
        // === 核心方法 ===
        
        /// <summary>
        /// 绘制配置面板
        /// </summary>
        public void DrawConfig(Rect rect, SkillActionEditorData skillAction)
        {
            GUILayout.BeginArea(rect);
            
            if (skillAction == null)
            {
                EditorGUILayout.HelpBox("请选择一个技能动作", MessageType.Info);
                GUILayout.EndArea();
                return;
            }
            
            // 如果动作变了，重建PropertyTree
            if (_currentSkillAction != skillAction)
            {
                SetSkillAction(skillAction);
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                // 绘制基础信息（使用 Odin Inspector）
                DrawOdinInspector();
                DrawAnimationSection();
                DrawAnimationStatusCheck();
                
                EditorGUILayout.Space(5);
                
                // 绘制技能专属内容
                DrawSkillCost();
                DrawHitDummyTemplateSection();
            }
            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 设置当前技能动作
        /// </summary>
        public void SetSkillAction(SkillActionEditorData skillAction)
        {
            _currentSkillAction = skillAction;
            _selectedEvent = null;
            
            // 重建PropertyTree
            _propertyTree?.Dispose();
            if (_currentSkillAction != null)
            {
                _propertyTree = PropertyTree.Create(_currentSkillAction);
            }

            EnsureHitDummyCollection();
            UpdateSelectedTemplateIndex(true);
            ApplySelectedTemplateToPreview();
        }
        
        /// <summary>
        /// 设置选中的时间轴事件
        /// </summary>
        public void SetSelectedEvent(Timeline.TimelineEvent evt)
        {
            _selectedEvent = evt;
        }
        
        /// <summary>
        /// 设置预览模块引用
        /// </summary>
        public void SetPreviewModule(AnimationPreviewModule previewModule)
        {
            _previewModule = previewModule;
            
            if (_previewModule != null)
            {
                _previewModule.SetHitDummyEnabled(true);
            }
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _propertyTree?.Dispose();
            _propertyTree = null;
        }
        
        // === 私有绘制方法（基础部分，复制自 ActionConfigModule） ===
        
        private void DrawOdinInspector()
        {
            if (_propertyTree == null) return;
            
            _propertyTree.UpdateTree();
            _propertyTree.BeginDraw(true);
            
            // 需要在DrawAnimationSection中手动绘制的字段（排除它们，避免重复绘制）
            var excludedAnimationMotionFields = new HashSet<string>
            {
                "ExtractMode",
                "ReferenceAnimationPath",
                "ReferenceAnimationClip",
                "HipsBoneName",
                "ExtractRotation",
                "ExtractHorizontalOnly",
                "RootMotionDataArray"
            };
            
            foreach (var property in _propertyTree.EnumerateTree(false))
            {
                // 绘制带 TitleGroup 的属性
                if (property.Info.GetAttribute<TitleGroupAttribute>() != null || 
                    property.Parent?.Info.GetAttribute<TitleGroupAttribute>() != null)
                {
                    // 排除在DrawAnimationSection中手动绘制的"动画位移"相关字段（除了ExtractMode）
                    if (excludedAnimationMotionFields.Contains(property.Name))
                    {
                        continue; // 跳过，这些字段在DrawAnimationSection中手动处理
                    }
                    
                    // 其他字段正常绘制（包括ExtractMode，它在Odin中显示以便选择模式）
                    property.Draw();
                }
            }
            
            _propertyTree.EndDraw();
            
            // 应用修改
            if (_propertyTree.ApplyChanges())
            {
                _currentSkillAction.MarkDirty();
                EditorUtility.SetDirty(_currentSkillAction);
                OnActionModified?.Invoke(_currentSkillAction);
            }
        }
        
        private void DrawAnimationSection()
        {
            if (_currentSkillAction == null) return;
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("基础动画", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                string newPath = EditorGUILayout.TextField(_currentSkillAction.AnimationPath);
                if (EditorGUI.EndChangeCheck())
                {
                    _currentSkillAction.AnimationPath = newPath;
                    
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        _currentSkillAction.AnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newPath);
                    }
                    else
                    {
                        _currentSkillAction.AnimationClip = null;
                    }
                    
                    _currentSkillAction.MarkDirty();
                    EditorUtility.SetDirty(_currentSkillAction);
                    OnActionModified?.Invoke(_currentSkillAction);
                }
                
                EditorGUILayout.Space(3);
                
                EditorGUILayout.LabelField("动画文件", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                var newClip = EditorGUILayout.ObjectField(_currentSkillAction.AnimationClip, typeof(AnimationClip), false) as AnimationClip;
                if (EditorGUI.EndChangeCheck())
                {
                    _currentSkillAction.AnimationClip = newClip;
                    
                    if (newClip != null)
                    {
                        _currentSkillAction.AnimationPath = AssetDatabase.GetAssetPath(newClip);
                    }
                    else
                    {
                        _currentSkillAction.AnimationPath = "";
                    }
                    
                    _currentSkillAction.MarkDirty();
                    EditorUtility.SetDirty(_currentSkillAction);
                    OnActionModified?.Invoke(_currentSkillAction);
                }
                
                EditorGUILayout.HelpBox("💡 拖拽 AnimationClip 到上方字段自动更新路径", MessageType.None);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            
            // 动画位移提取配置
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("动画位移提取", EditorStyles.boldLabel);
                
                // 绘制提取模式选择
                EditorGUI.BeginChangeCheck();
                var newMode = (SkillActionEditorData.RootMotionExtractMode)EditorGUILayout.EnumPopup(
                    "提取模式", 
                    _currentSkillAction.ExtractMode);
                if (EditorGUI.EndChangeCheck())
                {
                    _currentSkillAction.ExtractMode = newMode;
                    _currentSkillAction.MarkDirty();
                    EditorUtility.SetDirty(_currentSkillAction);
                    OnActionModified?.Invoke(_currentSkillAction);
                }
                
                EditorGUILayout.HelpBox(
                    _currentSkillAction.ExtractMode == SkillActionEditorData.RootMotionExtractMode.RootTransform
                        ? "根骨骼位移模式：直接提取动画根节点的位移曲线"
                        : "Hips差值模式：通过对比参考动画（带位移）和基础动画（不带位移）计算Hips骨骼位移差值",
                    MessageType.Info);
                
                EditorGUILayout.Space(3);
                
                // 模式2：参考动画配置
                if (_currentSkillAction.ExtractMode == SkillActionEditorData.RootMotionExtractMode.HipsDifference)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("参考动画配置", EditorStyles.boldLabel);
                    
                    EditorGUI.BeginChangeCheck();
                    string newRefPath = EditorGUILayout.TextField("参考动画路径", _currentSkillAction.ReferenceAnimationPath);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentSkillAction.ReferenceAnimationPath = newRefPath;
                        
                        if (!string.IsNullOrEmpty(newRefPath))
                        {
                            _currentSkillAction.ReferenceAnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newRefPath);
                        }
                        else
                        {
                            _currentSkillAction.ReferenceAnimationClip = null;
                        }
                        
                        _currentSkillAction.MarkDirty();
                        EditorUtility.SetDirty(_currentSkillAction);
                        OnActionModified?.Invoke(_currentSkillAction);
                    }
                    
                    EditorGUI.BeginChangeCheck();
                    var newRefClip = EditorGUILayout.ObjectField("参考动画文件", _currentSkillAction.ReferenceAnimationClip, typeof(AnimationClip), false) as AnimationClip;
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentSkillAction.ReferenceAnimationClip = newRefClip;
                        
                        if (newRefClip != null)
                        {
                            _currentSkillAction.ReferenceAnimationPath = AssetDatabase.GetAssetPath(newRefClip);
                        }
                        else
                        {
                            _currentSkillAction.ReferenceAnimationPath = "";
                        }
                        
                        _currentSkillAction.MarkDirty();
                        EditorUtility.SetDirty(_currentSkillAction);
                        OnActionModified?.Invoke(_currentSkillAction);
                    }
                    
                    EditorGUILayout.Space(3);
                    EditorGUI.BeginChangeCheck();
                    string hipsName = EditorGUILayout.TextField("Hips骨骼名称", _currentSkillAction.HipsBoneName);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentSkillAction.HipsBoneName = hipsName;
                        _currentSkillAction.MarkDirty();
                        EditorUtility.SetDirty(_currentSkillAction);
                        OnActionModified?.Invoke(_currentSkillAction);
                    }
                }
                
                EditorGUILayout.Space(5);
                
                // 提取选项
                EditorGUI.BeginChangeCheck();
                bool extractRotation = EditorGUILayout.Toggle("提取旋转", _currentSkillAction.ExtractRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    _currentSkillAction.ExtractRotation = extractRotation;
                    _currentSkillAction.MarkDirty();
                    EditorUtility.SetDirty(_currentSkillAction);
                    OnActionModified?.Invoke(_currentSkillAction);
                }
                
                EditorGUI.BeginChangeCheck();
                bool extractHorizontalOnly = EditorGUILayout.Toggle("只提取水平方向（忽略Y轴）", _currentSkillAction.ExtractHorizontalOnly);
                if (EditorGUI.EndChangeCheck())
                {
                    _currentSkillAction.ExtractHorizontalOnly = extractHorizontalOnly;
                    _currentSkillAction.MarkDirty();
                    EditorUtility.SetDirty(_currentSkillAction);
                    OnActionModified?.Invoke(_currentSkillAction);
                }
                
                EditorGUILayout.Space(5);
                
                // 提取位移数据按钮
                EditorGUILayout.BeginHorizontal();
                {
                    bool canExtract = _currentSkillAction.AnimationClip != null;
                    
                    // 模式2需要额外检查
                    if (_currentSkillAction.ExtractMode == SkillActionEditorData.RootMotionExtractMode.HipsDifference)
                    {
                        canExtract = canExtract && 
                                   _currentSkillAction.ReferenceAnimationClip != null && 
                                   _previewModule != null && 
                                   _previewModule.GetPreviewModel() != null;
                        
                        if (!canExtract && _currentSkillAction.AnimationClip != null)
                        {
                            EditorGUILayout.HelpBox("⚠️ Hips差值模式需要：参考动画文件 + 已加载的预览模型", MessageType.Warning);
                        }
                    }
                    
                    GUI.enabled = canExtract;
                    if (GUILayout.Button("提取位移数据", GUILayout.Height(30)))
                    {
                        ExtractRootMotionData();
                    }
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
                
                // 显示位移数据信息
                DrawRootMotionDataInfo();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// 提取根节点位移数据
        /// </summary>
        private void ExtractRootMotionData()
        {
            if (_currentSkillAction == null || _currentSkillAction.AnimationClip == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择动画文件", "确定");
                return;
            }
            
            try
            {
                var clip = _currentSkillAction.AnimationClip;
                List<int> result;
                
                // 根据提取模式选择不同的方法
                bool extractRotation = _currentSkillAction.ExtractRotation;
                bool extractHorizontalOnly = _currentSkillAction.ExtractHorizontalOnly;
                
                if (_currentSkillAction.ExtractMode == SkillActionEditorData.RootMotionExtractMode.RootTransform)
                {
                    // 模式1：提取根骨骼位移
                    result = Astrum.Editor.RoleEditor.Services.AnimationRootMotionExtractor.ExtractRootMotionToIntArray(
                        clip, extractRotation, extractHorizontalOnly);
                }
                else // HipsDifference
                {
                    // 模式2：使用参考动画计算Hips差值
                    if (_currentSkillAction.ReferenceAnimationClip == null)
                    {
                        EditorUtility.DisplayDialog("错误", "请先选择参考动画文件（带位移的动画）", "确定");
                        return;
                    }
                    
                    GameObject model = _previewModule?.GetPreviewModel();
                    if (model == null)
                    {
                        EditorUtility.DisplayDialog("错误", 
                            "未找到预览模型。请先在预览区域选择一个实体并加载模型。", 
                            "确定");
                        return;
                    }
                    
                    result = Astrum.Editor.RoleEditor.Services.AnimationRootMotionExtractor.ExtractHipsMotionDifference(
                        baseClip: clip,
                        referenceClip: _currentSkillAction.ReferenceAnimationClip,
                        hipsBoneName: _currentSkillAction.HipsBoneName ?? "Hips",
                        modelGameObject: model,
                        extractRotation: extractRotation,
                        extractHorizontalOnly: extractHorizontalOnly);
                }
                
                _currentSkillAction.RootMotionDataArray = result;
                
                // 更新预览模块的位移数据（如果预览模块可用）
                if (_previewModule != null)
                {
                    if (_currentSkillAction.RootMotionDataArray != null && _currentSkillAction.RootMotionDataArray.Count > 0)
                    {
                        _previewModule.SetRootMotionData(_currentSkillAction.RootMotionDataArray);
                    }
                    else
                    {
                        _previewModule.SetRootMotionData(null);
                    }
                }
                
                if (_currentSkillAction.RootMotionDataArray != null && _currentSkillAction.RootMotionDataArray.Count > 0)
                {
                    int frameCount = _currentSkillAction.RootMotionDataArray[0];
                    EditorUtility.DisplayDialog("提取成功", 
                        $"已提取位移数据：\n模式: {_currentSkillAction.ExtractMode}\n帧数: {frameCount}\n数据大小: {_currentSkillAction.RootMotionDataArray.Count} 整数", 
                        "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", 
                        $"未能提取到位移数据。\n模式: {_currentSkillAction.ExtractMode}", 
                        "确定");
                    _currentSkillAction.RootMotionDataArray = new List<int>();
                }
                
                _currentSkillAction.MarkDirty();
                EditorUtility.SetDirty(_currentSkillAction);
                OnActionModified?.Invoke(_currentSkillAction);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"提取位移数据失败：\n{ex.Message}", "确定");
                Debug.LogError($"[SkillActionConfigModule] Failed to extract root motion: {ex}");
            }
        }
        
        /// <summary>
        /// 显示根节点位移数据信息
        /// </summary>
        private void DrawRootMotionDataInfo()
        {
            if (_currentSkillAction == null) return;
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("位移数据信息", EditorStyles.boldLabel);
            
            if (_currentSkillAction.RootMotionDataArray == null || _currentSkillAction.RootMotionDataArray.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无位移数据", MessageType.Info);
                return;
            }
            
            int frameCount = _currentSkillAction.RootMotionDataArray[0];
            int dataSize = _currentSkillAction.RootMotionDataArray.Count;
            int expectedSize = 1 + frameCount * 7; // frameCount + (dx,dy,dz,rx,ry,rz,rw) * frameCount
            
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField($"帧数: {frameCount}", EditorStyles.label);
                EditorGUILayout.LabelField($"数据大小: {dataSize} 整数", EditorStyles.label);
                
                if (dataSize == expectedSize)
                {
                    EditorGUILayout.HelpBox("✓ 数据格式正确", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox($"⚠️ 数据格式异常 (期望: {expectedSize}, 实际: {dataSize})", MessageType.Warning);
                }
                
                // 显示当前累积位移
                if (_previewModule != null)
                {
                    Vector3 currentPos = _previewModule.GetAccumulatedPositionFromData(_previewModule.GetCurrentFrame());
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("当前累积位移", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"X: {currentPos.x:F3} m", EditorStyles.label);
                    EditorGUILayout.LabelField($"Y: {currentPos.y:F3} m", EditorStyles.label);
                    EditorGUILayout.LabelField($"Z: {currentPos.z:F3} m", EditorStyles.label);
                    
                    // 计算总位移长度
                    float distance = Vector3.Magnitude(currentPos);
                    EditorGUILayout.LabelField($"总位移: {distance:F3} m", EditorStyles.boldLabel);
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAnimationStatusCheck()
        {
            if (_currentSkillAction == null) return;
            
            if (string.IsNullOrEmpty(_currentSkillAction.AnimationPath))
            {
                EditorGUILayout.HelpBox(
                    "⚠️ 未设置动画路径，请先配置动画文件才能正常使用此技能动作", 
                    MessageType.Warning
                );
                return;
            }
            
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(_currentSkillAction.AnimationPath);
            if (clip == null)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ 动画文件不存在: {_currentSkillAction.AnimationPath}", 
                    MessageType.Error
                );
                return;
            }
            
            if (_currentSkillAction.Duration > _currentSkillAction.AnimationDuration)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ 动作总帧数({_currentSkillAction.Duration})超过了动画总帧数({_currentSkillAction.AnimationDuration})", 
                    MessageType.Warning
                );
                
                if (GUILayout.Button("自动修正为动画总帧数"))
                {
                    _currentSkillAction.Duration = _currentSkillAction.AnimationDuration;
                    _currentSkillAction.MarkDirty();
                    OnActionModified?.Invoke(_currentSkillAction);
                }
            }
        }
        
        // === 私有绘制方法（技能专属） ===
        
        /// <summary>
        /// 绘制技能成本区域
        /// </summary>
        private void DrawSkillCost()
        {
            if (_currentSkillAction == null) return;
            
            _skillCostFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_skillCostFoldout, "技能成本");
            
            if (_skillCostFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    // 实际法力消耗
                    EditorGUILayout.LabelField("实际法力消耗", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    int newCost = EditorGUILayout.IntSlider(_currentSkillAction.ActualCost, 0, 1000);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentSkillAction.ActualCost = newCost;
                        _currentSkillAction.MarkDirty();
                        EditorUtility.SetDirty(_currentSkillAction);
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    // 实际冷却（帧）
                    EditorGUILayout.LabelField("实际冷却（帧）", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    int newCooldown = EditorGUILayout.IntSlider(_currentSkillAction.ActualCooldown, 0, 3600);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _currentSkillAction.ActualCooldown = newCooldown;
                        _currentSkillAction.MarkDirty();
                        EditorUtility.SetDirty(_currentSkillAction);
                    }
                    
                    // 显示秒数提示
                    float seconds = _currentSkillAction.ActualCooldown / 60f;
                    EditorGUILayout.LabelField($"= {seconds:F2} 秒 (60帧 = 1秒)", EditorStyles.miniLabel);
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.HelpBox(
                        "💡 实际成本和冷却用于技能系统运行时计算，会覆盖技能表的基础值", 
                        MessageType.Info
                    );
                }
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawHitDummyTemplateSection()
        {
            if (_currentSkillAction == null)
                return;

            EnsureHitDummyCollection();

            if (_hitDummyCollection == null)
            {
                return;
            }

            int beforeCount = _hitDummyCollection.Templates.Count;
            _hitDummyCollection.EnsureDefaultTemplates();
            if (_hitDummyCollection.Templates.Count != beforeCount)
            {
                SaveTemplateChanges();
            }

            if (_hitDummyCollection.Templates == null || _hitDummyCollection.Templates.Count == 0)
            {
                EditorGUILayout.HelpBox("木桩模板集合为空，创建默认模板失败。", MessageType.Warning);
                return;
            }

            _hitDummyFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_hitDummyFoldout, "木桩模板");
            if (_hitDummyFoldout)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    DrawHitDummyTemplateSelector();
                    EditorGUILayout.Space(5);
                    DrawHitDummyTemplateEditor();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawHitDummyTemplateSelector()
        {
            var templates = _hitDummyCollection.Templates;
            if (templates == null || templates.Count == 0)
                return;

            string[] displayNames = new string[templates.Count];
            for (int i = 0; i < templates.Count; i++)
            {
                displayNames[i] = string.IsNullOrEmpty(templates[i].DisplayName)
                    ? $"模板 {i + 1}"
                    : templates[i].DisplayName;
            }

            EditorGUI.BeginChangeCheck();
            _selectedHitDummyIndex = Mathf.Clamp(_selectedHitDummyIndex, 0, templates.Count - 1);
            int newIndex = EditorGUILayout.Popup("当前模板", _selectedHitDummyIndex, displayNames);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedHitDummyIndex = newIndex;
                UpdateCurrentSkillActionTemplateId();
                ApplySelectedTemplateToPreview();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("新增模板", GUILayout.Width(100)))
            {
                CreateNewTemplate();
            }

            GUI.enabled = templates.Count > 1;
            if (GUILayout.Button("删除模板", GUILayout.Width(100)))
            {
                DeleteCurrentTemplate();
            }
            GUI.enabled = true;

            if (GUILayout.Button("复制模板", GUILayout.Width(100)))
            {
                DuplicateCurrentTemplate();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHitDummyTemplateEditor()
        {
            var template = GetSelectedTemplate();
            if (template == null)
                return;

            EditorGUI.BeginChangeCheck();
            template.DisplayName = EditorGUILayout.TextField("模板名称", template.DisplayName);
            template.Notes = EditorGUILayout.TextField("备注", template.Notes);
            template.BasePrefab = EditorGUILayout.ObjectField("默认Prefab", template.BasePrefab, typeof(GameObject), false) as GameObject;
            template.FollowAnchorPosition = EditorGUILayout.Toggle("跟随角色位置", template.FollowAnchorPosition);
            template.FollowAnchorRotation = EditorGUILayout.Toggle("跟随角色朝向", template.FollowAnchorRotation);
            template.LockY = EditorGUILayout.Toggle("锁定Y>=0", template.LockY);
            template.RootOffset = EditorGUILayout.Vector3Field("整体偏移", template.RootOffset);
            template.RootRotation = EditorGUILayout.Vector3Field("整体旋转", template.RootRotation);
            template.RootScale = EditorGUILayout.Vector3Field("整体缩放", template.RootScale);

            if (EditorGUI.EndChangeCheck())
            {
                SaveTemplateChanges();
                ApplySelectedTemplateToPreview();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("木桩列表", EditorStyles.boldLabel);
            _hitDummyScroll = EditorGUILayout.BeginScrollView(_hitDummyScroll, GUILayout.Height(180));
            {
                if (template.Placements == null)
                {
                    template.Placements = new List<SkillHitDummyPlacement>();
                }

                for (int i = 0; i < template.Placements.Count; i++)
                {
                    var placement = template.Placements[i] ?? SkillHitDummyPlacement.CreateDefault($"木桩{i + 1}");
                    bool remove = false;
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.BeginHorizontal();
                        placement.Name = EditorGUILayout.TextField("名称", placement.Name);
                        remove = GUILayout.Button("删除", GUILayout.Width(60));
                        EditorGUILayout.EndHorizontal();

                        if (!remove)
                        {
                            EditorGUI.BeginChangeCheck();
                            placement.OverridePrefab = EditorGUILayout.ObjectField("覆盖Prefab", placement.OverridePrefab, typeof(GameObject), false) as GameObject;
                            placement.Position = EditorGUILayout.Vector3Field("位置", placement.Position);
                            placement.Rotation = EditorGUILayout.Vector3Field("旋转", placement.Rotation);
                            placement.Scale = EditorGUILayout.Vector3Field("缩放", placement.Scale);
                            placement.DebugColor = EditorGUILayout.ColorField("调试颜色", placement.DebugColor);

                            if (EditorGUI.EndChangeCheck())
                            {
                                SaveTemplateChanges();
                                ApplySelectedTemplateToPreview();
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();

                    if (remove)
                    {
                        template.Placements.RemoveAt(i);
                        SaveTemplateChanges();
                        ApplySelectedTemplateToPreview();
                        i--;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("新增木桩", GUILayout.Height(24)))
            {
                template.Placements.Add(SkillHitDummyPlacement.CreateDefault($"木桩{template.Placements.Count + 1}"));
                SaveTemplateChanges();
                ApplySelectedTemplateToPreview();
            }
        }

        private void EnsureHitDummyCollection()
        {
            if (_hitDummyCollection != null) return;

            _hitDummyCollection = SkillHitDummyTemplateService.GetCollection();
            _hitDummyCollection?.EnsureDefaultTemplates();
        }

        private void UpdateSelectedTemplateIndex(bool forceReset = false)
        {
            if (_hitDummyCollection == null || _hitDummyCollection.Templates.Count == 0)
            {
                _selectedHitDummyIndex = -1;
                return;
            }

            if (_currentSkillAction == null)
            {
                _selectedHitDummyIndex = Mathf.Clamp(_selectedHitDummyIndex, 0, _hitDummyCollection.Templates.Count - 1);
                return;
            }

            if (!forceReset && _selectedHitDummyIndex >= 0 && _selectedHitDummyIndex < _hitDummyCollection.Templates.Count)
            {
                return;
            }

            string templateId = _currentSkillAction.HitDummyTemplateId;
            if (string.IsNullOrEmpty(templateId))
            {
                templateId = SkillHitDummyTemplateService.GetLastTemplateId();
            }

            int index = -1;
            for (int i = 0; i < _hitDummyCollection.Templates.Count; i++)
            {
                if (_hitDummyCollection.Templates[i].TemplateId == templateId)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                index = 0;
            }

            _selectedHitDummyIndex = index;
            UpdateCurrentSkillActionTemplateId();
        }

        private SkillHitDummyTemplate GetSelectedTemplate()
        {
            if (_hitDummyCollection == null) return null;
            if (_selectedHitDummyIndex < 0 || _selectedHitDummyIndex >= _hitDummyCollection.Templates.Count)
                return null;

            return _hitDummyCollection.Templates[_selectedHitDummyIndex];
        }

        private void UpdateCurrentSkillActionTemplateId()
        {
            if (_currentSkillAction == null) return;
            var template = GetSelectedTemplate();
            if (template == null) return;

            if (_currentSkillAction.HitDummyTemplateId != template.TemplateId)
            {
                _currentSkillAction.HitDummyTemplateId = template.TemplateId;
                _currentSkillAction.MarkDirty();
                SkillHitDummyTemplateService.SetLastTemplateId(template.TemplateId);
            }
        }

        private void ApplySelectedTemplateToPreview()
        {
            if (_previewModule == null) return;

            var template = GetSelectedTemplate();
            _previewModule.SetHitDummyTemplate(template);
        }

        private void SaveTemplateChanges()
        {
            if (_hitDummyCollection == null) return;

            EditorUtility.SetDirty(_hitDummyCollection);
            SkillHitDummyTemplateService.SaveCollection();
        }

        private void CreateNewTemplate()
        {
            if (_hitDummyCollection == null)
                return;

            var newTemplate = SkillHitDummyTemplate.CreateDefault($"模板{_hitDummyCollection.Templates.Count + 1}");
            _hitDummyCollection.AddTemplate(newTemplate);
            SaveTemplateChanges();
            _selectedHitDummyIndex = _hitDummyCollection.Templates.Count - 1;
            UpdateCurrentSkillActionTemplateId();
            ApplySelectedTemplateToPreview();
        }

        private void DuplicateCurrentTemplate()
        {
            var template = GetSelectedTemplate();
            if (template == null || _hitDummyCollection == null)
                return;

            var copy = template.DeepCopy();
            _hitDummyCollection.AddTemplate(copy);
            SaveTemplateChanges();
            _selectedHitDummyIndex = _hitDummyCollection.Templates.Count - 1;
            UpdateCurrentSkillActionTemplateId();
            ApplySelectedTemplateToPreview();
        }

        private void DeleteCurrentTemplate()
        {
            var template = GetSelectedTemplate();
            if (template == null || _hitDummyCollection == null)
                return;

            if (!EditorUtility.DisplayDialog("删除模板", $"确定要删除模板 {template.DisplayName} 吗？", "删除", "取消"))
            {
                return;
            }

            _hitDummyCollection.RemoveTemplate(template);
            SaveTemplateChanges();
            _selectedHitDummyIndex = Mathf.Clamp(_selectedHitDummyIndex - 1, 0, _hitDummyCollection.Templates.Count - 1);
            UpdateCurrentSkillActionTemplateId();
            ApplySelectedTemplateToPreview();
        }
    }
}

