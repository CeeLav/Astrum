using System;
using Astrum.CommonBase;

namespace Astrum.CommonBase
{
    public class TestSingleton : Singleton<TestSingleton>
    {
        public int Value { get; set; } = 42;
        protected override void Awake()
        {
            Console.WriteLine("TestSingleton Awake called");
        }
    }

    public static class SingletonTestEntry
    {
        public static void RunTest()
        {
            Console.WriteLine($"IsInitialized: {TestSingleton.IsInitialized}");
            var instance = TestSingleton.Instance;
            Console.WriteLine($"Value: {instance.Value}");
            instance.Value = 99;
            Console.WriteLine($"Changed Value: {TestSingleton.Instance.Value}");
            TestSingleton.Destroy();
            Console.WriteLine($"After Destroy IsInitialized: {TestSingleton.IsInitialized}");
            var newInstance = TestSingleton.Instance;
            Console.WriteLine($"New Value: {newInstance.Value}");
        }
    }
}
