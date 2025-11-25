using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// BaseUnitModelTable 数据读取器
    /// </summary>
    public static class EntityModelDataReader
    {
        /// <summary>
        /// 读取 BaseUnitModelTable 数据
        /// </summary>
        public static List<BaseUnitModelTableData> ReadAll()
        {
            var config = BaseUnitModelTableData.GetTableConfig();
            return LubanCSVReader.ReadTable<BaseUnitModelTableData>(config);
        }
        
        /// <summary>
        /// 读取单个 BaseUnitModel 数据
        /// </summary>
        public static BaseUnitModelTableData ReadById(int modelId)
        {
            var allData = ReadAll();
            return allData.Find(d => d.ModelId == modelId);
        }
        
        /// <summary>
        /// 读取为字典
        /// </summary>
        public static Dictionary<int, BaseUnitModelTableData> ReadAsDictionary()
        {
            var allData = ReadAll();
            var dict = new Dictionary<int, BaseUnitModelTableData>();
            
            foreach (var data in allData)
            {
                if (!dict.ContainsKey(data.ModelId))
                    dict.Add(data.ModelId, data);
            }
            
            return dict;
        }
    }
}

