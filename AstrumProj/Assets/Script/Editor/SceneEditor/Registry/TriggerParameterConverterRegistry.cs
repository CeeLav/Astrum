using System.Collections.Generic;
using Astrum.Editor.SceneEditor.Converters;
using Astrum.Editor.SceneEditor.Converters.Actions;
using Astrum.Editor.SceneEditor.Converters.Conditions;
using cfg;

namespace Astrum.Editor.SceneEditor.Registry
{
    public static class TriggerParameterConverterRegistry
    {
        private static readonly Dictionary<TriggerConditionType, object> ConditionConverters = new Dictionary<TriggerConditionType, object>();
        private static readonly Dictionary<TriggerActionType, object> ActionConverters = new Dictionary<TriggerActionType, object>();
        
        static TriggerParameterConverterRegistry()
        {
            // 注册条件转换器
            RegisterCondition(TriggerConditionType.SceneStart, new SceneStartParameterConverter());
            RegisterCondition(TriggerConditionType.Delay, new DelayParameterConverter());
            RegisterCondition(TriggerConditionType.TriggerEvent, new TriggerEventParameterConverter());
            
            // 注册动作转换器
            RegisterAction(TriggerActionType.SpawnEntity, new SpawnEntityParameterConverter());
            RegisterAction(TriggerActionType.PlayEffect, new PlayEffectParameterConverter());
        }
        
        public static void RegisterCondition<T>(TriggerConditionType type, ITriggerParameterConverter<T> converter) where T : class
        {
            ConditionConverters[type] = converter;
        }
        
        public static void RegisterAction<T>(TriggerActionType type, ITriggerParameterConverter<T> converter) where T : class
        {
            ActionConverters[type] = converter;
        }
        
        public static ITriggerParameterConverter<T> GetConditionConverter<T>(TriggerConditionType type) where T : class
        {
            return ConditionConverters.TryGetValue(type, out var converter) 
                ? converter as ITriggerParameterConverter<T> 
                : null;
        }
        
        public static ITriggerParameterConverter<T> GetActionConverter<T>(TriggerActionType type) where T : class
        {
            return ActionConverters.TryGetValue(type, out var converter) 
                ? converter as ITriggerParameterConverter<T> 
                : null;
        }
        
        /// <summary>
        /// 获取条件转换器（返回object，需要调用者转换）
        /// </summary>
        public static object GetConditionConverter(TriggerConditionType type)
        {
            return ConditionConverters.TryGetValue(type, out var converter) ? converter : null;
        }
        
        /// <summary>
        /// 获取动作转换器（返回object，需要调用者转换）
        /// </summary>
        public static object GetActionConverter(TriggerActionType type)
        {
            return ActionConverters.TryGetValue(type, out var converter) ? converter : null;
        }
    }
}

