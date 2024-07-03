using System;

namespace FlowDance.Common.Exceptions
{
    [Serializable]
    public class CompensationSpanCreationException : Exception
    {
        public CompensationSpanCreationException()
        {
        }

        public CompensationSpanCreationException(string message)
            : base(message)
        {
        }

        public CompensationSpanCreationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
