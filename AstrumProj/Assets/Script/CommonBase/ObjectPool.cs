using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 全局对象池管理器，提供线程安全的对象复用功能。
    /// 通过复用对象减少 GC 压力，提升性能。每个类型维护独立的对象池，默认最大容量为 1000。
    /// </summary>
    /// <remarks>
    /// <para>使用方式：</para>
    /// <list type="bullet">
    /// <item><description>获取对象：<c>var obj = ObjectPool.Instance.Fetch&lt;MyClass&gt;();</c></description></item>
    /// <item><description>回收对象：<c>ObjectPool.Instance.Recycle(obj);</c></description></item>
    /// </list>
    /// <para>注意事项：</para>
    /// <list type="bullet">
    /// <item><description>实现了 <see cref="IPool"/> 接口的对象会被自动标记，防止重复入池</description></item>
    /// <item><description>池为空时会自动创建新实例，无需手动初始化</description></item>
    /// <item><description>每个类型的对象池容量达到上限后，多余的回收对象会被丢弃</description></item>
    /// </list>
    /// </remarks>
    public class ObjectPool: Singleton<ObjectPool>
    {
        private ConcurrentDictionary<Type, Pool> objPool;

        /// <summary>
        /// 根据类型获取对象池容量
        /// </summary>
        private int GetPoolCapacity(Type type)
        {
            // 检查是否是 World 类型（通过命名空间和类型名判断，避免循环依赖）
            if (type.Namespace == "Astrum.LogicCore.Core" && type.Name == "World")
                return 10; // World 池容量（World 数量较少）
            
            // 检查是否是 Entity 类型
            if (type.Namespace == "Astrum.LogicCore.Core" && type.Name == "Entity")
                return 500; // Entity 池容量
            
            // 检查是否是 Component 类型（通过命名空间判断）
            if (type.Namespace == "Astrum.LogicCore.Components")
                return 1000; // Component 池容量
            
            return 1000; // 默认容量
        }

        private Func<Type, Pool> AddPoolFunc => type => new Pool(type, GetPoolCapacity(type));

        public void Awake()
        {
            lock (this)
            {
                objPool = new ConcurrentDictionary<Type, Pool>();
            }
        }

        public T Fetch<T>() where T : class
        {
            return this.Fetch(typeof (T)) as T;
        }

        public object Fetch(Type type, bool isFromPool = true)
        {
            if (!isFromPool)
            {
                return Activator.CreateInstance(type);
            }
            
            Pool pool = GetPool(type);
            object obj = pool.Get();
            if (obj is IPool p)
            {
                p.IsFromPool = true;
            }
            return obj;
        }

        /// <summary>
        /// 泛型回收方法（性能优化：避免 GetType 和装箱）
        /// </summary>
        public void Recycle<T>(T obj) where T : class
        {
            if (obj is IPool p)
            {
                if (!p.IsFromPool)
                {
                    return;
                }

                // 防止多次入池
                p.IsFromPool = false;
            }

            Pool pool = GetPool(typeof(T));
            pool.Return(obj);
        }
        
        public void Recycle(object obj)
        {
            if (obj is IPool p)
            {
                if (!p.IsFromPool)
                {
                    return;
                }

                // 防止多次入池
                p.IsFromPool = false;
            }

            Type type = obj.GetType();
            Pool pool = GetPool(type);
            pool.Return(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Pool GetPool(Type type)
        {
            return this.objPool.GetOrAdd(type, AddPoolFunc);
        }

        /// <summary>
        /// 线程安全的无锁对象池
        /// </summary>
        private class Pool
        {
            private readonly Type ObjectType;
            private readonly int MaxCapacity;
            private int NumItems;
            private readonly ConcurrentQueue<object> _items = new();
            private object FastItem;

            public Pool(Type objectType, int maxCapacity)
            {
                ObjectType = objectType;
                MaxCapacity = maxCapacity;
            }

            public object Get()
            {
                object item = FastItem;
                if (item == null || Interlocked.CompareExchange(ref FastItem, null, item) != item)
                {
                    if (_items.TryDequeue(out item))
                    {
                        Interlocked.Decrement(ref NumItems);
                        return item;
                    }

                    return Activator.CreateInstance(this.ObjectType);
                }

                return item;
            }

            public void Return(object obj)
            {
                if (FastItem != null || Interlocked.CompareExchange(ref FastItem, obj, null) != null)
                {
                    if (Interlocked.Increment(ref NumItems) <= MaxCapacity)
                    {
                        _items.Enqueue(obj);
                        return;
                    }

                    Interlocked.Decrement(ref NumItems);
                }
            }
        }
    }
} 