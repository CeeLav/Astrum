using UnityEngine;

namespace Astrum.Client.Core
{
    /// <summary>
    /// MonoBehaviour 单例基类 - 用于需要作为组件存在但又全局唯一的类
    /// </summary>
    /// <typeparam name="T">单例类型，必须继承自 MonoBehaviourSingleton&lt;T&gt;</typeparam>
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T>
    {
        private static T _instance;
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
                    // 尝试在场景中查找现有实例
                    _instance = FindObjectOfType<T>();
                    
                    if (_instance == null)
                    {
                        Debug.LogWarning($"{typeof(T).Name}: 未找到实例，请确保场景中存在 {typeof(T).Name} 组件");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized => _isInitialized && _instance != null;

        /// <summary>
        /// 是否在场景切换时保持存活（默认 false）
        /// </summary>
        protected virtual bool DontDestroyOnLoad => false;

        /// <summary>
        /// 单例初始化方法，在 Awake 中调用
        /// </summary>
        protected virtual void OnSingletonAwake()
        {
            // 子类可以重写此方法进行初始化
        }

        protected virtual void Awake()
        {
            // 单例模式处理
            if (_instance == null)
            {
                _instance = this as T;
                _isInitialized = true;

                // 如果设置了 DontDestroyOnLoad，则保持存活
                if (DontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }

                // 调用子类的初始化方法
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                // 如果已存在实例，销毁重复的 GameObject
                Debug.LogWarning($"{typeof(T).Name}: 检测到重复实例，销毁 {gameObject.name}");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            // 如果销毁的是当前实例，清理静态引用
            if (_instance == this)
            {
                _instance = null;
                _isInitialized = false;
            }
        }
    }
}

