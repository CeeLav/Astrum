using System;

namespace Astrum.CommonBase
{
    public static class RandomGenerator
    {
        private static readonly Random random = new Random();
        
        public static uint RandUInt32()
        {
            return (uint)random.Next();
        }
        
        public static int RandInt32()
        {
            return random.Next();
        }
        
        public static long RandInt64()
        {
            // 兼容性处理：使用两个 int 组合成 long
            long high = (long)random.Next() << 32;
            long low = (long)(uint)random.Next();
            return high | low;
        }
        
        public static float RandFloat()
        {
            return (float)random.NextDouble();
        }
        
        public static double RandDouble()
        {
            return random.NextDouble();
        }
    }
}
