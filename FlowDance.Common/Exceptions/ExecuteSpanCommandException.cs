using System;

namespace FlowDance.Common.Exceptions
{
    [Serializable]
    public class ExecuteSpanCommandException : Exception
    {
        public ExecuteSpanCommandException()
        {
        }

        public ExecuteSpanCommandException(string message)
            : base(message)
        {
        }

        public ExecuteSpanCommandException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
