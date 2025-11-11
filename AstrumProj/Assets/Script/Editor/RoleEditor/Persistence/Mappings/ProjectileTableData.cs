using System.Collections.Generic;
using Astrum.Editor.RoleEditor.Persistence.Core;

namespace Astrum.Editor.RoleEditor.Persistence.Mappings
{
    /// <summary>
    /// ProjectileTable数据映射
    /// </summary>
    public class ProjectileTableData
    {
        [TableField(0, "projectileId")]
        public int ProjectileId { get; set; }
        
        [TableField(1, "projectileName")]
        public string ProjectileName { get; set; }
        
        [TableField(2, "projectileArchetype")]
        public string ProjectileArchetype { get; set; }
        
        [TableField(3, "lifeTime")]
        public int LifeTime { get; set; }
        
        [TableField(4, "trajectoryType")]
        public string TrajectoryType { get; set; }
        
        [TableField(5, "trajectoryData")]
        public string TrajectoryData { get; set; }
        
        [TableField(6, "pierceCount")]
        public int PierceCount { get; set; }
        
        [TableField(7, "defaultEffectIds")]
        public List<int> DefaultEffectIds { get; set; } = new List<int>();
        
        [TableField(8, "spawnEffectPath")]
        public string SpawnEffectPath { get; set; }
        
        [TableField(9, "loopEffectPath")]
        public string LoopEffectPath { get; set; }
        
        [TableField(10, "hitEffectPath")]
        public string HitEffectPath { get; set; }

        /// <summary>
        /// 获取表配置
        /// </summary>
        public static LubanTableConfig GetTableConfig()
        {
            return new LubanTableConfig
            {
                FilePath = "AstrumConfig/Tables/Datas/Projectile/#ProjectileTable.csv",
                HeaderLines = 4,
                HasEmptyFirstColumn = true,
                Header = new TableHeader
                {
                    VarNames = new List<string>
                    {
                        "projectileId", "projectileName", "projectileArchetype", "lifeTime", 
                        "trajectoryType", "trajectoryData", "pierceCount", "defaultEffectIds",
                        "spawnEffectPath", "loopEffectPath", "hitEffectPath"
                    },
                    Types = new List<string>
                    {
                        "int", "string", "string", "int", 
                        "string", "string", "int", "(array#sep=|),int",
                        "string", "string", "string"
                    },
                    Groups = new List<string>
                    {
                        "", "", "", "", "", "", "", "", "", "", ""
                    },
                    Descriptions = new List<string>
                    {
                        "弹道ID", "弹道名称", "实体原型", "生命周期(帧)", 
                        "轨迹类型", "轨迹配置JSON", "允许穿透数量", "默认效果ID",
                        "开火特效资源路径", "飞行特效资源路径", "命中特效资源路径"
                    }
                }
            };
        }

        public ProjectileTableData Clone()
        {
            return new ProjectileTableData
            {
                ProjectileId = ProjectileId,
                ProjectileName = ProjectileName,
                ProjectileArchetype = ProjectileArchetype,
                LifeTime = LifeTime,
                TrajectoryType = TrajectoryType,
                TrajectoryData = TrajectoryData,
                PierceCount = PierceCount,
                DefaultEffectIds = new List<int>(DefaultEffectIds),
                SpawnEffectPath = SpawnEffectPath,
                LoopEffectPath = LoopEffectPath,
                HitEffectPath = HitEffectPath
            };
        }
    }
}
