using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Astrum.CommonBase;

namespace Astrum.Network.MessageHandlers
{
    /// <summary>
    /// 消息处理器信息
    /// </summary>
    public class MessageHandlerInfo
    {
        public IMessageHandler Handler { get; }
        public int Priority { get; }
        public bool Enabled { get; }
        
        public MessageHandlerInfo(IMessageHandler handler, int priority = 0, bool enabled = true)
        {
            Handler = handler;
            Priority = priority;
            Enabled = enabled;
        }
    }
    
    /// <summary>
    /// 消息处理器分发器
    /// 负责注册和分发消息到对应的处理器
    /// </summary>
    public class MessageHandlerDispatcher : Singleton<MessageHandlerDispatcher>
    {
        private readonly Dictionary<Type, List<MessageHandlerInfo>> _handlers = new();
        private bool _isInitialized = false;
        
        /// <summary>
        /// 初始化消息处理器分发器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                ASLogger.Instance.Warning("MessageHandlerDispatcher already initialized");
                return;
            }
            
            try
            {
                RegisterHandlers();
                _isInitialized = true;
                ASLogger.Instance.Info($"MessageHandlerDispatcher initialized with {_handlers.Count} message types");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Failed to initialize MessageHandlerDispatcher: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterHandlers()
        {
            // 获取所有带有MessageHandlerAttribute的类
            var handlerTypes = CodeTypes.Instance.GetTypes(typeof(MessageHandlerAttribute));
            
            foreach (var handlerType in handlerTypes)
            {
                try
                {
                    RegisterHandler(handlerType);
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"Failed to register handler {handlerType.Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 注册外部提供的消息处理器列表
        /// </summary>
        /// <param name="handlers">处理器列表</param>
        public void RegisterExternalHandlers(IEnumerable<IMessageHandler> handlers)
        {
            if (handlers == null)
            {
                ASLogger.Instance.Warning("MessageHandlerDispatcher: 传入的处理器列表为空");
                return;
            }
            
            foreach (var handler in handlers)
            {
                try
                {
                    RegisterHandlerInstance(handler);
                }
                catch (Exception ex)
                {
                    ASLogger.Instance.Error($"Failed to register handler {handler.GetType().Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 注册单个消息处理器
        /// </summary>
        /// <param name="handlerType">处理器类型</param>
        private void RegisterHandler(Type handlerType)
        {
            // 检查是否实现了IMessageHandler接口
            if (!typeof(IMessageHandler).IsAssignableFrom(handlerType))
            {
                ASLogger.Instance.Warning($"Handler {handlerType.Name} does not implement IMessageHandler");
                return;
            }
            
            // 获取MessageHandlerAttribute
            var attribute = handlerType.GetCustomAttributes(typeof(MessageHandlerAttribute), false)
                .FirstOrDefault() as MessageHandlerAttribute;
            
            if (attribute == null)
            {
                ASLogger.Instance.Warning($"Handler {handlerType.Name} does not have MessageHandlerAttribute");
                return;
            }
            
            // MessageHandlerAttribute 没有 Enabled 属性，所以总是启用
            
            // 创建处理器实例
            var handler = Activator.CreateInstance(handlerType) as IMessageHandler;
            if (handler == null)
            {
                ASLogger.Instance.Error($"Failed to create instance of handler {handlerType.Name}");
                return;
            }
            
            // 验证消息类型
            var messageType = handler.GetMessageType();
            if (attribute.MessageType != null && attribute.MessageType != messageType)
            {
                ASLogger.Instance.Warning($"Handler {handlerType.Name} message type mismatch: expected {attribute.MessageType.Name}, got {messageType.Name}");
            }
            
            // 注册处理器
            RegisterHandler(messageType, handler, 0, true);
            
            ASLogger.Instance.Info($"Registered handler {handlerType.Name} for message type {messageType.Name}");
        }
        
        /// <summary>
        /// 注册处理器实例
        /// </summary>
        /// <param name="handler">处理器实例</param>
        private void RegisterHandlerInstance(IMessageHandler handler)
        {
            if (handler == null)
            {
                ASLogger.Instance.Warning("MessageHandlerDispatcher: 处理器实例为空");
                return;
            }
            
            // 获取MessageHandlerAttribute
            var attribute = handler.GetType().GetCustomAttributes(typeof(MessageHandlerAttribute), false)
                .FirstOrDefault() as MessageHandlerAttribute;
            
            if (attribute == null)
            {
                ASLogger.Instance.Warning($"Handler {handler.GetType().Name} does not have MessageHandlerAttribute");
                return;
            }
            
            // MessageHandlerAttribute 没有 Enabled 属性，所以总是启用
            
            // 验证消息类型
            var messageType = handler.GetMessageType();
            if (attribute.MessageType != null && attribute.MessageType != messageType)
            {
                ASLogger.Instance.Warning($"Handler {handler.GetType().Name} message type mismatch: expected {attribute.MessageType.Name}, got {messageType.Name}");
            }
            
            // 注册处理器
            RegisterHandler(messageType, handler, 0, true);
            
            ASLogger.Instance.Info($"Registered handler {handler.GetType().Name} for message type {messageType.Name}");
        }
        
        /// <summary>
        /// 注册外部提供的消息处理器列表
        /// </summary>
        /// <param name="handlers">消息处理器列表</param>
        public void RegisterHandlers(IEnumerable<IMessageHandler> handlers)
        {
            try
            {
                ASLogger.Instance.Info($"MessageHandlerDispatcher: 注册外部消息处理器，数量: {handlers.Count()}");
                
                foreach (var handler in handlers)
                {
                    var messageType = handler.GetMessageType();
                    RegisterHandler(messageType, handler);
                    ASLogger.Instance.Debug($"MessageHandlerDispatcher: 注册外部处理器 {handler.GetType().Name} for {messageType.Name}");
                }
                
                ASLogger.Instance.Info($"MessageHandlerDispatcher: 外部消息处理器注册完成");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MessageHandlerDispatcher: 注册外部消息处理器时发生异常 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="handler">处理器实例</param>
        /// <param name="priority">优先级</param>
        /// <param name="enabled">是否启用</param>
        public void RegisterHandler(Type messageType, IMessageHandler handler, int priority = 0, bool enabled = true)
        {
            if (!_handlers.ContainsKey(messageType))
            {
                _handlers[messageType] = new List<MessageHandlerInfo>();
            }
            
            var handlerInfo = new MessageHandlerInfo(handler, priority, enabled);
            _handlers[messageType].Add(handlerInfo);
            
            // 按优先级排序
            _handlers[messageType] = _handlers[messageType]
                .OrderBy(h => h.Priority)
                .ToList();
        }
        
        /// <summary>
        /// 分发消息到对应的处理器
        /// </summary>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        public async Task DispatchAsync(MessageObject message)
        {
            if (!_isInitialized)
            {
                ASLogger.Instance.Error("MessageHandlerDispatcher not initialized");
                return;
            }
            
            var messageType = message.GetType();
            
            if (!_handlers.TryGetValue(messageType, out var handlers))
            {
                ASLogger.Instance.Warning($"No handlers found for message type: {messageType.Name}");
                return;
            }
            
            // 执行所有启用的处理器
            var enabledHandlers = handlers.Where(h => h.Enabled).ToList();
            
            if (enabledHandlers.Count == 0)
            {
                ASLogger.Instance.Warning($"No enabled handlers found for message type: {messageType.Name}");
                return;
            }
            
            // 并行执行所有处理器
            var tasks = enabledHandlers.Select(handlerInfo => 
                ExecuteHandler(handlerInfo, message));
            
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Error executing handlers for message {messageType.Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 执行单个处理器
        /// </summary>
        /// <param name="handlerInfo">处理器信息</param>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        private async Task ExecuteHandler(MessageHandlerInfo handlerInfo, MessageObject message)
        {
            try
            {
                await handlerInfo.Handler.HandleAsync(message);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Handler {handlerInfo.Handler.GetType().Name} failed to process message {message.GetType().Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取指定消息类型的处理器数量
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <returns>处理器数量</returns>
        public int GetHandlerCount(Type messageType)
        {
            return _handlers.TryGetValue(messageType, out var handlers) ? handlers.Count : 0;
        }
        
        /// <summary>
        /// 获取所有已注册的消息类型
        /// </summary>
        /// <returns>消息类型列表</returns>
        public IEnumerable<Type> GetRegisteredMessageTypes()
        {
            return _handlers.Keys;
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Shutdown()
        {
            _handlers.Clear();
            _isInitialized = false;
            ASLogger.Instance.Info("MessageHandlerDispatcher shutdown completed");
        }
    }
}
