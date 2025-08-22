using UnityEngine;

namespace Astrum.Editor.UIGenerator.Core
{
    public class UIGenerationResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }
        public UIGeneratedData Data { get; private set; }
        
        private UIGenerationResult(bool success, string errorMessage = null, UIGeneratedData data = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
            Data = data;
        }
        
        public static UIGenerationResult CreateSuccess(UIGeneratedData data)
        {
            return new UIGenerationResult(true, null, data);
        }
        
        public static UIGenerationResult Failed(string errorMessage)
        {
            return new UIGenerationResult(false, errorMessage);
        }
    }
    
    public class UIGeneratedData
    {
        public GameObject Prefab;
        public string CodePath;
        public string UIName;
        public string PrefabPath;
        
        public UIGeneratedData()
        {
        }
        
        public UIGeneratedData(GameObject prefab, string codePath, string uiName)
        {
            Prefab = prefab;
            CodePath = codePath;
            UIName = uiName;
        }
    }
    
    public class PrefabGenerationResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }
        public GameObject Prefab { get; private set; }
        
        private PrefabGenerationResult(bool success, string errorMessage = null, GameObject prefab = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
            Prefab = prefab;
        }
        
        public static PrefabGenerationResult CreateSuccess(GameObject prefab)
        {
            return new PrefabGenerationResult(true, null, prefab);
        }
        
        public static PrefabGenerationResult Failed(string errorMessage)
        {
            return new PrefabGenerationResult(false, errorMessage);
        }
    }
    
    public class CodeGenerationResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }
        public string CodePath { get; private set; }
        
        private CodeGenerationResult(bool success, string errorMessage = null, string codePath = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
            CodePath = codePath;
        }
        
        public static CodeGenerationResult CreateSuccess(string codePath)
        {
            return new CodeGenerationResult(true, null, codePath);
        }
        
        public static CodeGenerationResult Failed(string errorMessage)
        {
            return new CodeGenerationResult(false, errorMessage);
        }
    }
    
    public class UIRefsGenerationResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }
        public MonoBehaviour UIRefsComponent { get; private set; }
        
        private UIRefsGenerationResult(bool success, string errorMessage = null, MonoBehaviour uiRefsComponent = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
            UIRefsComponent = uiRefsComponent;
        }
        
        public static UIRefsGenerationResult CreateSuccess(MonoBehaviour uiRefsComponent)
        {
            return new UIRefsGenerationResult(true, null, uiRefsComponent);
        }
        
        public static UIRefsGenerationResult Failed(string errorMessage)
        {
            return new UIRefsGenerationResult(false, errorMessage);
        }
    }
}
