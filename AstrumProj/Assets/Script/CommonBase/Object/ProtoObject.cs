using System;
using System.ComponentModel;
using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;

namespace Astrum.CommonBase
{
    public abstract class ProtoObject: Object, ISupportInitialize
    {
        public object Clone()
        {
            byte[] bytes = MongoHelper.Serialize(this);
            return MongoHelper.Deserialize(this.GetType(), bytes, 0, bytes.Length);
        }
        
        public virtual void BeginInit()
        {
        }
        
        
        public virtual void EndInit()
        {
        }
    }
}