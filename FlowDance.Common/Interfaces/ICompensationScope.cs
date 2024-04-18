using System;

namespace FlowDance.Common.Interfaces
{
    public interface ICompensationScope : IDisposable
    {
        void Complete();
    }
}
