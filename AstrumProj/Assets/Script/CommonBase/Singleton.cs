using System;
using System.Threading;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 单例模式基类 - 线程安全
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static T? _instance;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                            _instance.Awake();
                            _isInitialized = true;
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化方法，在实例创建后调用
        /// </summary>
        protected virtual void Awake()
        {
            // 子类可以重写此方法进行初始化
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// 销毁单例实例
        /// </summary>
        public static void Destroy()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    if (_instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _instance = null;
                    _isInitialized = false;
                }
            }
        }

        /// <summary>
        /// 重置单例实例
        /// </summary>
        public static void Reset()
        {
            Destroy();
            _ = Instance; // 重新创建实例
        }
    }

    /// <summary>
    /// 延迟初始化单例 - 更高效的线程安全实现
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class LazySingleton<T> where T : LazySingleton<T>, new()
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = CreateInstanceStatic();
                            _instance.Awake();
                            _isInitialized = true;
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化方法，在实例创建后调用
        /// </summary>
        protected virtual void Awake()
        {
            // 子类可以重写此方法进行初始化
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// 创建实例的静态方法
        /// </summary>
        private static T CreateInstanceStatic()
        {
            // 使用 new() 约束创建实例
            return new T();
        }

        /// <summary>
        /// 创建实例的抽象方法（已废弃，使用 CreateInstanceStatic）
        /// </summary>
        [Obsolete("使用 CreateInstanceStatic 方法")]
        protected abstract T CreateInstance();

        /// <summary>
        /// 销毁单例实例
        /// </summary>
        public static void Destroy()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    if (_instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _instance = null;
                    _isInitialized = false;
                }
            }
        }
    }

    /// <summary>
    /// 带初始化接口的单例
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class InitializableSingleton<T> : Singleton<T> where T : InitializableSingleton<T>, IInitializable, new()
    {
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public new static bool IsInitialized => ((IInitializable)Instance).IsInitialized;

        /// <summary>
        /// 初始化单例
        /// </summary>
        public static void Initialize()
        {
            ((IInitializable)Instance).Initialize();
        }

        /// <summary>
        /// 销毁单例
        /// </summary>
        public static new void Destroy()
        {
            if (((IInitializable)Instance).IsInitialized)
            {
                ((IInitializable)Instance).Destroy();
            }
            Singleton<T>.Destroy();
        }
    }

    /// <summary>
    /// 初始化接口
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 初始化
        /// </summary>
        void Initialize();

        /// <summary>
        /// 销毁
        /// </summary>
        void Destroy();
    }


} 