using System;

namespace FlowDance.Common.Exceptions
{
    [Serializable]
    public class CompensationSpanValidationException : Exception
    {
        public CompensationSpanValidationException()
        {
        }

        public CompensationSpanValidationException(string message)
            : base(message)
        {
        }

        public CompensationSpanValidationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
