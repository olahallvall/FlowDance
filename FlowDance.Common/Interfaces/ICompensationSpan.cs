using System;

namespace FlowDance.Common.Interfaces
{
    public interface ICompensationSpan: IDisposable
    {
        void Complete();
    }
}
