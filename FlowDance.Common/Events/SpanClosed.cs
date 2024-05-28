﻿namespace FlowDance.Common.Events
{
    /// <summary>
    /// Holds the data associated with the closing of a CompensationSpan.  
    /// </summary>
    public class SpanClosed : SpanEvent
    {
        public bool MarkedAsCommitted { get; set; }
        public bool ExceptionDetected { get; set; }
    }
}
