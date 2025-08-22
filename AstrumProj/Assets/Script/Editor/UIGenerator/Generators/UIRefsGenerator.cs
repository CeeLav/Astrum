using System;
using System.Collections.Generic;
using UnityEngine;
using Astrum.Editor.UIGenerator.Core;
using Astrum.Editor.UIGenerator.Utils;
using Astrum.Client.UI.Core;

namespace Astrum.Editor.UIGenerator.Generators
{
    public class UIRefsGenerator
    {
        private UIGeneratorConfig config;
        private UIGenerationLogger logger;
        
        public UIRefsGenerator(UIGeneratorConfig config)
        {
            this.config = config;
            this.logger = new UIGenerationLogger();
        }
        
        public UIRefsGenerationResult GenerateFromPrefab(GameObject prefab, string uiName)
        {
            try
            {
                logger.LogInfo($"开始从Prefab生成UIRefs组件: {uiName}");
                
                // 打开Prefab进行编辑
                UnityEditor.AssetDatabase.OpenAsset(prefab);
                logger.LogInfo($"已打开Prefab进行编辑: {prefab.name}");
                
                // 在Prefab编辑场景中获取根节点
                GameObject prefabRoot = GetPrefabRootInEditingScene();
                if (prefabRoot == null)
                {
                    logger.LogError("无法在Prefab编辑场景中找到根节点");
                    return UIRefsGenerationResult.Failed("无法在Prefab编辑场景中找到根节点");
                }
                
                logger.LogInfo($"找到Prefab编辑场景根节点: {prefabRoot.name}");
                
                // 获取或创建UIRefs组件
                var uiRefs = prefabRoot.GetComponent<UIRefs>();
                if (uiRefs == null)
                {
                    uiRefs = prefabRoot.AddComponent<UIRefs>();
                    logger.LogInfo($"在根节点上添加UIRefs组件");
                }
                else
                {
                    logger.LogInfo($"根节点已有UIRefs组件，将更新现有组件");
                }
                
                // 设置UIRefs属性
                uiRefs.SetUIClassName($"{uiName}UI");
                uiRefs.SetUINamespace(config.CodeSettings.Namespace);
                
                // 收集所有引用
                CollectReferencesFromPrefab(uiRefs, prefabRoot);
                
                // 标记预制体为已修改，确保更改被保存
                UnityEditor.EditorUtility.SetDirty(prefabRoot);
                
                logger.LogInfo($"UIRefs组件生成完成");
                
                return UIRefsGenerationResult.CreateSuccess(uiRefs);
            }
            catch (Exception ex)
            {
                logger.LogError($"UIRefs组件生成失败: {ex.Message}");
                return UIRefsGenerationResult.Failed($"UIRefs生成失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 在Prefab编辑场景中获取根节点
        /// </summary>
        private GameObject GetPrefabRootInEditingScene()
        {
            try
            {
                // 检查是否在Prefab编辑模式
                var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage == null)
                {
                    logger.LogError("当前不在Prefab编辑模式");
                    return null;
                }
                
                // 获取Prefab编辑场景的根节点
                var scene = prefabStage.scene;
                var rootObjects = scene.GetRootGameObjects();
                
                if (rootObjects.Length == 0)
                {
                    logger.LogError("Prefab编辑场景中没有根对象");
                    return null;
                }
                
                // 查找Canvas下的UI根节点
                GameObject prefabRoot = null;
                foreach (var rootObj in rootObjects)
                {
                    var canvas = rootObj.GetComponent<Canvas>();
                    if (canvas != null && rootObj.transform.childCount > 0)
                    {
                        // 获取Canvas下的第一个子对象作为UI根节点
                        prefabRoot = rootObj.transform.GetChild(0).gameObject;
                        logger.LogInfo($"找到Canvas下的UI根节点: {prefabRoot.name}");
                        break;
                    }
                }
                
                // 如果没有找到Canvas下的子对象，使用第一个根对象
                if (prefabRoot == null)
                {
                    prefabRoot = rootObjects[0];
                    logger.LogInfo($"使用第一个根对象作为UI根节点: {prefabRoot.name}");
                }
                
                return prefabRoot;
            }
            catch (Exception ex)
            {
                logger.LogError($"获取Prefab编辑场景根节点失败: {ex.Message}");
                return null;
            }
        }
        
        private void CollectReferencesFromPrefab(UIRefs uiRefs, GameObject root)
        {
            var uiRefItems = new List<UIRefItem>();
            
            // 收集所有节点的引用
            CollectAllNodeReferences(root, uiRefItems);
            
            // 直接设置uiRefItems公共字段
            uiRefs.uiRefItems = uiRefItems;
            
            logger.LogInfo($"收集到 {uiRefItems.Count} 个UI引用项");
        }
        
        private void CollectAllNodeReferences(GameObject gameObject, List<UIRefItem> uiRefItems)
        {
            // 收集当前节点的引用
            if (!string.IsNullOrEmpty(gameObject.name))
            {
                // 收集所有MonoBehaviour组件引用
                var components = gameObject.GetComponents<MonoBehaviour>();
                foreach (var component in components)
                {
                    if (component != null && component.GetType() != typeof(UIRefs))
                    {
                        var componentTypeName = component.GetType().Name;
                        var uiRefItem = new UIRefItem
                        {
                            path = gameObject.name,
                            componentType = componentTypeName,
                            reference = component
                        };
                        uiRefItems.Add(uiRefItem);
                    }
                }
                
                // 收集GameObject引用
                var gameObjectRefItem = new UIRefItem
                {
                    path = gameObject.name,
                    componentType = "GameObject",
                    reference = gameObject
                };
                uiRefItems.Add(gameObjectRefItem);
            }
            
            // 递归处理子节点
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var childNode = gameObject.transform.GetChild(i).gameObject;
                CollectAllNodeReferences(childNode, uiRefItems);
            }
        }
        
    }
}