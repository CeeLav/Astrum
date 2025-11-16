using System;
using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
#endif

namespace Astrum.Client.Data
{
    /// <summary>
    /// ParrelSync 辅助类 - 检测和管理 ParrelSync 克隆实例
    /// 使用反射访问 ParrelSync，避免编译时依赖
    /// </summary>
    public static class ParrelSyncHelper
    {
        #if UNITY_EDITOR
        private static bool? _isParrelSyncAvailable = null;
        private static Type _clonesManagerType = null;
        
        /// <summary>
        /// 检查 ParrelSync 是否可用
        /// </summary>
        private static bool IsParrelSyncAvailable()
        {
            if (_isParrelSyncAvailable.HasValue)
            {
                return _isParrelSyncAvailable.Value;
            }
            
            try
            {
                var assembly = System.Reflection.Assembly.Load("ParrelSync");
                _clonesManagerType = assembly?.GetType("ParrelSync.ClonesManager");
                _isParrelSyncAvailable = _clonesManagerType != null;
            }
            catch
            {
                _isParrelSyncAvailable = false;
            }
            
            return _isParrelSyncAvailable.Value;
        }
        #endif
        
        /// <summary>
        /// 检测是否是 ParrelSync 克隆实例
        /// </summary>
        public static bool IsClone()
        {
            #if UNITY_EDITOR
            if (!IsParrelSyncAvailable())
            {
                return false;
            }
            
            try
            {
                var method = _clonesManagerType.GetMethod("IsClone", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    var result = method.Invoke(null, null);
                    return result is bool isClone && isClone;
                }
            }
            catch
            {
                // ParrelSync 调用失败时返回 false
            }
            return false;
            #else
            return false;
            #endif
        }
        
        /// <summary>
        /// 获取当前实例的唯一标识符
        /// </summary>
        public static string GetInstanceId()
        {
            #if UNITY_EDITOR
            if (!IsParrelSyncAvailable())
            {
                return "Main";
            }
            
            try
            {
                if (IsClone())
                {
                    var method = _clonesManagerType.GetMethod("GetArgument", BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                    {
                        var result = method.Invoke(null, null);
                        var argument = result?.ToString();
                        return string.IsNullOrEmpty(argument) ? "Clone_Unknown" : argument;
                    }
                }
            }
            catch
            {
                // ParrelSync 调用失败时使用默认值
            }
            #endif
            return "Main";
        }
    }
}

