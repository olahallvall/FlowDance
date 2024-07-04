using System;

namespace FlowDance.Common.Exceptions
{
    [Serializable]
    public class SpanListValidationException : Exception
    {
        public SpanListValidationException()
        {
        }

        public SpanListValidationException(string message)
            : base(message)
        {
        }

        public SpanListValidationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
