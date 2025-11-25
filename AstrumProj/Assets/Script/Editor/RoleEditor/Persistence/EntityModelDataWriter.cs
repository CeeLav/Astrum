using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// BaseUnitModelTable 数据写入器
    /// </summary>
    public static class EntityModelDataWriter
    {
        /// <summary>
        /// 写入 BaseUnitModelTable 数据
        /// </summary>
        public static bool WriteAll(List<BaseUnitModelTableData> dataList, bool enableBackup = true)
        {
            var config = BaseUnitModelTableData.GetTableConfig();
            bool success = LubanCSVWriter.WriteTable(config, dataList, enableBackup);
            
            if (success)
            {
                // 自动打表
                Core.LubanTableGenerator.GenerateClientTables(showDialog: false);
            }
            
            return success;
        }
        
        /// <summary>
        /// 更新单个 BaseUnitModel 数据
        /// </summary>
        public static bool UpdateById(int modelId, BaseUnitModelTableData newData, bool enableBackup = true)
        {
            var allData = EntityModelDataReader.ReadAll();
            var index = allData.FindIndex(d => d.ModelId == modelId);
            
            if (index >= 0)
            {
                allData[index] = newData;
            }
            else
            {
                allData.Add(newData);
            }
            
            return WriteAll(allData, enableBackup);
        }
        
        /// <summary>
        /// 删除 BaseUnitModel 数据
        /// </summary>
        public static bool DeleteById(int modelId, bool enableBackup = true)
        {
            var allData = EntityModelDataReader.ReadAll();
            allData.RemoveAll(d => d.ModelId == modelId);
            
            return WriteAll(allData, enableBackup);
        }
    }
}

