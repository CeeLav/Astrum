using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Astrum.CommonBase;
using Astrum.Network.MessageHandlers;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 消息处理器注册器
    /// 负责注册AstrumClient项目中的消息处理器到Network项目的MessageHandlerDispatcher
    /// </summary>
    public class MessageHandlerRegistry : Singleton<MessageHandlerRegistry>
    {
        private readonly List<IMessageHandler> _handlers = new List<IMessageHandler>();
        
        /// <summary>
        /// 注册所有消息处理器
        /// </summary>
        public void RegisterAllHandlers()
        {
            try
            {
                //ASLogger.Instance.Info("MessageHandlerRegistry: 开始注册消息处理器...");
                
                // 创建所有消息处理器实例
                CreateHandlerInstances();
                
                // 注册到Network项目的MessageHandlerDispatcher
                RegisterToDispatcher();
                
                //ASLogger.Instance.Info($"MessageHandlerRegistry: 成功注册 {_handlers.Count} 个消息处理器");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MessageHandlerRegistry: 注册消息处理器时发生异常 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建消息处理器实例
        /// </summary>
        private void CreateHandlerInstances()
        {
            _handlers.Clear();
            
            // 使用反射自动发现所有带有MessageHandlerAttribute的处理器
            var handlerTypes = GetHandlerTypes();
            
            foreach (var handlerType in handlerTypes)
            {
                try
                {
                    var handler = Activator.CreateInstance(handlerType) as IMessageHandler;
                    if (handler != null)
                    {
                        _handlers.Add(handler);
                        //ASLogger.Instance.Debug($"MessageHandlerRegistry: 注册处理器 - {handlerType.Name}");
                    }
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"MessageHandlerRegistry: 创建处理器 {handlerType.Name} 失败 - {ex.Message}");
                }
            }
            
            //ASLogger.Instance.Info($"MessageHandlerRegistry: 通过反射发现并注册了 {_handlers.Count} 个消息处理器");
        }
        
        /// <summary>
        /// 获取所有带有MessageHandlerAttribute的处理器类型
        /// </summary>
        private List<Type> GetHandlerTypes()
        {
            var handlerTypes = new List<Type>();
            
            try
            {
                // 获取当前程序集中所有类型
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var types = assembly.GetTypes();
                
                //ASLogger.Instance.Info($"MessageHandlerRegistry: 扫描程序集 {assembly.FullName}，发现 {types.Length} 个类型");
                
                foreach (var type in types)
                {
                    // 跳过抽象类和接口
                    if (type.IsAbstract || type.IsInterface)
                        continue;
                    
                    // 检查是否实现了IMessageHandler接口
                    if (!typeof(IMessageHandler).IsAssignableFrom(type))
                        continue;
                    
                    //ASLogger.Instance.Debug($"MessageHandlerRegistry: 发现实现IMessageHandler的类型 - {type.Name}");
                    
                    // 检查是否有MessageHandlerAttribute
                    var attribute = type.GetCustomAttributes(typeof(MessageHandlerAttribute), false)
                        .FirstOrDefault() as MessageHandlerAttribute;
                    
                    if (attribute != null)
                    {
                        handlerTypes.Add(type);
                        //ASLogger.Instance.Debug($"MessageHandlerRegistry: 发现带有MessageHandlerAttribute的处理器 - {type.Name}");
                    }
                    else
                    {
                        //ASLogger.Instance.Debug($"MessageHandlerRegistry: 类型 {type.Name} 没有MessageHandlerAttribute");
                    }
                }
                
                //ASLogger.Instance.Info($"MessageHandlerRegistry: 总共发现 {handlerTypes.Count} 个有效的消息处理器");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MessageHandlerRegistry: 获取处理器类型时发生异常 - {ex.Message}");
            }
            
            return handlerTypes;
        }
        
        
        /// <summary>
        /// 注册到Network项目的MessageHandlerDispatcher
        /// </summary>
        private void RegisterToDispatcher()
        {
            // 通过反射获取Network项目的MessageHandlerDispatcher
            var dispatcherType = Type.GetType("Astrum.Network.MessageHandlers.MessageHandlerDispatcher, Network");
            if (dispatcherType != null)
            {
                var instanceProperty = dispatcherType.GetProperty("Instance");
                if (instanceProperty != null)
                {
                    var dispatcher = instanceProperty.GetValue(null);
                    if (dispatcher != null)
                    {
                        // 调用RegisterExternalHandlers方法
                        var registerMethod = dispatcherType.GetMethod("RegisterExternalHandlers");
                        if (registerMethod != null)
                        {
                            registerMethod.Invoke(dispatcher, new object[] { _handlers });
                            //ASLogger.Instance.Info("MessageHandlerRegistry: 已注册到Network项目的MessageHandlerDispatcher");
                        }
                    }
                }
            }
            else
            {
                ASLogger.Instance.Warning("MessageHandlerRegistry: 无法找到Network项目的MessageHandlerDispatcher");
            }
        }
        
        /// <summary>
        /// 获取所有注册的处理器
        /// </summary>
        public IReadOnlyList<IMessageHandler> GetHandlers()
        {
            return _handlers.AsReadOnly();
        }
    }
}
