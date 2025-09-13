using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Astrum.Editor.UIGenerator.Core;
using Astrum.Editor.UIGenerator.Utils;
using Astrum.Client.UI.Core;

namespace Astrum.Editor.UIGenerator.Generators
{
    public class CSharpCodeGenerator
    {
        private UIGeneratorConfig config;
        private UIGenerationLogger logger;
        
        public CSharpCodeGenerator(UIGeneratorConfig config)
        {
            this.config = config;
            this.logger = new UIGenerationLogger();
        }
        
        public CodeGenerationResult GenerateFromPrefab(GameObject prefab, string uiName)
        {
            try
            {
                logger.LogInfo($"开始从Prefab生成C#代码: {uiName}");
                
                if (config.CodeSettings.UsePartialClass)
                {
                    return GeneratePartialClassFiles(prefab, uiName);
                }
                else
                {
                    return GenerateSingleClassFile(prefab, uiName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"C#代码生成失败: {ex.Message}");
                return CodeGenerationResult.Failed($"代码生成失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 生成单个类文件（原有逻辑）
        /// </summary>
        private CodeGenerationResult GenerateSingleClassFile(GameObject prefab, string uiName)
        {
            // 生成代码内容
            string codeContent = GenerateCodeContentFromPrefab(prefab, uiName);
            
            // 确定输出路径
            string outputPath = Path.Combine(UIGeneratorConfig.CODE_OUTPUT_PATH, $"{uiName}UI.cs");
            
            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            
            // 写入文件
            File.WriteAllText(outputPath, codeContent, Encoding.UTF8);
            
            logger.LogInfo($"C#代码生成完成: {outputPath}");
            
            return CodeGenerationResult.CreateSuccess(outputPath);
        }
        
        /// <summary>
        /// 生成partial class文件（设计器文件 + 逻辑文件）
        /// </summary>
        private CodeGenerationResult GeneratePartialClassFiles(GameObject prefab, string uiName)
        {
            string className = $"{uiName}View";
            
            // 生成设计器文件
            string designerContent = GenerateDesignerFileContent(prefab, className);
            string designerPath = Path.Combine(UIGeneratorConfig.CODE_OUTPUT_PATH, $"{className}{config.CodeSettings.DesignerFileSuffix}");
            
            // 生成逻辑文件（如果不存在）
            string logicContent = GenerateLogicFileContent(className);
            string logicPath = Path.Combine(UIGeneratorConfig.CODE_OUTPUT_PATH, $"{className}{config.CodeSettings.LogicFileSuffix}");
            
            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(designerPath));
            
            // 写入设计器文件（总是覆盖）
            File.WriteAllText(designerPath, designerContent, Encoding.UTF8);
            logger.LogInfo($"设计器文件生成完成: {designerPath}");
            
            // 写入逻辑文件（仅当不存在时）
            if (!File.Exists(logicPath))
            {
                File.WriteAllText(logicPath, logicContent, Encoding.UTF8);
                logger.LogInfo($"逻辑文件生成完成: {logicPath}");
            }
            else
            {
                logger.LogInfo($"逻辑文件已存在，跳过生成: {logicPath}");
            }
            
            return CodeGenerationResult.CreateSuccess(designerPath);
        }
        
        private string GenerateCodeContentFromPrefab(GameObject prefab, string uiName)
        {
            var codeBuilder = new StringBuilder();
            
            // 添加using语句
            codeBuilder.AppendLine("using UnityEngine;");
            codeBuilder.AppendLine("using UnityEngine.UI;");
            codeBuilder.AppendLine("using System;");
            codeBuilder.AppendLine("using Astrum.Client.UI.Core;");
            codeBuilder.AppendLine();
            
            // 添加命名空间
            codeBuilder.AppendLine($"namespace {config.CodeSettings.Namespace}");
            codeBuilder.AppendLine("{");
            
            // 添加类定义
            codeBuilder.AppendLine($"    /// <summary>");
            codeBuilder.AppendLine($"    /// {uiName} UI逻辑类");
            codeBuilder.AppendLine($"    /// 由UI生成器自动生成");
            codeBuilder.AppendLine($"    /// </summary>");
            codeBuilder.AppendLine($"    public class {uiName}UI");
            codeBuilder.AppendLine("    {");
            
            // 添加字段
            codeBuilder.AppendLine("        #region Fields");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        // UI引用");
            codeBuilder.AppendLine("        private UIRefs uiRefs;");
            codeBuilder.AppendLine();
            
            // 生成UI元素引用字段
            GenerateUIElementFields(codeBuilder, prefab);
            
            codeBuilder.AppendLine("        #endregion");
            codeBuilder.AppendLine();
            
            // 添加属性
            codeBuilder.AppendLine("        #region Properties");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        public bool IsInitialized => uiRefs != null;");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        #endregion");
            codeBuilder.AppendLine();
            
            // 添加方法
            codeBuilder.AppendLine("        #region Methods");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 初始化UI");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        public void Initialize(UIRefs refs)");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            uiRefs = refs;");
            codeBuilder.AppendLine("            InitializeUIElements();");
            codeBuilder.AppendLine("            OnInitialize();");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            
            // 生成UI元素初始化方法
            GenerateUIElementInitialization(codeBuilder, prefab);
            
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 初始化完成后的回调");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        protected virtual void OnInitialize()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            // 在这里添加初始化逻辑");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 显示UI");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        public virtual void Show()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            if (uiRefs != null)");
            codeBuilder.AppendLine("            {");
            codeBuilder.AppendLine("                uiRefs.gameObject.SetActive(true);");
            codeBuilder.AppendLine("                OnShow();");
            codeBuilder.AppendLine("            }");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 隐藏UI");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        public virtual void Hide()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            if (uiRefs != null)");
            codeBuilder.AppendLine("            {");
            codeBuilder.AppendLine("                OnHide();");
            codeBuilder.AppendLine("                uiRefs.gameObject.SetActive(false);");
            codeBuilder.AppendLine("            }");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 显示时的回调");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        protected virtual void OnShow()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            // 在这里添加显示逻辑");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 隐藏时的回调");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        protected virtual void OnHide()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            // 在这里添加隐藏逻辑");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        #endregion");
            codeBuilder.AppendLine("    }");
            codeBuilder.AppendLine("}");
            
            return codeBuilder.ToString();
        }
        
        private void GenerateUIElementFields(StringBuilder codeBuilder, GameObject prefab)
        {
            var uiElements = new List<string>();
            CollectUIElements(prefab, uiElements);
            
            foreach (var element in uiElements)
            {
                codeBuilder.AppendLine($"        // {element}");
            }
        }
        
        private void GenerateUIElementInitialization(StringBuilder codeBuilder, GameObject prefab)
        {
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 初始化UI元素引用");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        private void InitializeUIElements()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            // 在这里添加UI元素引用初始化");
            codeBuilder.AppendLine("            // 例如: button = uiRefs.GetComponent<Button>(\"ButtonName\");");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
        }
        
        private void CollectUIElements(GameObject gameObject, List<string> uiElements)
        {
            // 收集当前节点的UI元素
            var components = gameObject.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component != null && component.GetType().Name != "UIRefs")
                {
                    var componentType = component.GetType().Name;
                    var elementName = gameObject.name;
                    uiElements.Add($"{componentType} {elementName};");
                }
            }
            
            // 递归处理子节点
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var childNode = gameObject.transform.GetChild(i).gameObject;
                CollectUIElements(childNode, uiElements);
            }
        }
        
        /// <summary>
        /// 生成设计器文件内容（.designer.cs）
        /// </summary>
        private string GenerateDesignerFileContent(GameObject prefab, string className)
        {
            var codeBuilder = new StringBuilder();
            
            // 添加文件头注释
            codeBuilder.AppendLine("// <auto-generated>");
            codeBuilder.AppendLine("// 此文件由UI生成器自动生成，请勿手动修改");
            codeBuilder.AppendLine("// </auto-generated>");
            codeBuilder.AppendLine();
            
            // 添加using语句
            codeBuilder.AppendLine("using UnityEngine;");
            codeBuilder.AppendLine("using UnityEngine.UI;");
            codeBuilder.AppendLine("using System;");
            codeBuilder.AppendLine("using Astrum.Client.UI.Core;");
            codeBuilder.AppendLine();
            
            // 添加命名空间
            codeBuilder.AppendLine($"namespace {config.CodeSettings.Namespace}");
            codeBuilder.AppendLine("{");
            
            // 添加类定义
            codeBuilder.AppendLine($"    /// <summary>");
            codeBuilder.AppendLine($"    /// {className} 设计器部分");
            codeBuilder.AppendLine($"    /// 由UI生成器自动生成，包含UI元素引用和初始化逻辑");
            codeBuilder.AppendLine($"    /// </summary>");
            codeBuilder.AppendLine($"    public partial class {className}");
            codeBuilder.AppendLine("    {");
            
            // 添加字段
            codeBuilder.AppendLine("        #region UI References");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        // UI引用");
            codeBuilder.AppendLine("        private UIRefs uiRefs;");
            codeBuilder.AppendLine();
            
            // 生成UI元素引用字段
            GenerateUIElementFieldsForDesigner(codeBuilder, prefab);
            
            codeBuilder.AppendLine("        #endregion");
            codeBuilder.AppendLine();
            
            // 添加属性
            codeBuilder.AppendLine("        #region Properties");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        public bool IsInitialized => uiRefs != null;");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        #endregion");
            codeBuilder.AppendLine();
            
            // 添加初始化方法
            codeBuilder.AppendLine("        #region Initialization");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 初始化UI");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        public void Initialize(UIRefs refs)");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            uiRefs = refs;");
            codeBuilder.AppendLine("            InitializeUIElements();");
            codeBuilder.AppendLine("            OnInitialize();");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            
            // 生成UI元素初始化方法
            GenerateUIElementInitializationForDesigner(codeBuilder, prefab);
            
            codeBuilder.AppendLine("        #endregion");
            codeBuilder.AppendLine();
            
            // 添加基础方法
            codeBuilder.AppendLine("        #region Basic Methods");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 显示UI");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        public virtual void Show()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            if (uiRefs != null)");
            codeBuilder.AppendLine("            {");
            codeBuilder.AppendLine("                uiRefs.gameObject.SetActive(true);");
            codeBuilder.AppendLine("                OnShow();");
            codeBuilder.AppendLine("            }");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 隐藏UI");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        public virtual void Hide()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            if (uiRefs != null)");
            codeBuilder.AppendLine("            {");
            codeBuilder.AppendLine("                OnHide();");
            codeBuilder.AppendLine("                uiRefs.gameObject.SetActive(false);");
            codeBuilder.AppendLine("            }");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        #endregion");
            codeBuilder.AppendLine("    }");
            codeBuilder.AppendLine("}");
            
            return codeBuilder.ToString();
        }
        
        /// <summary>
        /// 生成逻辑文件内容（.cs）
        /// </summary>
        private string GenerateLogicFileContent(string className)
        {
            var codeBuilder = new StringBuilder();
            
            // 添加文件头注释
            codeBuilder.AppendLine("// 此文件用于编写UI逻辑代码");
            codeBuilder.AppendLine("// 第一次生成后，可以手动编辑，不会被重新生成覆盖");
            codeBuilder.AppendLine();
            
            // 添加using语句
            codeBuilder.AppendLine("using UnityEngine;");
            codeBuilder.AppendLine("using UnityEngine.UI;");
            codeBuilder.AppendLine("using System;");
            codeBuilder.AppendLine("using Astrum.Client.UI.Core;");
            codeBuilder.AppendLine();
            
            // 添加命名空间
            codeBuilder.AppendLine($"namespace {config.CodeSettings.Namespace}");
            codeBuilder.AppendLine("{");
            
            // 添加类定义
            codeBuilder.AppendLine($"    /// <summary>");
            codeBuilder.AppendLine($"    /// {className} 逻辑部分");
            codeBuilder.AppendLine($"    /// 用于编写UI的业务逻辑代码");
            codeBuilder.AppendLine($"    /// </summary>");
            codeBuilder.AppendLine($"    public partial class {className}");
            codeBuilder.AppendLine("    {");
            
            // 添加虚方法供重写
            codeBuilder.AppendLine("        #region Virtual Methods");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 初始化完成后的回调");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        protected virtual void OnInitialize()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            // 在这里添加初始化逻辑");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 显示时的回调");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        protected virtual void OnShow()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            // 在这里添加显示逻辑");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 隐藏时的回调");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        protected virtual void OnHide()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            // 在这里添加隐藏逻辑");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        #endregion");
            codeBuilder.AppendLine();
            
            // 添加业务逻辑区域
            codeBuilder.AppendLine("        #region Business Logic");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        // 在这里添加业务逻辑方法");
            codeBuilder.AppendLine("        // 例如：");
            codeBuilder.AppendLine("        // public void OnButtonClick() { }");
            codeBuilder.AppendLine("        // public void OnInputFieldChanged(string value) { }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        #endregion");
            codeBuilder.AppendLine("    }");
            codeBuilder.AppendLine("}");
            
            return codeBuilder.ToString();
        }
        
        /// <summary>
        /// 为设计器文件生成UI元素字段
        /// </summary>
        private void GenerateUIElementFieldsForDesigner(StringBuilder codeBuilder, GameObject prefab)
        {
            var uiElements = new List<UIElementInfo>();
            CollectUIElementsForDesigner(prefab, uiElements, "");
            
            foreach (var element in uiElements)
            {
                if (element.ComponentType != null)
                {
                    codeBuilder.AppendLine($"        // {element.Path}");
                    codeBuilder.AppendLine($"        private {element.ComponentType.Name} {element.FieldName};");
                    codeBuilder.AppendLine();
                }
            }
        }
        
        /// <summary>
        /// 为设计器文件生成UI元素初始化方法
        /// </summary>
        private void GenerateUIElementInitializationForDesigner(StringBuilder codeBuilder, GameObject prefab)
        {
            codeBuilder.AppendLine("        /// <summary>");
            codeBuilder.AppendLine("        /// 初始化UI元素引用");
            codeBuilder.AppendLine("        /// </summary>");
            codeBuilder.AppendLine("        private void InitializeUIElements()");
            codeBuilder.AppendLine("        {");
            
            var uiElements = new List<UIElementInfo>();
            CollectUIElementsForDesigner(prefab, uiElements, "");
            
            foreach (var element in uiElements)
            {
                if (element.ComponentType != null)
                {
                    codeBuilder.AppendLine($"            {element.FieldName} = uiRefs.GetComponent<{element.ComponentType.Name}>(\"{element.Path}\");");
                }
            }
            
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
        }
        
        /// <summary>
        /// 为设计器文件收集UI元素信息
        /// </summary>
        private void CollectUIElementsForDesigner(GameObject gameObject, List<UIElementInfo> uiElements, string path)
        {
            string currentPath = string.IsNullOrEmpty(path) ? gameObject.name : $"{path}/{gameObject.name}";
            
            // 收集当前节点的UI组件
            var components = gameObject.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component != null && 
                    component.GetType() != typeof(Transform) && 
                    component.GetType() != typeof(UIRefs) &&
                    (component is Button || component is Text || component is Image || 
                     component is InputField || component is ScrollRect || component is Toggle ||
                     component is Slider || component is Dropdown))
                {
                    var elementInfo = new UIElementInfo
                    {
                        Path = currentPath,
                        ComponentType = component.GetType(),
                        FieldName = GetFieldName(gameObject.name, component.GetType())
                    };
                    uiElements.Add(elementInfo);
                }
            }
            
            // 递归处理子节点
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var childNode = gameObject.transform.GetChild(i).gameObject;
                CollectUIElementsForDesigner(childNode, uiElements, currentPath);
            }
        }
        
        /// <summary>
        /// 获取字段名称
        /// </summary>
        private string GetFieldName(string objectName, Type componentType)
        {
            string baseName = objectName.Replace(" ", "").Replace("-", "").Replace("_", "");
            string typeName = componentType.Name;
            
            // 转换为驼峰命名
            if (baseName.Length > 0)
            {
                baseName = char.ToLower(baseName[0]) + baseName.Substring(1);
            }
            
            return $"{baseName}{typeName}";
        }
        
        /// <summary>
        /// UI元素信息
        /// </summary>
        private class UIElementInfo
        {
            public string Path;
            public Type ComponentType;
            public string FieldName;
        }
    }
}
