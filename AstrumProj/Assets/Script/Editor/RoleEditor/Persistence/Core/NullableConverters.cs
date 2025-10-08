using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Astrum.Editor.RoleEditor.Persistence.Core
{
    /// <summary>
    /// 可空Int转换器 - 空字符串转为0
    /// </summary>
    public class NullableInt32Converter : Int32Converter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }
            
            return base.ConvertFromString(text, row, memberMapData);
        }
    }
    
    /// <summary>
    /// 可空Float转换器 - 空字符串转为0f
    /// </summary>
    public class NullableFloatConverter : SingleConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0f;
            }
            
            return base.ConvertFromString(text, row, memberMapData);
        }
    }
    
    /// <summary>
    /// 可空Double转换器 - 空字符串转为0.0
    /// </summary>
    public class NullableDoubleConverter : DoubleConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0.0;
            }
            
            return base.ConvertFromString(text, row, memberMapData);
        }
    }
    
    /// <summary>
    /// 可空Boolean转换器 - 空字符串转为false
    /// </summary>
    public class NullableBooleanConverter : BooleanConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }
            
            return base.ConvertFromString(text, row, memberMapData);
        }
    }
}

