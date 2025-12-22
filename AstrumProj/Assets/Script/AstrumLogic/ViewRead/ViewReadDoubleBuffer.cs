using System.Collections.Generic;

namespace Astrum.LogicCore.ViewRead
{
    internal sealed class ViewReadDoubleBuffer<TViewRead> : IViewReadBuffer
        where TViewRead : struct
    {
        private Dictionary<long, TViewRead> _front;
        private Dictionary<long, TViewRead> _back;

        private int _frontGeneration;
        private int _backBaselineGeneration;
        private bool _hasWriteThisFrame;

        public ViewReadDoubleBuffer(int initialCapacity = 256)
        {
            _front = new Dictionary<long, TViewRead>(initialCapacity);
            _back = new Dictionary<long, TViewRead>(initialCapacity);
            _frontGeneration = 0;
            _backBaselineGeneration = 0;
            _hasWriteThisFrame = false;
        }

        public bool TryGetFront(long entityId, out TViewRead value)
        {
            return _front.TryGetValue(entityId, out value);
        }

        public void WriteBack(long entityId, in TViewRead value)
        {
            EnsureBackSyncedToFrontIfNeeded();
            _back[entityId] = value;
            _hasWriteThisFrame = true;
        }

        public bool SwapIfWritten()
        {
            if (!_hasWriteThisFrame)
            {
                return false;
            }

            var tmp = _front;
            _front = _back;
            _back = tmp;

            _frontGeneration++;
            _backBaselineGeneration = _frontGeneration - 1;
            _hasWriteThisFrame = false;
            return true;
        }

        public void RemoveEntity(long entityId)
        {
            // 删除是一种“写入”，需要发布到 front，所以写 back 并触发 swap。
            EnsureBackSyncedToFrontIfNeeded();
            if (_back.Remove(entityId))
            {
                _hasWriteThisFrame = true;
            }
        }

        public void Clear()
        {
            _front.Clear();
            _back.Clear();
            _frontGeneration = 0;
            _backBaselineGeneration = 0;
            _hasWriteThisFrame = false;
        }

        private void EnsureBackSyncedToFrontIfNeeded()
        {
            if (_backBaselineGeneration == _frontGeneration)
            {
                return;
            }

            _back.Clear();
            foreach (var kv in _front)
            {
                _back[kv.Key] = kv.Value;
            }

            _backBaselineGeneration = _frontGeneration;
        }
    }
}


