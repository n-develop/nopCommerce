using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Infrastructure;

namespace Nop.Services.Events
{
    /// <summary>
    /// Represents the event subscription service implementation
    /// </summary>
    public partial class EventSubscriptionService : IEventSubscriptionService
    {
        /// <summary>
        /// Get event consumers
        /// </summary>
        /// <typeparam name="TEvent">Type of event</typeparam>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains list of event consumers</returns>
        public virtual async Task<IList<IConsumer<TEvent>>> GetEventConsumersAsync<TEvent>(CancellationToken cancellationToken)
        {
            var eventConsumers = await EngineContext.Current.ResolveAllAsync<IConsumer<TEvent>>(cancellationToken);
            return eventConsumers.ToList();
        }
    }
}