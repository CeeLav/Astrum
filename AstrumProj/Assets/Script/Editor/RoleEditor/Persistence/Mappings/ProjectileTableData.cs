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

        [TableField(11, "baseSpeed")]
        public int BaseSpeed { get; set; }

        [TableField(12, "spawnEffectPositionOffset")]
        public List<int> SpawnEffectPositionOffset { get; set; } = new List<int>();

        [TableField(13, "spawnEffectRotationOffset")]
        public List<int> SpawnEffectRotationOffset { get; set; } = new List<int>();

        [TableField(14, "spawnEffectScaleOffset")]
        public List<int> SpawnEffectScaleOffset { get; set; } = new List<int>();

        [TableField(15, "loopEffectPositionOffset")]
        public List<int> LoopEffectPositionOffset { get; set; } = new List<int>();

        [TableField(16, "loopEffectRotationOffset")]
        public List<int> LoopEffectRotationOffset { get; set; } = new List<int>();

        [TableField(17, "loopEffectScaleOffset")]
        public List<int> LoopEffectScaleOffset { get; set; } = new List<int>();

        [TableField(18, "hitEffectPositionOffset")]
        public List<int> HitEffectPositionOffset { get; set; } = new List<int>();

        [TableField(19, "hitEffectRotationOffset")]
        public List<int> HitEffectRotationOffset { get; set; } = new List<int>();

        [TableField(20, "hitEffectScaleOffset")]
        public List<int> HitEffectScaleOffset { get; set; } = new List<int>();

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
                        "spawnEffectPath", "loopEffectPath", "hitEffectPath",
                        "baseSpeed",
                        "spawnEffectPositionOffset", "spawnEffectRotationOffset", "spawnEffectScaleOffset",
                        "loopEffectPositionOffset", "loopEffectRotationOffset", "loopEffectScaleOffset",
                        "hitEffectPositionOffset", "hitEffectRotationOffset", "hitEffectScaleOffset"
                    },
                    Types = new List<string>
                    {
                        "int", "string", "string", "int", 
                        "string", "string", "int", "(array#sep=|),int",
                        "string", "string", "string",
                        "int",
                        "(array#sep=|),int", "(array#sep=|),int", "(array#sep=|),int",
                        "(array#sep=|),int", "(array#sep=|),int", "(array#sep=|),int",
                        "(array#sep=|),int", "(array#sep=|),int", "(array#sep=|),int"
                    },
                    Groups = new List<string>
                    {
                        "", "", "", "", "", "", "", "", "", "", "",
                        "", "", "", "", "", "", "", "", "", ""
                    },
                    Descriptions = new List<string>
                    {
                        "弹道ID", "弹道名称", "实体原型", "生命周期(帧)", 
                        "轨迹类型", "轨迹配置JSON", "允许穿透数量", "默认效果ID",
                        "开火特效资源路径", "飞行特效资源路径", "命中特效资源路径",
                        "基础速度(整型)",
                        "开火特效位置偏移", "开火特效旋转偏移", "开火特效缩放",
                        "飞行特效位置偏移", "飞行特效旋转偏移", "飞行特效缩放",
                        "命中特效位置偏移", "命中特效旋转偏移", "命中特效缩放"
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
                HitEffectPath = HitEffectPath,
                BaseSpeed = BaseSpeed,
                SpawnEffectPositionOffset = new List<int>(SpawnEffectPositionOffset ?? new List<int>()),
                SpawnEffectRotationOffset = new List<int>(SpawnEffectRotationOffset ?? new List<int>()),
                SpawnEffectScaleOffset = new List<int>(SpawnEffectScaleOffset ?? new List<int>()),
                LoopEffectPositionOffset = new List<int>(LoopEffectPositionOffset ?? new List<int>()),
                LoopEffectRotationOffset = new List<int>(LoopEffectRotationOffset ?? new List<int>()),
                LoopEffectScaleOffset = new List<int>(LoopEffectScaleOffset ?? new List<int>()),
                HitEffectPositionOffset = new List<int>(HitEffectPositionOffset ?? new List<int>()),
                HitEffectRotationOffset = new List<int>(HitEffectRotationOffset ?? new List<int>()),
                HitEffectScaleOffset = new List<int>(HitEffectScaleOffset ?? new List<int>())
            };
        }
    }
}
