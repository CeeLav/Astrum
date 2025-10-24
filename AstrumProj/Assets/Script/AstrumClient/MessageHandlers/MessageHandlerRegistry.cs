using System;
using System.Collections.Generic;
using System.Linq;
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
            
            // 登录相关
            _handlers.Add(new LoginMessageHandler());
            
            // 房间相关
            _handlers.Add(new CreateRoomResponseHandler());
            _handlers.Add(new JoinRoomResponseHandler());
            _handlers.Add(new LeaveRoomResponseHandler());
            _handlers.Add(new GetRoomListResponseHandler());
            _handlers.Add(new RoomUpdateNotificationHandler());
            
            // TODO: 添加其他消息处理器
            // _handlers.Add(new GameMessageHandler());
            // _handlers.Add(new FrameSyncMessageHandler());
            // _handlers.Add(new MatchMessageHandler());
            // _handlers.Add(new HeartbeatMessageHandler());
            // _handlers.Add(new ConnectMessageHandler());
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
                        // 调用RegisterHandlers方法
                        var registerMethod = dispatcherType.GetMethod("RegisterHandlers");
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
