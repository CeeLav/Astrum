namespace Astrum.LogicCore.Capabilities
{
    /// <summary>
    /// 字符串稳定哈希工具类
    /// 使用 FNV-1a 算法生成稳定的哈希值
    /// </summary>
    public static class StringHashUtility
    {
        /// <summary>
        /// 获取字符串的稳定哈希值
        /// </summary>
        public static int GetStableHash(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            
            unchecked
            {
                const int fnvPrime = 16777619;
                int hash = (int)2166136261;
                for (int i = 0; i < str.Length; i++)
                    hash = (hash ^ str[i]) * fnvPrime;
                return hash;
            }
        }
    }

    /// <summary>
    /// 类型哈希工具类
    /// 为每个类型生成唯一的稳定哈希 ID
    /// </summary>
    public static class TypeHash<T>
    {
        /// <summary>
        /// 类型的稳定哈希 ID（编译期常量）
        /// </summary>
        public static readonly int Hash = typeof(T).FullName?.GetStableHash() ?? 0;
        
        /// <summary>
        /// 获取类型的哈希 ID
        /// </summary>
        public static int GetHash() => Hash;
    }
}

