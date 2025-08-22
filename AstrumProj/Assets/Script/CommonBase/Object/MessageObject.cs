using System;
using System.ComponentModel;
using MongoDB.Bson.Serialization.Attributes;

namespace Astrum.CommonBase
{
    // 不需要返回消息
    public interface IMessage
    {
    }
    
    [DisableNew]
    public abstract class MessageObject: ProtoObject, IMessage, IDisposable, IPool
    {
        public virtual void Dispose()
        {
        }

        [BsonIgnore]
        public bool IsFromPool { get; set; }
    }
}