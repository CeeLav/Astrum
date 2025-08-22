using UnityEngine;

namespace Astrum.Editor.UIGenerator.Core
{
    [System.Serializable]
    public class UIGeneratorConfig
    {
        // Prefab搜索路径
        public static readonly string PREFAB_SEARCH_PATH = "Assets/";
        
        
        // 生成的代码路径
        public static readonly string CODE_OUTPUT_PATH = "Assets/Script/AstrumClient/UI/Generated";
        
        
        // 代码生成设置
        public CodeGenerationSettings CodeSettings = new CodeGenerationSettings
        {
            Namespace = "Astrum.Client.UI.Generated",
            BaseClassName = "UIBase",
            GenerateComments = true,
            GenerateRegions = true,
            UseRegions = true
        };
    }
    
    
    [System.Serializable]
    public class CodeGenerationSettings
    {
        public string Namespace = "Astrum.Client.UI.Generated";
        public string BaseClassName = "UIBase";
        public bool GenerateComments = true;
        public bool GenerateRegions = true;
        public bool UseRegions = true;
        
        // 新增：支持partial class分离
        public bool UsePartialClass = true;
        public string DesignerFileSuffix = ".designer.cs";
        public string LogicFileSuffix = ".cs";
    }
}
