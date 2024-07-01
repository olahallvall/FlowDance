namespace FlowDance.Common.Enums
{
    /// <summary>
    /// The first Span in a call chain (applies for both synchronous and asynchronous call chains) will be, a so called, RootSpan.
    /// A RootSpan are responsible for creation the stream and set the rules for when compensaion action can be exetuted.
    /// 
    /// BlockingCallChain / NonBlockingCallapplies descibes how Spans are joined together in run-time in a call chain. 
    /// Spans can either be joined together in a synchronous call chain (blocking architecture) or a asynchronous call chain (non-blocking architecture).
    /// </summary>
    public enum CompensationSpanOption
    {
        /// <summary>
        /// An ambient transaction is required by the scope. A correlationId/traceId must be available.
        /// </summary>
        Required = 0,

        /// <summary>
        /// A new transaction is always created for the span. A new correlationId/traceId will be generated.
        /// Spans will managed by the FlowDance server as they are part of a synchronous call chain. That means the compensation actions not will be 
        /// called until the RootSpan has been closed (Dispose). 
        /// </summary>
        RequiresNewBlockingCallChain = 1,

        /// <summary>
        /// A new transaction is always created for the span. A new correlationId/traceId will be generated.
        /// Spans will managed by the FlowDance server as they are part of a asynchronous call chain. That means the compensation actions will be 
        /// called regardless if the RootSpan has been closed (Dispose) or not. 
        /// </summary>
        RequiresNewNonBlockingCallChain = 2
    }
}
