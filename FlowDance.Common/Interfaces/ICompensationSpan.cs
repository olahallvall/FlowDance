using System;

namespace FlowDance.Common.Interfaces
{
    public interface ICompensationSpan: IDisposable
    {
        void Complete();
        void AddCompensationData(string compensationData);
        void AddCompensationData(string compensationData, string compensationDataIdentifier);
    }
}
