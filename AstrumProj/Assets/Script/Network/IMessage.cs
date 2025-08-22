using System;
using Astrum.CommonBase;
namespace Astrum.Network
{
    public interface IRequest: IMessage
    {
        int RpcId
        {
            get;
            set;
        }
    }

    public interface IResponse: IMessage
    {
        int Error
        {
            get;
            set;
        }

        string Message
        {
            get;
            set;
        }

        int RpcId
        {
            get;
            set;
        }
    }

}