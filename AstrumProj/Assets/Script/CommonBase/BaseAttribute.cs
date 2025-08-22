using System;

namespace Astrum.CommonBase
{
	[AttributeUsage(AttributeTargets.Class)]
	[EnableClass]
	public class BaseAttribute: Attribute
	{
	}
	
	public class EnableClassAttribute: BaseAttribute
	{
	}
	
	/// <summary>
	/// 唯一Id标签
	/// 使用此标签标记的类 会检测类内部的 const int 字段成员是否唯一
	/// 可以指定唯一Id的最小值 最大值区间
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class UniqueIdAttribute : Attribute
	{
		public int Min;

		public int Max;
        
		public UniqueIdAttribute(int min = int.MinValue, int max = int.MaxValue)
		{
			this.Min = min;
			this.Max = max;
		}
	}
	
	/// <summary>
	/// 添加该标记的类或结构体禁止使用new关键字构造对象
	/// </summary>
	[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct,Inherited = true)]
	public class DisableNewAttribute : Attribute
	{
        
	}
}