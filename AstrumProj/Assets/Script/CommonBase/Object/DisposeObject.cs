using System;
using System.ComponentModel;
using MongoDB.Bson.Serialization.Attributes;

namespace Astrum.CommonBase
{
    public abstract class DisposeObject: Object, IDisposable, ISupportInitialize
    {
        public virtual void Dispose()
        {
        }
        
        public virtual void BeginInit()
        {
        }
        
        public virtual void EndInit()
        {
        }
    }

    public interface IPool
    {
        bool IsFromPool
        {
            get;
            set;
        }
    }
}