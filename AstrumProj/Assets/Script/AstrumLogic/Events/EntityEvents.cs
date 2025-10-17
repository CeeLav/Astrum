using System;
using Astrum.Generated;
using Astrum.LogicCore.Core;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 实体创建事件数据
    /// </summary>
    public class EntityCreatedEventData : EventData
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public long EntityId { get; set; }
        
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName { get; set; }
        
        /// <summary>
        /// 实体类型
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// 世界ID
        /// </summary>
        public int WorldId { get; set; }
        
        /// <summary>
        /// 房间ID
        /// </summary>
        public long RoomId { get; set; }
        
        /// <summary>
        /// 实体创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }
        
        /// <summary>
        /// 实体数据（可选，用于传递额外的实体信息）
        /// </summary>
        public object EntityData { get; set; }
        
        public EntityCreatedEventData()
        {
            CreationTime = DateTime.Now;
        }
        
        public EntityCreatedEventData(Entity entity, int worldId, long roomId)
        {
            EntityId = entity.UniqueId;
            EntityName = entity.Name;
            EntityType = entity.GetType().Name;
            WorldId = worldId;
            RoomId = roomId;
            CreationTime = entity.CreationTime;
        }
    }
    
    /// <summary>
    /// 实体销毁事件数据
    /// </summary>
    public class EntityDestroyedEventData : EventData
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public long EntityId { get; set; }
        
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName { get; set; }
        
        /// <summary>
        /// 实体类型
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// 世界ID
        /// </summary>
        public int WorldId { get; set; }
        
        /// <summary>
        /// 房间ID
        /// </summary>
        public long RoomId { get; set; }
        
        /// <summary>
        /// 销毁时间
        /// </summary>
        public DateTime DestroyTime { get; set; }
        
        public EntityDestroyedEventData()
        {
            DestroyTime = DateTime.Now;
        }
        
        public EntityDestroyedEventData(Entity entity, int worldId, long roomId)
        {
            EntityId = entity.UniqueId;
            EntityName = entity.Name;
            EntityType = entity.GetType().Name;
            WorldId = worldId;
            RoomId = roomId;
            DestroyTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 实体更新事件数据
    /// </summary>
    public class EntityUpdatedEventData : EventData
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public long EntityId { get; set; }
        
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName { get; set; }
        
        /// <summary>
        /// 实体类型
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// 世界ID
        /// </summary>
        public int WorldId { get; set; }
        
        /// <summary>
        /// 房间ID
        /// </summary>
        public long RoomId { get; set; }
        
        /// <summary>
        /// 更新类型（位置、状态、属性等）
        /// </summary>
        public string UpdateType { get; set; }
        
        /// <summary>
        /// 更新数据
        /// </summary>
        public object UpdateData { get; set; }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
        
        public EntityUpdatedEventData()
        {
            UpdateTime = DateTime.Now;
        }
        
        public EntityUpdatedEventData(Entity entity, int worldId, long roomId, string updateType, object updateData)
        {
            EntityId = entity.UniqueId;
            EntityName = entity.Name;
            EntityType = entity.GetType().Name;
            WorldId = worldId;
            RoomId = roomId;
            UpdateType = updateType;
            UpdateData = updateData;
            UpdateTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 实体激活状态变化事件数据
    /// </summary>
    public class EntityActiveStateChangedEventData : EventData
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public long EntityId { get; set; }
        
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName { get; set; }
        
        /// <summary>
        /// 实体类型
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// 世界ID
        /// </summary>
        public int WorldId { get; set; }
        
        /// <summary>
        /// 房间ID
        /// </summary>
        public long RoomId { get; set; }
        
        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// 状态变化时间
        /// </summary>
        public DateTime StateChangeTime { get; set; }
        
        public EntityActiveStateChangedEventData()
        {
            StateChangeTime = DateTime.Now;
        }
        
        public EntityActiveStateChangedEventData(Entity entity, int worldId, long roomId, bool isActive)
        {
            EntityId = entity.UniqueId;
            EntityName = entity.Name;
            EntityType = entity.GetType().Name;
            WorldId = worldId;
            RoomId = roomId;
            IsActive = isActive;
            StateChangeTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 实体组件变化事件数据
    /// </summary>
    public class EntityComponentChangedEventData : EventData
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public long EntityId { get; set; }
        
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName { get; set; }
        
        /// <summary>
        /// 实体类型
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// 世界ID
        /// </summary>
        public int WorldId { get; set; }
        
        /// <summary>
        /// 房间ID
        /// </summary>
        public long RoomId { get; set; }
        
        /// <summary>
        /// 组件类型
        /// </summary>
        public string ComponentType { get; set; }
        
        /// <summary>
        /// 变化类型（添加、移除、更新）
        /// </summary>
        public string ChangeType { get; set; }
        
        /// <summary>
        /// 组件数据
        /// </summary>
        public object ComponentData { get; set; }
        
        /// <summary>
        /// 变化时间
        /// </summary>
        public DateTime ChangeTime { get; set; }
        
        public EntityComponentChangedEventData()
        {
            ChangeTime = DateTime.Now;
        }
        
        public EntityComponentChangedEventData(Entity entity, int worldId, long roomId, string componentType, string changeType, object componentData)
        {
            EntityId = entity.UniqueId;
            EntityName = entity.Name;
            EntityType = entity.GetType().Name;
            WorldId = worldId;
            RoomId = roomId;
            ComponentType = componentType;
            ChangeType = changeType;
            ComponentData = componentData;
            ChangeTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 帧输入上传事件数据
    /// </summary>
    public class FrameDataUploadEventData : EventData
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public int FrameID { get; set; }

        public LSInput Input { get; set; }
        
        public FrameDataUploadEventData(int frameID, LSInput lSInput)
        {
            FrameID = frameID;
            Input = lSInput;
        }
    }
    
    /// <summary>
    /// 新玩家创建事件数据
    /// </summary>
    public class NewPlayerEventData : EventData
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public long PlayerID { get; set; }

        public int BornInfo { get; set; }
        
        public NewPlayerEventData(long playerID, int bornInfo)
        {
            PlayerID = playerID;
            BornInfo = bornInfo;
        }
    }
    
    /// <summary>
    /// 实体死亡事件数据
    /// </summary>
    public class EntityDiedEventData : EventData
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public long EntityId { get; set; }
        
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName { get; set; }
        
        /// <summary>
        /// 实体类型
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// 世界ID
        /// </summary>
        public int WorldId { get; set; }
        
        /// <summary>
        /// 房间ID
        /// </summary>
        public long RoomId { get; set; }
        
        /// <summary>
        /// 击杀者ID（可空，-1表示环境/自杀）
        /// </summary>
        public long KillerId { get; set; } = -1;
        
        /// <summary>
        /// 致死技能ID（可空，0表示非技能致死）
        /// </summary>
        public int SkillId { get; set; } = 0;
        
        /// <summary>
        /// 死亡时间
        /// </summary>
        public DateTime DeathTime { get; set; }
        
        public EntityDiedEventData()
        {
            DeathTime = DateTime.Now;
        }
        
        public EntityDiedEventData(Entity entity, int worldId, long roomId, long killerId = -1, int skillId = 0)
        {
            EntityId = entity.UniqueId;
            EntityName = entity.Name;
            EntityType = entity.GetType().Name;
            WorldId = worldId;
            RoomId = roomId;
            KillerId = killerId;
            SkillId = skillId;
            DeathTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 实体复活事件数据（预留）
    /// </summary>
    public class EntityRevivedEventData : EventData
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public long EntityId { get; set; }
        
        /// <summary>
        /// 世界ID
        /// </summary>
        public int WorldId { get; set; }
        
        /// <summary>
        /// 房间ID
        /// </summary>
        public long RoomId { get; set; }
        
        /// <summary>
        /// 复活时生命值百分比
        /// </summary>
        public TrueSync.FP ReviveHealthPercent { get; set; } = (TrueSync.FP)1.0;
        
        /// <summary>
        /// 复活时间
        /// </summary>
        public DateTime ReviveTime { get; set; }
        
        public EntityRevivedEventData()
        {
            ReviveTime = DateTime.Now;
        }
    }
}