namespace Astrum.LogicCore.ViewRead
{
    internal interface IViewReadBuffer
    {
        bool SwapIfWritten();

        void RemoveEntity(long entityId);

        void Clear();
    }
}


