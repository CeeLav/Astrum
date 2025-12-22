using System.Collections.Generic;

namespace Astrum.LogicCore.ViewRead
{
    public sealed class ViewReadStore
    {
        private readonly Dictionary<int, IViewReadBuffer> _buffersByTypeId = new Dictionary<int, IViewReadBuffer>(32);

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
            return any;
        }

        internal void Clear()
        {
            foreach (var buffer in _buffersByTypeId.Values)
            {
                buffer.Clear();
            }
            _buffersByTypeId.Clear();
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


