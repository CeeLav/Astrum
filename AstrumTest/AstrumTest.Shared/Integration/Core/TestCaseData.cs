using System.Collections.Generic;
using Astrum.LogicCore;
using TrueSync;

namespace AstrumTest.Shared.Integration.Core
{
    /// <summary>
    /// 测试用例数据
    /// </summary>
    public class TestCaseData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<EntityTemplate> EntityTemplates { get; set; } = new();
        public List<FrameData> Frames { get; set; } = new();
    }

    /// <summary>
    /// 实体模板 - 定义实体的配置模板
    /// 通过 CreateEntity 输入指令创建实例
    /// </summary>
    public class EntityTemplate
    {
        public string TemplateId { get; set; }  // 模板ID（如 "knight1", "boss1"）
        public int RoleId { get; set; }
        public int Team { get; set; }
        public int? CustomHealth { get; set; }
    }

    /// <summary>
    /// 帧数据 - 测试的核心单位
    /// 每一帧包含：输入 → 查询 → 预期输出
    /// </summary>
    public class FrameData
    {
        public int FrameNumber { get; set; }             // 帧号
        public string Comment { get; set; }              // 注释（用于说明这一帧做什么）
        public List<InputCommand> Inputs { get; set; } = new();   // 输入指令
        public List<QueryCommand> Queries { get; set; } = new();  // 查询指令
        public Dictionary<string, object> ExpectedOutputs { get; set; } = new();  // 预期输出
    }

    /// <summary>
    /// 输入指令（玩家操作、AI指令、实体创建等）
    /// </summary>
    public class InputCommand
    {
        public string Type { get; set; }  // "CreateEntity", "CastSkill", "Move", etc.
        
        // 实体标识（字符串ID，如 "entity_0", "boss_1"）
        public string EntityId { get; set; }
        
        // 创建实体相关
        public string TemplateId { get; set; }  // 引用 EntityTemplate 的 TemplateId
        public PositionData Position { get; set; }
        
        // 技能相关
        public int? SkillId { get; set; }
        public string TargetId { get; set; }  // 目标实体ID
        
        // 移动相关
        public PositionData TargetPosition { get; set; }
        public PositionData Direction { get; set; }
        
        // 物品相关
        public int? ItemId { get; set; }
    }

    /// <summary>
    /// 查询指令（查询实体状态）
    /// </summary>
    public class QueryCommand
    {
        public string Type { get; set; }       // "EntityHealth", "EntityPosition", "EntityIsAlive", etc.
        public string EntityId { get; set; }   // 查询哪个实体（字符串ID）
        public string TargetId { get; set; }   // 可选：第二个实体（用于距离查询等）
        public string ResultKey { get; set; }  // 结果键（用于匹配 ExpectedOutputs）
    }

    /// <summary>
    /// 位置数据（用于JSON序列化）
    /// </summary>
    public class PositionData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public TSVector ToTSVector()
        {
            return new TSVector((FP)X, (FP)Y, (FP)Z);
        }
    }

    /// <summary>
    /// 测试结果
    /// </summary>
    public class TestResult
    {
        public string TestCaseName { get; set; }
        public bool Success { get; set; }
        public string FailureMessage { get; set; }
        public int TotalFrames { get; set; }
        public int PassedFrames { get; set; }
        public int FailedFrames { get; set; }
    }
}

