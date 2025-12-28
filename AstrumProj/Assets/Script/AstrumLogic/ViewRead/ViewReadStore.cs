using System.Collections.Generic;

namespace Astrum.LogicCore.ViewRead
{
    /// <summary>
    /// 脏组件记录：实体ID + 组件类型ID
    /// </summary>
    public readonly struct DirtyComponentRecord
    {
        public readonly long EntityId;
        public readonly int ComponentTypeId;

        public DirtyComponentRecord(long entityId, int componentTypeId)
        {
            EntityId = entityId;
            ComponentTypeId = componentTypeId;
        }
    }

    public sealed class ViewReadStore
    {
        private readonly Dictionary<int, IViewReadBuffer> _buffersByTypeId = new Dictionary<int, IViewReadBuffer>(32);

        // 脏组件双缓冲：Logic 线程写 back，View 线程读 front
        private List<DirtyComponentRecord> _dirtyFront = new List<DirtyComponentRecord>(64);
        private List<DirtyComponentRecord> _dirtyBack = new List<DirtyComponentRecord>(64);

        public bool TryGet<TViewRead>(long entityId, int componentTypeId, out TViewRead value)
            where TViewRead : struct
        {
            if (_buffersByTypeId.TryGetValue(componentTypeId, out var buffer) &&
                buffer is ViewReadDoubleBuffer<TViewRead> typed)
            {
                return typed.TryGetFront(entityId, out value);
            }
            
            value = default;
            return false;
        }

        internal void WriteBack<TViewRead>(long entityId, int componentTypeId, in TViewRead value)
            where TViewRead : struct
        {
            GetOrCreateBuffer<TViewRead>(componentTypeId).WriteBack(entityId, in value);
        }

        /// <summary>
        /// Logic 线程调用：记录本帧更新的脏组件
        /// </summary>
        internal void RecordDirty(long entityId, int componentTypeId)
        {
            _dirtyBack.Add(new DirtyComponentRecord(entityId, componentTypeId));
        }

        internal void RemoveEntityFromAll(long entityId)
        {
            foreach (var buffer in _buffersByTypeId.Values)
            {
                buffer.RemoveEntity(entityId);
            }
        }

        internal bool SwapAllIfWritten()
        {
            bool any = false;
            foreach (var buffer in _buffersByTypeId.Values)
            {
                any |= buffer.SwapIfWritten();
            }

            // 交换脏组件列表
            var tmp = _dirtyFront;
            _dirtyFront = _dirtyBack;
            _dirtyBack = tmp;
            _dirtyBack.Clear();

            return any;
        }

        /// <summary>
        /// View 线程调用：获取本帧更新的脏组件列表
        /// </summary>
        public IReadOnlyList<DirtyComponentRecord> GetDirtyComponents()
        {
            return _dirtyFront;
        }

        internal void Clear()
        {
            foreach (var buffer in _buffersByTypeId.Values)
            {
                buffer.Clear();
            }
            _buffersByTypeId.Clear();
            _dirtyFront.Clear();
            _dirtyBack.Clear();
        }

        private ViewReadDoubleBuffer<TViewRead> GetOrCreateBuffer<TViewRead>(int componentTypeId)
            where TViewRead : struct
        {
            if (_buffersByTypeId.TryGetValue(componentTypeId, out var buffer))
            {
                return (ViewReadDoubleBuffer<TViewRead>)buffer;
            }

            var created = new ViewReadDoubleBuffer<TViewRead>();
            _buffersByTypeId[componentTypeId] = created;
            return created;
        }
    }
}


