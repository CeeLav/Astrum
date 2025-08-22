using System;
using System.Collections.Generic;
using System.Reflection;

namespace Astrum.CommonBase
{
    public class CodeTypes: Singleton<CodeTypes>
    {
        private readonly Dictionary<string, Type> allTypes = new();
        private readonly UnOrderMultiMapSet<Type, Type> types = new();
        
        public void Awake()
        {
            LoadTypes();
        }
        
        private void LoadTypes()
        {
            // 加载当前程序集中的所有类型
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var assemblyTypes = assembly.GetTypes();
                    foreach (var type in assemblyTypes)
                    {
                        if (!allTypes.ContainsKey(type.FullName))
                        {
                            allTypes[type.FullName] = type;
                        }
                        
                        if (type.IsAbstract)
                        {
                            continue;
                        }
                        
                        // 记录所有的有BaseAttribute标记的的类型
                        object[] objects = type.GetCustomAttributes(typeof(BaseAttribute), true);

                        foreach (object o in objects)
                        {
                            this.types.Add(o.GetType(), type);
                        }
                    }
                }
                catch (Exception)
                {
                    // 忽略无法加载的程序集
                }
            }
        }

        public HashSet<Type> GetTypes(Type systemAttributeType)
        {
            if (!this.types.ContainsKey(systemAttributeType))
            {
                return new HashSet<Type>();
            }

            return this.types[systemAttributeType];
        }

        public Dictionary<string, Type> GetTypes()
        {
            return allTypes;
        }

        public Type GetType(string typeName)
        {
            if (allTypes.TryGetValue(typeName, out Type type))
            {
                return type;
            }
            return null;
        }
    }
}
