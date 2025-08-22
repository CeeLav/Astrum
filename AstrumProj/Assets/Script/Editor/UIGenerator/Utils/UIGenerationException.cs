using System;

namespace Astrum.Editor.UIGenerator.Utils
{
    public class UIGenerationException : Exception
    {
        public string ErrorCode { get; private set; }
        public string Operation { get; private set; }
        public object Context { get; private set; }
        
        public UIGenerationException(string message) : base(message)
        {
        }
        
        public UIGenerationException(string message, Exception innerException) : base(message, innerException)
        {
        }
        
        public UIGenerationException(string errorCode, string operation, string message, object context = null) 
            : base(message)
        {
            ErrorCode = errorCode;
            Operation = operation;
            Context = context;
        }
        
        public UIGenerationException(string errorCode, string operation, string message, Exception innerException, object context = null) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Operation = operation;
            Context = context;
        }
        
        public override string ToString()
        {
            var result = $"UIGenerationException: {Message}";
            
            if (!string.IsNullOrEmpty(ErrorCode))
            {
                result += $"\nError Code: {ErrorCode}";
            }
            
            if (!string.IsNullOrEmpty(Operation))
            {
                result += $"\nOperation: {Operation}";
            }
            
            if (Context != null)
            {
                result += $"\nContext: {Context}";
            }
            
            if (InnerException != null)
            {
                result += $"\nInner Exception: {InnerException}";
            }
            
            result += $"\nStackTrace: {StackTrace}";
            
            return result;
        }
    }
    
    public static class UIGenerationErrorCodes
    {
        // JSON解析错误
        public const string JSON_PARSE_ERROR = "JSON_PARSE_ERROR";
        public const string JSON_VALIDATION_ERROR = "JSON_VALIDATION_ERROR";
        
        // 配置错误
        public const string CONFIG_ERROR = "CONFIG_ERROR";
        public const string INVALID_UI_NAME = "INVALID_UI_NAME";
        public const string INVALID_PREFAB_PATH = "INVALID_PREFAB_PATH";
        
        // 组件错误
        public const string UNSUPPORTED_COMPONENT = "UNSUPPORTED_COMPONENT";
        public const string COMPONENT_CREATION_ERROR = "COMPONENT_CREATION_ERROR";
        public const string COMPONENT_PROPERTY_ERROR = "COMPONENT_PROPERTY_ERROR";
        
        // 生成错误
        public const string PREFAB_GENERATION_ERROR = "PREFAB_GENERATION_ERROR";
        public const string CODE_GENERATION_ERROR = "CODE_GENERATION_ERROR";
        public const string UIRefs_GENERATION_ERROR = "UIRefs_GENERATION_ERROR";
        
        // 文件操作错误
        public const string FILE_READ_ERROR = "FILE_READ_ERROR";
        public const string FILE_WRITE_ERROR = "FILE_WRITE_ERROR";
        public const string DIRECTORY_CREATE_ERROR = "DIRECTORY_CREATE_ERROR";
        
        // 验证错误
        public const string STRUCTURE_VALIDATION_ERROR = "STRUCTURE_VALIDATION_ERROR";
        public const string COMPONENT_VALIDATION_ERROR = "COMPONENT_VALIDATION_ERROR";
        
        // 系统错误
        public const string UNITY_EDITOR_ERROR = "UNITY_EDITOR_ERROR";
        public const string REFLECTION_ERROR = "REFLECTION_ERROR";
        public const string UNKNOWN_ERROR = "UNKNOWN_ERROR";
    }
}
