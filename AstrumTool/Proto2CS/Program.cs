using System;

namespace AstrumTool.Proto2CS
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("开始生成Proto2CS...");
                Proto2CS.Export();
                Console.WriteLine("Proto2CS生成完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Proto2CS生成失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
        }
    }
}
