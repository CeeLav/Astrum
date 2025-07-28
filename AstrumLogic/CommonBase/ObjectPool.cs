using System;
using System.Collections.Generic;
using System.Threading;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 对象池接口
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IObjectPool<T> where T : class
    {
        /// <summary>
        /// 获取对象
        /// </summary>
        /// <returns>对象实例</returns>
        T Get();

        /// <summary>
        /// 归还对象
        /// </summary>
        /// <param name="item">对象实例</param>
        void Return(T item);

        /// <summary>
        /// 清空对象池
        /// </summary>
        void Clear();

        /// <summary>
        /// 池中对象数量
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 最大容量
        /// </summary>
        int MaxSize { get; }
    }

    /// <summary>
    /// 对象池工厂接口
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IObjectPoolFactory<T> where T : class
    {
        /// <summary>
        /// 创建新对象
        /// </summary>
        /// <returns>新对象实例</returns>
        T Create();

        /// <summary>
        /// 重置对象状态
        /// </summary>
        /// <param name="item">对象实例</param>
        void Reset(T item);

        /// <summary>
        /// 销毁对象
        /// </summary>
        /// <param name="item">对象实例</param>
        void Destroy(T item);
    }

    /// <summary>
    /// 通用对象池
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private readonly Stack<T> _pool;
        private readonly IObjectPoolFactory<T> _factory;
        private readonly int _maxSize;
        private int _createdCount;
        private readonly object _lock = new object();

        /// <summary>
        /// 池中对象数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _pool.Count;
                }
            }
        }

        /// <summary>
        /// 最大容量
        /// </summary>
        public int MaxSize => _maxSize;

        /// <summary>
        /// 已创建的对象总数
        /// </summary>
        public int CreatedCount => _createdCount;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="factory">对象工厂</param>
        /// <param name="initialSize">初始大小</param>
        /// <param name="maxSize">最大大小</param>
        public ObjectPool(IObjectPoolFactory<T> factory, int initialSize = 0, int maxSize = 1000)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _maxSize = maxSize;
            _pool = new Stack<T>(initialSize);

            // 预创建对象
            for (int i = 0; i < initialSize; i++)
            {
                var item = _factory.Create();
                _pool.Push(item);
                _createdCount++;
            }
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <returns>对象实例</returns>
        public T Get()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    var item = _pool.Pop();
                    _factory.Reset(item);
                    return item;
                }

                if (_createdCount < _maxSize)
                {
                    var item = _factory.Create();
                    _createdCount++;
                    return item;
                }

                // 如果达到最大容量，返回null或抛出异常
                throw new InvalidOperationException($"对象池已达到最大容量: {_maxSize}");
            }
        }

        /// <summary>
        /// 归还对象
        /// </summary>
        /// <param name="item">对象实例</param>
        public void Return(T item)
        {
            if (item == null) return;

            lock (_lock)
            {
                if (_pool.Count < _maxSize)
                {
                    _pool.Push(item);
                }
                else
                {
                    // 如果池已满，销毁对象
                    _factory.Destroy(item);
                    _createdCount--;
                }
            }
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                while (_pool.Count > 0)
                {
                    var item = _pool.Pop();
                    _factory.Destroy(item);
                }
                _createdCount = 0;
            }
        }
    }

    /// <summary>
    /// 简单对象池工厂
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class SimpleObjectPoolFactory<T> : IObjectPoolFactory<T> where T : class, new()
    {
        /// <summary>
        /// 创建新对象
        /// </summary>
        /// <returns>新对象实例</returns>
        public virtual T Create()
        {
            return new T();
        }

        /// <summary>
        /// 重置对象状态
        /// </summary>
        /// <param name="item">对象实例</param>
        public virtual void Reset(T item)
        {
            // 默认实现不做任何操作
        }

        /// <summary>
        /// 销毁对象
        /// </summary>
        /// <param name="item">对象实例</param>
        public virtual void Destroy(T item)
        {
            // 默认实现不做任何操作
        }
    }

    /// <summary>
    /// 对象池管理器
    /// </summary>
    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        private readonly Dictionary<Type, object> _pools = new Dictionary<Type, object>();
        private readonly object _lock = new object();

        /// <summary>
        /// 创建或获取对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="factory">对象工厂</param>
        /// <param name="initialSize">初始大小</param>
        /// <param name="maxSize">最大大小</param>
        /// <returns>对象池</returns>
        public IObjectPool<T> GetPool<T>(IObjectPoolFactory<T> factory, int initialSize = 0, int maxSize = 1000) where T : class
        {
            var type = typeof(T);
            lock (_lock)
            {
                if (!_pools.ContainsKey(type))
                {
                    var pool = new ObjectPool<T>(factory, initialSize, maxSize);
                    _pools[type] = pool;
                }
                return (IObjectPool<T>)_pools[type];
            }
        }

        /// <summary>
        /// 获取简单对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="initialSize">初始大小</param>
        /// <param name="maxSize">最大大小</param>
        /// <returns>对象池</returns>
        public IObjectPool<T> GetSimplePool<T>(int initialSize = 0, int maxSize = 1000) where T : class, new()
        {
            var factory = new SimpleObjectPoolFactory<T>();
            return GetPool(factory, initialSize, maxSize);
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAllPools()
        {
            lock (_lock)
            {
                foreach (var pool in _pools.Values)
                {
                    var clearMethod = pool.GetType().GetMethod("Clear");
                    clearMethod?.Invoke(pool, null);
                }
                _pools.Clear();
            }
        }

        /// <summary>
        /// 获取对象池统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public Dictionary<string, object> GetPoolStatistics()
        {
            var stats = new Dictionary<string, object>();
            lock (_lock)
            {
                foreach (var kvp in _pools)
                {
                    var pool = kvp.Value;
                    var type = kvp.Key;
                    var countProperty = pool.GetType().GetProperty("Count");
                    var maxSizeProperty = pool.GetType().GetProperty("MaxSize");
                    var createdCountProperty = pool.GetType().GetProperty("CreatedCount");

                    var stat = new
                    {
                        Count = countProperty?.GetValue(pool),
                        MaxSize = maxSizeProperty?.GetValue(pool),
                        CreatedCount = createdCountProperty?.GetValue(pool)
                    };

                    stats[type.Name] = stat;
                }
            }
            return stats;
        }
    }

    /// <summary>
    /// 可重置接口
    /// </summary>
    public interface IResettable
    {
        /// <summary>
        /// 重置对象状态
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// 可销毁接口
    /// </summary>
    public interface IDestroyable
    {
        /// <summary>
        /// 销毁对象
        /// </summary>
        void Destroy();
    }
} 