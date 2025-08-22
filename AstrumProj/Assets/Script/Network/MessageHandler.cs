using System;
using System.Threading.Tasks;
using Astrum.CommonBase;

namespace Astrum.Network
{
    public interface IMHandler
    {
        Task Handle(MessageObject actorMessage);
        Type GetRequestType();
        Type GetResponseType();
    }
    
    public abstract class MessageHandler<Message>: HandlerObject, IMHandler  where Message : class, IMessage
    {
        protected abstract Task Run(Message message);

        public async Task Handle(MessageObject actorMessage)
        {
            if (actorMessage is not Message msg)
            {
                ASLogger.Instance.Error($"消息类型转换错误: {actorMessage.GetType().FullName} to {typeof (Message).Name}");
                return;
            }

            await this.Run(msg);
        }

        public Type GetRequestType()
        {
            if (typeof (ILocationMessage).IsAssignableFrom(typeof (Message)))
            {
                ASLogger.Instance.Error($"message is IActorLocationMessage but handler is AMActorHandler: {typeof (Message)}");
            }

            return typeof (Message);
        }

        public Type GetResponseType()
        {
            return null;
        }
    }
    
    public abstract class MessageHandler<Request, Response>: HandlerObject, IMHandler  where Request : MessageObject, IRequest where Response : MessageObject, IResponse
    {
        protected abstract Task Run(Request request, Response response);

        public async Task Handle(MessageObject actorMessage)
        {
            try
            {
                if (actorMessage is not Request request)
                {
                    ASLogger.Instance.Error($"消息类型转换错误: {actorMessage.GetType().FullName} to {typeof (Request).Name}");
                    return;
                }

                int rpcId = request.RpcId;
                Response response = ObjectPool.Instance.Fetch<Response>();
                try
                {
                    await this.Run(request, response);
                }
                catch (RpcException exception)
                {
                    response.Error = exception.Error;
                    response.Message = exception.ToString();
                }
                catch (Exception exception)
                {
                    response.Error = ErrorCore.ERR_RpcFail;
                    response.Message = exception.ToString();
                }
                
                response.RpcId = rpcId;
                //fiber.Root.GetComponent<ProcessInnerSender>().Reply(fromAddress, response);
            }
            catch (Exception e)
            {
                throw new Exception($"解释消息失败: {actorMessage.GetType().FullName}", e);
            }
        }

        public Type GetRequestType()
        {
            if (typeof (ILocationRequest).IsAssignableFrom(typeof (Request)))
            {
                ASLogger.Instance.Error($"message is IActorLocationMessage but handler is AMActorRpcHandler: {typeof (Request)}");
            }

            return typeof (Request);
        }

        public Type GetResponseType()
        {
            return typeof (Response);
        }
    }
}