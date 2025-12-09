using System;
using Astrum.Generated;
using Astrum.LogicCore.Core;
using cfg;

namespace Astrum.CommonBase
{
    /// <summary>
    /// 子原型变化类型
    /// </summary>
    public enum SubArchetypeChangeType
    {
        /// <summary>
        /// 挂载子原型
        /// </summary>
        Attach,
        
        /// <summary>
        /// 卸载子原型
        /// </summary>
        Detach
    }
    
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
        /// 实体数据（可选，用于传递额外的实体信息）
        /// </summary>
        public object EntityData { get; set; }
        
        public EntityCreatedEventData()
        {
        }
        
        public EntityCreatedEventData(Entity entity, int worldId, long roomId)
        {
            EntityId = entity.UniqueId;
            EntityName = entity.Name;
            EntityType = entity.GetType().Name;
            WorldId = worldId;
            RoomId = roomId;
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
    /// 世界回滚事件数据（用于通知 View 层同步 EntityView）
    /// </summary>
    public class WorldRollbackEventData : EventData
    {
        /// <summary>
        /// 世界ID
        /// </summary>
        public int WorldId { get; set; }
        
        /// <summary>
        /// 房间ID
        /// </summary>
        public long RoomId { get; set; }
        
        /// <summary>
        /// 回滚到的帧号
        /// </summary>
        public int RollbackFrame { get; set; }
        
        /// <summary>
        /// 回滚时间
        /// </summary>
        public DateTime RollbackTime { get; set; }
        
        public WorldRollbackEventData()
        {
            RollbackTime = DateTime.Now;
        }
        
        public WorldRollbackEventData(int worldId, long roomId, int rollbackFrame)
        {
            WorldId = worldId;
            RoomId = roomId;
            RollbackFrame = rollbackFrame;
            RollbackTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 实体子原型变化事件数据
    /// </summary>
    public class EntitySubArchetypeChangedEventData : EventData
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
        /// 子原型类型
        /// </summary>
        public EArchetype SubArchetype { get; set; }
        
        /// <summary>
        /// 变化类型
        /// </summary>
        public SubArchetypeChangeType ChangeType { get; set; }
        
        /// <summary>
        /// 变化时间
        /// </summary>
        public DateTime ChangeTime { get; set; }
        
        public EntitySubArchetypeChangedEventData()
        {
            ChangeTime = DateTime.Now;
        }
        
        public EntitySubArchetypeChangedEventData(Entity entity, int worldId, long roomId, EArchetype subArchetype, SubArchetypeChangeType changeType)
        {
            EntityId = entity.UniqueId;
            EntityName = entity.Name;
            EntityType = entity.GetType().Name;
            WorldId = worldId;
            RoomId = roomId;
            SubArchetype = subArchetype;
            ChangeType = changeType;
            ChangeTime = DateTime.Now;
        }
    }
}