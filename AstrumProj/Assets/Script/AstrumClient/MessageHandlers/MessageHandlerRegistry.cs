using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Astrum.CommonBase;

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
                ASLogger.Instance.Info("MessageHandlerRegistry: 开始注册消息处理器...");
                
                // 创建所有消息处理器实例
                CreateHandlerInstances();
                
                // 注册到Network项目的MessageHandlerDispatcher
                RegisterToDispatcher();
                
                ASLogger.Instance.Info($"MessageHandlerRegistry: 成功注册 {_handlers.Count} 个消息处理器");
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
            
            try
            {
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
                            ASLogger.Instance.Debug($"MessageHandlerRegistry: 注册处理器 - {handlerType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        ASLogger.Instance.Error($"MessageHandlerRegistry: 创建处理器 {handlerType.Name} 失败 - {ex.Message}");
                    }
                }
                
                ASLogger.Instance.Info($"MessageHandlerRegistry: 通过反射发现并注册了 {_handlers.Count} 个消息处理器");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MessageHandlerRegistry: 反射发现处理器时发生异常 - {ex.Message}");
                
                // 如果反射失败，回退到手动注册
                RegisterHandlersManually();
            }
        }
        
        /// <summary>
        /// 获取所有带有MessageHandlerAttribute的处理器类型
        /// </summary>
        private List<Type> GetHandlerTypes()
        {
            var handlerTypes = new List<Type>();
            
            // 获取当前程序集中所有类型
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();
            
            foreach (var type in types)
            {
                // 检查是否实现了IMessageHandler接口
                if (!typeof(IMessageHandler).IsAssignableFrom(type))
                    continue;
                    
                // 检查是否有MessageHandlerAttribute
                var attribute = type.GetCustomAttributes(typeof(MessageHandlerAttribute), false)
                    .FirstOrDefault() as MessageHandlerAttribute;
                    
                if (attribute != null)
                {
                    handlerTypes.Add(type);
                }
            }
            
            return handlerTypes;
        }
        
        /// <summary>
        /// 手动注册处理器（作为反射失败时的回退方案）
        /// </summary>
        private void RegisterHandlersManually()
        {
            ASLogger.Instance.Info("MessageHandlerRegistry: 使用手动注册方式");
            
            // 登录相关
            _handlers.Add(new LoginMessageHandler());
            
            // 房间相关
            _handlers.Add(new CreateRoomResponseHandler());
            _handlers.Add(new JoinRoomResponseHandler());
            _handlers.Add(new LeaveRoomResponseHandler());
            _handlers.Add(new GetRoomListResponseHandler());
            _handlers.Add(new RoomUpdateNotificationHandler());
            
            // 连接相关
            _handlers.Add(new ConnectMessageHandler());
            
            // 心跳相关
            _handlers.Add(new HeartbeatMessageHandler());
            
            // 匹配相关
            _handlers.Add(new QuickMatchResponseHandler());
            _handlers.Add(new CancelMatchResponseHandler());
            _handlers.Add(new MatchFoundNotificationHandler());
            _handlers.Add(new MatchTimeoutNotificationHandler());
            
            // 游戏相关
            _handlers.Add(new GameResponseHandler());
            _handlers.Add(new GameStartNotificationHandler());
            _handlers.Add(new GameEndNotificationHandler());
            
            // 帧同步相关
            _handlers.Add(new FrameSyncStartNotificationHandler());
            _handlers.Add(new FrameSyncEndNotificationHandler());
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
                            ASLogger.Instance.Info("MessageHandlerRegistry: 已注册到Network项目的MessageHandlerDispatcher");
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
