namespace FlowDance.Client.AspNetCore.ActionFilters
{
    public enum CompensationSpanOption
    {
        /// <summary>
        /// An ambient transaction is required by the scope.
        /// </summary>
        Required = 0,
        /// <summary>
        /// A new transaction is always created for the span.
        /// </summary>
        RequiresNew = 1
    }
}
