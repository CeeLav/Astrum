using System;
using MongoDB.Bson;

namespace Astrum.CommonBase
{
    [EnableClass]
    public abstract class Object
    {
        public override string ToString()
        {
            // 这里不能用MongoHelper.ToJson，因为单步调试会调用ToString来显示数据
            // 如果MongoHelper.ToJson会调用BeginInit,就出大事了
            // return MongoHelper.ToJson(this);
            try
            {
                return $"{GetType().Name}[HashCode={GetHashCode()}]";
                
                //return ((object)this).ToJson();
            }
            catch (Exception ex)
            {
                // 防止 ToString() 抛出异常导致 CLR 内部错误（特别是在调试器中）
                // 返回类型名称和哈希码作为安全的字符串表示
                return $"{GetType().Name}[HashCode={GetHashCode()}, ToJsonError={ex.GetType().Name}]";
            }
        }
        
        public string ToJson()
        {
            return MongoHelper.ToJson(this);
        }
        
        public byte[] ToBson()
        {
            return MongoHelper.Serialize(this);
        }
    }
}