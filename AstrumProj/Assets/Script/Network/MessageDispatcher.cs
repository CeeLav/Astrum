using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Astrum.CommonBase;

namespace Astrum.Network
{
    public class MessageDispatcherInfo
    {

        public IMHandler IMHandler { get; }

        public MessageDispatcherInfo(IMHandler imHandler)
        {
            this.IMHandler = imHandler;
        }
    }

    /// <summary>
    /// Actor消息分发组件
    /// </summary>
    public class MessageDispatcher: Singleton<MessageDispatcher>
    {
        private readonly Dictionary<Type, List<MessageDispatcherInfo>> messageHandlers = new();

        public void Awake()
        {
            /*HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (MessageHandlerAttribute));
            
            foreach (Type type in types)
            {
                this.Register(type);
            }*/
        }
        /*
        private void Register(Type type)
        {
            object obj = Activator.CreateInstance(type);

            IMHandler imHandler = obj as IMHandler;
            if (imHandler == null)
            {
                throw new Exception($"message handler not inherit IMActorHandler abstract class: {obj.GetType().FullName}");
            }
                
            object[] attrs = type.GetCustomAttributes(typeof(MessageHandlerAttribute), true);

            foreach (object attr in attrs)
            {
                MessageHandlerAttribute messageHandlerAttribute = attr as MessageHandlerAttribute;

                Type messageType = imHandler.GetRequestType();

                Type handleResponseType = imHandler.GetResponseType();
                if (handleResponseType != null)
                {
                    Type responseType = OpcodeType.Instance.GetResponseType(messageType);
                    if (handleResponseType != responseType)
                    {
                        throw new Exception($"message handler response type error: {messageType.FullName}");
                    }
                }

                MessageDispatcherInfo messageDispatcherInfo = new(imHandler);

                this.RegisterHandler(messageType, messageDispatcherInfo);
            }
        }
        */
        private void RegisterHandler(Type type, MessageDispatcherInfo handler)
        {
            if (!this.messageHandlers.ContainsKey(type))
            {
                this.messageHandlers.Add(type, new List<MessageDispatcherInfo>());
            }

            this.messageHandlers[type].Add(handler);
        }

        public async Task Handle(MessageObject message)
        {
            List<MessageDispatcherInfo> list;
            if (!this.messageHandlers.TryGetValue(message.GetType(), out list))
            {
                throw new Exception($"not found message handler: {message} ");
            }

            foreach (MessageDispatcherInfo actorMessageDispatcherInfo in list)
            {
                await actorMessageDispatcherInfo.IMHandler.Handle(message);   
            }
        }
    }
}