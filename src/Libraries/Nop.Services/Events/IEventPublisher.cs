using System.Threading;
using System.Threading.Tasks;

namespace Nop.Services.Events
{
    /// <summary>
    /// Represents an event publisher
    /// </summary>
    public partial interface IEventPublisher
    {
        /// <summary>
        /// Publish event
        /// </summary>
        /// <typeparam name="TEvent">Type of event</typeparam>
        /// <param name="eventObject">Event object</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the event is published</returns>
        Task PublishEventAsync<TEvent>(TEvent eventObject, CancellationToken cancellationToken);
    }
}