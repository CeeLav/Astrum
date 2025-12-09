using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Astrum.Editor.SceneEditor.Converters
{
    /// <summary>
    /// Trigger参数转换器接口
    /// </summary>
    public interface ITriggerParameterConverter<T> where T : class
    {
        /// <summary>当前版本号</summary>
        int CurrentVersion { get; }
        
        /// <summary>序列化：参数对象 -> 三个数组</summary>
        void Serialize(T parameters, out int[] intParams, out float[] floatParams, out string[] stringParams);
        
        /// <summary>反序列化：三个数组 -> 参数对象</summary>
        T Deserialize(int[] intParams, float[] floatParams, string[] stringParams);
        
        /// <summary>导出为JSON</summary>
        string ExportToJson(T parameters, bool includeMetadata = false);
        
        /// <summary>从JSON导入（带错误报告）</summary>
        JsonImportResult<T> ImportFromJson(string json);
        
        /// <summary>创建默认参数对象</summary>
        T CreateDefault();
    }
    
    /// <summary>JSON导入结果</summary>
    public class JsonImportResult<T>
    {
        public bool Success { get; set; }
        public T Parameters { get; set; }
        public List<ImportWarning> Warnings { get; set; } = new List<ImportWarning>();
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
        public CompatibilityReport CompatibilityReport { get; set; }
    }
    
    public class ImportWarning
    {
        public string Field { get; set; }
        public string Message { get; set; }
        public string Resolution { get; set; }
    }
    
    public class ImportError
    {
        public string Field { get; set; }
        public string Message { get; set; }
    }
    
    public class CompatibilityReport
    {
        public int DetectedVersion { get; set; }
        public int TargetVersion { get; set; }
        public bool WasMigrated { get; set; }
        public List<CompatibilityIssue> Issues { get; set; } = new List<CompatibilityIssue>();
    }
    
    public class CompatibilityIssue
    {
        public string Field { get; set; }
        public string Issue { get; set; }
        public string Resolution { get; set; }
        public Severity Severity { get; set; }
    }
    
    public enum Severity { Info, Warning, Error }
}

