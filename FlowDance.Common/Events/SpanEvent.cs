﻿using System;

namespace FlowDance.Common.Events
{
    /// <summary>
    /// Base class for a Span event.  
    /// </summary>
    public class SpanEvent
    {
        public Guid TraceId { get; set; }

        public Guid SpanId { get; set; }

        public DateTime Timestamp { get; set; }
    }
}