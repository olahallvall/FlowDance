namespace FlowDance.Client.AspNetCore.ActionFilters
{
    public enum CompensationSpanOption
    {
        /// <summary>
        /// An ambient transaction is required by the scope. A correlationId/traceId must be available.
        /// </summary>
        Required = 0,
        /// <summary>
        /// A new transaction is always created for the span. A new correlationId/traceId will be generated.
        /// </summary>
        RequiresNew = 1
    }
}
