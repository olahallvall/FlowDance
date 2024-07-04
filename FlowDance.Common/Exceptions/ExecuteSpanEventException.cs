using System;

namespace FlowDance.Common.Exceptions
{
    [Serializable]
    public class ExecuteSpanEventException : Exception
    {
        public ExecuteSpanEventException()
        {
        }

        public ExecuteSpanEventException(string message)
            : base(message)
        {
        }

        public ExecuteSpanEventException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
