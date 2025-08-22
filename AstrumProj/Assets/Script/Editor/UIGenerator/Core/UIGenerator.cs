using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Astrum.Editor.UIGenerator.Generators;
using Astrum.Editor.UIGenerator.Utils;

namespace Astrum.Editor.UIGenerator.Core
{
    public class UIGenerator
    {
        private UIGeneratorConfig config;
        private UIGenerationLogger logger;
        
        public UIGenerator()
        {
            config = new UIGeneratorConfig();
            logger = new UIGenerationLogger();
        }
        
        /// <summary>
        /// 从现有Prefab生成UI代码和UIRefs组件
        /// </summary>
        /// <param name="prefabPath">Prefab资源路径</param>
        /// <param name="uiName">UI名称</param>
        /// <returns>生成结果</returns>
        public UIGenerationResult GenerateFromPrefab(string prefabPath, string uiName)
        {
            try
            {
                logger.LogInfo($"开始从Prefab生成UI: {prefabPath}");
                
                // 1. 加载Prefab
                var prefab = LoadPrefab(prefabPath);
                if (prefab == null)
                {
                    return UIGenerationResult.Failed("Prefab加载失败");
                }
                
                // 2. 生成C#代码
                var codeGenerator = new CSharpCodeGenerator(config);
                var codeResult = codeGenerator.GenerateFromPrefab(prefab, uiName);
                if (!codeResult.Success)
                {
                    return UIGenerationResult.Failed($"代码生成失败: {codeResult.ErrorMessage}");
                }
                
                // 3. 生成UIRefs组件
                var refsGenerator = new UIRefsGenerator(config);
                var refsResult = refsGenerator.GenerateFromPrefab(prefab, uiName);
                if (!refsResult.Success)
                {
                    return UIGenerationResult.Failed($"UIRefs生成失败: {refsResult.ErrorMessage}");
                }
                
                logger.LogInfo($"UI生成完成: {uiName}");
                
                return UIGenerationResult.CreateSuccess(new UIGeneratedData
                {
                    Prefab = prefab,
                    CodePath = codeResult.CodePath,
                    UIName = uiName,
                    PrefabPath = prefabPath
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"UI生成过程中发生异常: {ex.Message}\n{ex.StackTrace}");
                return UIGenerationResult.Failed($"生成异常: {ex.Message}");
            }
        }
        
        
        private GameObject LoadPrefab(string prefabPath)
        {
            try
            {
                if (string.IsNullOrEmpty(prefabPath))
                {
                    logger.LogError("Prefab路径为空");
                    return null;
                }
                
                // 加载Prefab资源
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    logger.LogError($"无法加载Prefab: {prefabPath}");
                    return null;
                }
                
                logger.LogInfo($"Prefab加载成功: {prefabPath}");
                return prefab;
            }
            catch (Exception ex)
            {
                logger.LogError($"加载Prefab失败: {ex.Message}");
                return null;
            }
        }
    }
}
