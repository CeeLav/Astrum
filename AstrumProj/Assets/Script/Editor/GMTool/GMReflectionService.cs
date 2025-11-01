using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Astrum.CommonBase;
using Astrum.Client.Managers.GameModes;
using Astrum.Client.Core;

namespace Astrum.Editor.GMTool
{
    /// <summary>
    /// GM反射服务 - 扫描单例和GameMode，提供反射调用功能
    /// </summary>
    public class GMReflectionService
    {
        private static readonly Dictionary<string, Type> _singletonTypes = new Dictionary<string, Type>();
        private static readonly Dictionary<string, object> _singletonInstances = new Dictionary<string, object>();
        private static bool _initialized = false;

        /// <summary>
        /// 初始化服务，扫描所有单例和GameMode类型
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            _singletonTypes.Clear();
            _singletonInstances.Clear();

            // 扫描所有程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    // 扫描继承 Singleton<T> 的类型
                    ScanSingletonTypes(assembly);

                    // 扫描实现 IGameMode 的类型（已在上面扫描，这里只是记录）
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[GMReflectionService] 扫描程序集 {assembly.FullName} 时出错: {ex.Message}");
                }
            }

            _initialized = true;
            UnityEngine.Debug.Log($"[GMReflectionService] 初始化完成，找到 {_singletonTypes.Count} 个单例类型");
        }

        /// <summary>
        /// 扫描单例类型
        /// </summary>
        private static void ScanSingletonTypes(Assembly assembly)
        {
            var singletonBaseType = typeof(Singleton<>);
            var gameModeInterfaceType = typeof(IGameMode);

            foreach (var type in assembly.GetTypes())
            {
                try
                {
                    // 跳过抽象类和接口
                    if (type.IsAbstract || type.IsInterface) continue;

                    // 检查是否继承 Singleton<T>
                    if (IsSubclassOfGenericType(type, singletonBaseType))
                    {
                        string typeName = GetTypeDisplayName(type);
                        _singletonTypes[typeName] = type;
                    }

                    // 检查是否实现 IGameMode（GameMode 通常也是单例，但单独列出）
                    if (gameModeInterfaceType.IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        string typeName = $"GameMode: {GetTypeDisplayName(type)}";
                        _singletonTypes[typeName] = type;
                    }
                }
                catch (Exception ex)
                {
                    // 忽略无法访问的类型
                    continue;
                }
            }
        }

        /// <summary>
        /// 检查类型是否是泛型基类的子类
        /// </summary>
        private static bool IsSubclassOfGenericType(Type type, Type genericBaseType)
        {
            Type currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                Type baseType = currentType.BaseType;
                if (baseType != null && baseType.IsGenericType)
                {
                    Type genericTypeDefinition = baseType.GetGenericTypeDefinition();
                    if (genericTypeDefinition == genericBaseType)
                    {
                        return true;
                    }
                }
                currentType = baseType;
            }
            return false;
        }

        /// <summary>
        /// 获取类型的显示名称
        /// </summary>
        private static string GetTypeDisplayName(Type type)
        {
            string name = type.Name;
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                // 简化命名空间显示
                string[] nsParts = type.Namespace.Split('.');
                if (nsParts.Length > 0)
                {
                    name = $"{nsParts[nsParts.Length - 1]}.{name}";
                }
            }
            return name;
        }

        /// <summary>
        /// 类型分类枚举
        /// </summary>
        public enum TypeCategory
        {
            Manager,
            GameMode,
            Other
        }
        
        /// <summary>
        /// 获取类型的分类
        /// </summary>
        public static TypeCategory GetTypeCategory(Type type, string typeName)
        {
            // GameMode类型
            if (typeName.StartsWith("GameMode:") || typeof(IGameMode).IsAssignableFrom(type))
            {
                return TypeCategory.GameMode;
            }
            
            // Manager类型（通常以Manager结尾，或在Managers命名空间下）
            string name = type.Name;
            if (name.EndsWith("Manager") || 
                (type.Namespace != null && type.Namespace.Contains("Managers")))
            {
                return TypeCategory.Manager;
            }
            
            return TypeCategory.Other;
        }
        
        /// <summary>
        /// 获取分类的类型列表
        /// </summary>
        public static Dictionary<TypeCategory, List<string>> GetCategorizedTypes()
        {
            Initialize();
            
            var categorized = new Dictionary<TypeCategory, List<string>>
            {
                { TypeCategory.Manager, new List<string>() },
                { TypeCategory.GameMode, new List<string>() },
                { TypeCategory.Other, new List<string>() }
            };
            
            foreach (var kvp in _singletonTypes)
            {
                var category = GetTypeCategory(kvp.Value, kvp.Key);
                categorized[category].Add(kvp.Key);
            }
            
            // 排序
            foreach (var category in categorized.Keys)
            {
                categorized[category].Sort();
            }
            
            return categorized;
        }

        /// <summary>
        /// 获取所有可用的类型名称（保持向后兼容）
        /// </summary>
        public static List<string> GetAvailableTypeNames()
        {
            Initialize();
            return _singletonTypes.Keys.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// 获取类型的实例（单例通过Instance属性，GameMode通过GameDirector）
        /// </summary>
        public static object GetInstance(string typeName)
        {
            Initialize();

            if (!_singletonTypes.TryGetValue(typeName, out Type type))
            {
                return null;
            }

            // 如果是 GameMode，从 GameDirector 获取
            if (typeName.StartsWith("GameMode:"))
            {
                try
                {
                    // 直接访问 GameDirector.Instance（最可靠的方式，因为这是编译时已知的类型）
                    GameDirector gameDirector = null;
                    try
                    {
                        gameDirector = GameDirector.Instance;
                        UnityEngine.Debug.Log($"[GMReflectionService] 直接访问 GameDirector.Instance 成功");
                    }
                    catch (Exception directEx)
                    {
                        UnityEngine.Debug.LogError($"[GMReflectionService] 直接访问 GameDirector.Instance 失败: {directEx.Message}");
                        
                        // Fallback: 尝试通过反射获取（如果直接访问失败）
                        var gameDirectorType = typeof(GameDirector);
                        var instanceProperty = gameDirectorType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                        if (instanceProperty == null && gameDirectorType.BaseType != null)
                        {
                            instanceProperty = gameDirectorType.BaseType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                        }
                        if (instanceProperty != null)
                        {
                            gameDirector = (GameDirector)instanceProperty.GetValue(null);
                            UnityEngine.Debug.Log($"[GMReflectionService] 通过反射获取 GameDirector 实例成功");
                        }
                    }
                    
                    if (gameDirector != null)
                    {
                        var gameDirectorType = typeof(GameDirector);
                        var currentGameModeProperty = gameDirectorType.GetProperty("CurrentGameMode", BindingFlags.Public | BindingFlags.Instance);
                        if (currentGameModeProperty != null)
                        {
                            var currentGameMode = gameDirector.CurrentGameMode;
                            if (currentGameMode != null)
                            {
                                var currentType = currentGameMode.GetType();
                                // 检查类型是否匹配
                                // 使用IsAssignableFrom确保能够匹配基类和接口关系
                                if (type.IsAssignableFrom(currentType))
                                {
                                    UnityEngine.Debug.Log($"[GMReflectionService] 找到匹配的 GameMode: 当前类型={currentType.Name}, 目标类型={type.Name}");
                                    return currentGameMode;
                                }
                                else
                                {
                                    UnityEngine.Debug.LogWarning($"[GMReflectionService] GameMode 类型不匹配: 当前={currentType.Name}, 目标={type.Name} (类型检查: IsAssignableFrom={type.IsAssignableFrom(currentType)})");
                                }
                            }
                            else
                            {
                                UnityEngine.Debug.LogWarning($"[GMReflectionService] 当前没有激活的 GameMode");
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"[GMReflectionService] 无法找到 CurrentGameMode 属性");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[GMReflectionService] GameDirector 实例为 null，可能尚未初始化");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[GMReflectionService] 获取 GameMode 失败: {ex.Message}\n{ex.StackTrace}");
                }
                return null;
            }

            // 普通单例通过 Instance 属性获取
            try
            {
                var instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    return instanceProperty.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[GMReflectionService] 获取单例 {typeName} 失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 获取类型的所有公共方法（过滤系统方法）
        /// </summary>
        public static List<MethodInfo> GetMethods(string typeName)
        {
            Initialize();

            if (!_singletonTypes.TryGetValue(typeName, out Type type))
            {
                return new List<MethodInfo>();
            }

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => 
                    !m.IsSpecialName &&  // 排除属性访问器
                    !m.Name.StartsWith("get_") &&
                    !m.Name.StartsWith("set_") &&
                    m.DeclaringType != typeof(object) &&  // 排除 object 的方法
                    m.DeclaringType != typeof(System.Object) &&
                    !IsSystemMethod(m))
                .OrderBy(m => m.Name)
                .ToList();

            return methods;
        }

        /// <summary>
        /// 判断是否是系统方法
        /// </summary>
        private static bool IsSystemMethod(MethodInfo method)
        {
            // 排除一些常见的不需要的系统方法
            string[] excludedMethods = {
                "ToString", "GetType", "GetHashCode", "Equals",
                "Awake", "OnEnable", "OnDisable", "OnDestroy"
            };

            return excludedMethods.Contains(method.Name);
        }

        /// <summary>
        /// 获取方法的完整签名字符串
        /// </summary>
        public static string GetMethodSignature(MethodInfo method)
        {
            string parameters = string.Join(", ", method.GetParameters()
                .Select(p => $"{GetTypeShortName(p.ParameterType)} {p.Name}"));

            string returnType = method.ReturnType == typeof(void) ? "void" : GetTypeShortName(method.ReturnType);
            string staticKeyword = method.IsStatic ? "static " : "";

            return $"{staticKeyword}{returnType} {method.Name}({parameters})";
        }

        /// <summary>
        /// 获取类型的简短名称
        /// </summary>
        public static string GetTypeShortName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(void)) return "void";

            if (type.IsGenericType)
            {
                string name = type.Name.Split('`')[0];
                var args = type.GetGenericArguments().Select(GetTypeShortName);
                return $"{name}<{string.Join(", ", args)}>";
            }

            return type.Name;
        }

        /// <summary>
        /// 重置初始化状态（用于重新扫描）
        /// </summary>
        public static void Reset()
        {
            _initialized = false;
            _singletonTypes.Clear();
            _singletonInstances.Clear();
        }
    }
}

