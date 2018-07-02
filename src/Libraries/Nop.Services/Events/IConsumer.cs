using System.Threading;
using System.Threading.Tasks;

namespace Nop.Services.Events
{
    /// <summary>
    /// Represents an event consumer
    /// </summary>
    /// <typeparam name="TEvent">Type of event</typeparam>
    public partial interface IConsumer<TEvent>
    {
        /// <summary>
        /// Handle event
        /// </summary>
        /// <param name="eventMessage">Event</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the event is handled</returns>
        Task HandleEventAsync(TEvent eventMessage, CancellationToken cancellationToken = default(CancellationToken));
    }
}