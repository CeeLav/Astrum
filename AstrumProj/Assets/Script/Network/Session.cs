using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.LogicCore;

namespace Astrum.Network
{
    /*
    public readonly struct RpcInfo
    {
        public Type RequestType { get; }
        
        private readonly Task<IResponse> tcs;

        public RpcInfo(Type requestType)
        {
            this.RequestType = requestType;
            
            this.tcs = new Task(Func<IResponse>);
        }

        public void SetResult(IResponse response)
        {
            this.tcs.SetResult(response);
        }

        public void SetException(Exception exception)
        {
            this.tcs.SetException(exception);
        }

        public async Task<IResponse> Wait()
        {
            return await this.tcs;
        }
    }*/

    public class Session
    {
        public AService AService { get; set; }
        
        public long Id
        {
            get;
            set;
        }
        
        public int RpcId
        {
            get;
            set;
        }

        //public readonly Dictionary<int, RpcInfo> requestCallbacks = new();
        
        public long LastRecvTime
        {
            get;
            set;
        }

        public long LastSendTime
        {
            get;
            set;
        }

        public int Error
        {
            get;
            set;
        }

        public IPEndPoint RemoteAddress
        {
            get;
            set;
        }
        
        public void Initialize(AService aService, long id, IPEndPoint remoteAddress)
        {
            AService = aService;
            Id = id;
            RemoteAddress = remoteAddress;
            long timeNow = TimeInfo.Instance.ClientNow();
            LastRecvTime = timeNow;
            LastSendTime = timeNow;

            //requestCallbacks.Clear();
            
            ASLogger.Instance.Info($"session create:  id: {Id} {timeNow} ");
        }
        
        private void Awake(AService aService)
        {
            AService = aService;
            long timeNow = TimeInfo.Instance.ClientNow();
            LastRecvTime = timeNow;
            LastSendTime = timeNow;

            //requestCallbacks.Clear();
            
            ASLogger.Instance.Info($"session create:  id: {Id} {timeNow} ");
        }
        
        private void Destroy()
        {
            AService.Remove(Id, Error);
            /*
            foreach (RpcInfo responseCallback in self.requestCallbacks.Values.ToArray())
            {
                responseCallback.SetException(new RpcException(self.Error, $"session dispose: {self.Id} {self.RemoteAddress}"));
            }

            Log.Info($"session dispose: {self.RemoteAddress} id: {self.Id} ErrorCode: {self.Error}, please see ErrorCode.cs! {TimeInfo.Instance.ClientNow()}");
            
            self.requestCallbacks.Clear();*/
        }
        /*
        public void OnResponse(IResponse response)
        {
            if (!requestCallbacks.Remove(response.RpcId, out RpcInfo action))
            {
                return;
            }
            action.SetResult(response);
        }*/
        /*
        public async Task<IResponse> Call( IRequest request, ETCancellationToken cancellationToken)
        {
            int rpcId = ++self.RpcId;
            RpcInfo rpcInfo = new(request.GetType());
            self.requestCallbacks[rpcId] = rpcInfo;
            request.RpcId = rpcId;

            self.Send(request);
            
            void CancelAction()
            {
                if (!self.requestCallbacks.Remove(rpcId, out RpcInfo action))
                {
                    return;
                }

                Type responseType = OpcodeType.Instance.GetResponseType(action.RequestType);
                IResponse response = (IResponse) Activator.CreateInstance(responseType);
                response.Error = ErrorCore.ERR_Cancel;
                action.SetResult(response);
            }

            IResponse ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await rpcInfo.Wait();
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }
            return ret;
        }*/
/*
        public async Task<IResponse> Call(IRequest request, int time = 0)
        {
            int rpcId = ++self.RpcId;
            RpcInfo rpcInfo = new(request.GetType());
            self.requestCallbacks[rpcId] = rpcInfo;
            request.RpcId = rpcId;
            self.Send(request);

            if (time > 0)
            {
                async Task Timeout()
                {
                    await self.Root().GetComponent<TimerComponent>().WaitAsync(time);
                    if (!self.requestCallbacks.TryGetValue(rpcId, out RpcInfo action))
                    {
                        return;
                    }

                    if (!self.requestCallbacks.Remove(rpcId))
                    {
                        return;
                    }
                    
                    action.SetException(new Exception($"session call timeout: {action.RequestType.FullName} {time}"));
                }
                
                Timeout().Coroutine();
            }

            return await rpcInfo.Wait();
        }*/
        
        public void Send(IMessage message)
        {
            LastSendTime = TimeInfo.Instance.ClientNow();
            ASLogger.Instance.Debug($"发送消息: {message.GetType().Name}", "Network.Send");
            MemoryBuffer memoryBuffer = AService.Fetch();
            MessageSerializeHelper.MessageToStream(memoryBuffer, message as MessageObject);
            AService.Send(Id, memoryBuffer);
        }
    }
}