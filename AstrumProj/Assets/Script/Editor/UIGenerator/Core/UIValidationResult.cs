using System.Collections.Generic;

namespace Astrum.Editor.UIGenerator.Core
{
    /// <summary>
    /// UI验证结果
    /// </summary>
    public class UIValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        
        public static UIValidationResult Success()
        {
            return new UIValidationResult
            {
                IsValid = true
            };
        }
        
        public static UIValidationResult Failed(string errorMessage)
        {
            return new UIValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }
}

