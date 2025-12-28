using System;
using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.ViewRead
{
    /// <summary>
    /// ViewRead 帧同步：负责组件注册和帧末导出
    /// </summary>
    public static class ViewReadFrameSync
    {
        // 委托定义：(entity, store) => void
        private delegate void ExportDelegate(Entity entity, ViewReadStore store);
        
        // 注册表：componentTypeId -> 导出委托
        private static readonly Dictionary<int, ExportDelegate> _exporters = new Dictionary<int, ExportDelegate>();
        
        /// <summary>
        /// 注册组件的 ViewRead 导出器（泛型版本，类型安全）
        /// </summary>
        /// <typeparam name="TComponent">组件类型</typeparam>
        /// <typeparam name="TViewRead">ViewRead 结构体类型</typeparam>
        /// <param name="componentTypeId">组件类型ID</param>
        /// <param name="createViewRead">从组件创建 ViewRead 的委托</param>
        /// <param name="createInvalid">创建无效 ViewRead 的委托</param>
        public static void Register<TComponent, TViewRead>(
            int componentTypeId,
            Func<TComponent, TViewRead> createViewRead,
            Func<long, TViewRead> createInvalid)
            where TComponent : BaseComponent
            where TViewRead : struct
        {
            _exporters[componentTypeId] = (entity, store) =>
            {
                if (entity.GetComponentById(componentTypeId) is TComponent comp)
                {
                    var vr = createViewRead(comp);
                    store.WriteBack(entity.UniqueId, componentTypeId, in vr);
                }
                else
                {
                    var vr = createInvalid(entity.UniqueId);
                    store.WriteBack(entity.UniqueId, componentTypeId, in vr);
                }
            };
        }
        
        /// <summary>
        /// 检查组件是否已注册导出器
        /// </summary>
        public static bool IsRegistered(int componentTypeId)
        {
            return _exporters.ContainsKey(componentTypeId);
        }
        
        /// <summary>
        /// Logic 帧末同步：遍历脏组件，导出到 ViewRead 双缓冲的 back，然后 swap
        /// </summary>
        public static void EndOfLogicFrame(World world)
        {
            if (world?.Entities == null || world.ViewReads == null)
            {
                return;
            }

            var store = world.ViewReads;

            // 线程安全：先复制 Entities 的值到列表，避免遍历时集合被修改
            Entity[] entitiesSnapshot;
            lock (world.Entities)
            {
                entitiesSnapshot = new Entity[world.Entities.Count];
                world.Entities.Values.CopyTo(entitiesSnapshot, 0);
            }

            foreach (var entity in entitiesSnapshot)
            {
                if (entity == null) continue;

                // 线程安全：使用快照方法获取脏组件 ID，内部已加锁
                var dirtyIdsCopy = entity.GetDirtyComponentIdsSnapshot();
                if (dirtyIdsCopy.Length == 0) continue;

                foreach (var componentTypeId in dirtyIdsCopy)
                {
                    // 查字典执行导出（如果已注册）
                    if (_exporters.TryGetValue(componentTypeId, out var exporter))
                    {
                        exporter(entity, store);
                    }
                    // 无论是否注册导出器，都记录脏组件（View 层可能需要监听未迁移的组件）
                    store.RecordDirty(entity.UniqueId, componentTypeId);
                }
                
                // 在逻辑线程中清理脏标记（线程安全，内部已加锁）
                entity.ClearDirtyComponents();
            }

            // 交换所有有写入的 buffer
            store.SwapAllIfWritten();
        }
    }
}


