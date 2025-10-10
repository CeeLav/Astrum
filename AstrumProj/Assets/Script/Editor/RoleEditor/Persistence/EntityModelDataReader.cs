using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// EntityModelTable 数据读取器
    /// </summary>
    public static class EntityModelDataReader
    {
        /// <summary>
        /// 读取 EntityModelTable 数据
        /// </summary>
        public static List<EntityModelTableData> ReadAll()
        {
            var config = EntityModelTableData.GetTableConfig();
            return LubanCSVReader.ReadTable<EntityModelTableData>(config);
        }
        
        /// <summary>
        /// 读取单个 EntityModel 数据
        /// </summary>
        public static EntityModelTableData ReadById(int modelId)
        {
            var allData = ReadAll();
            return allData.Find(d => d.ModelId == modelId);
        }
        
        /// <summary>
        /// 读取为字典
        /// </summary>
        public static Dictionary<int, EntityModelTableData> ReadAsDictionary()
        {
            var allData = ReadAll();
            var dict = new Dictionary<int, EntityModelTableData>();
            
            foreach (var data in allData)
            {
                if (!dict.ContainsKey(data.ModelId))
                    dict.Add(data.ModelId, data);
            }
            
            return dict;
        }
    }
}

