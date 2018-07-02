using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Services.Events
{
    /// <summary>
    /// Represents an event subscription service
    /// </summary>
    public partial interface IEventSubscriptionService
    {
        /// <summary>
        /// Get event consumers
        /// </summary>
        /// <typeparam name="TEvent">Type of event</typeparam>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains list of event consumers</returns>
        Task<IList<IConsumer<TEvent>>> GetEventConsumersAsync<TEvent>(CancellationToken cancellationToken = default(CancellationToken));
    }
}