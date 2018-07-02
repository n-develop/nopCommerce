using System.Threading;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Events;

namespace Nop.Services.Events
{
    /// <summary>
    /// Represents event publisher extensions
    /// </summary>
    public static partial class EventPublisherExtensions
    {
        /// <summary>
        /// Publish entity inserted event
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="entity">Entity</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the event is published</returns>
        public static async Task EntityInsertedAsync<TEntity>(this IEventPublisher eventPublisher, TEntity entity,
            CancellationToken cancellationToken = default(CancellationToken)) where TEntity : BaseEntity
        {
            await eventPublisher.PublishEventAsync(new EntityInsertedEvent<TEntity>(entity), cancellationToken);
        }

        /// <summary>
        /// Publish entity updated event
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="entity">Entity</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the event is published</returns>
        public static async Task EntityUpdatedAsync<TEntity>(this IEventPublisher eventPublisher, TEntity entity,
            CancellationToken cancellationToken = default(CancellationToken)) where TEntity : BaseEntity
        {
            await eventPublisher.PublishEventAsync(new EntityUpdatedEvent<TEntity>(entity), cancellationToken);
        }

        /// <summary>
        /// Publish entity deleted event
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="entity">Entity</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the event is published</returns>
        public static async Task EntityDeletedAsync<TEntity>(this IEventPublisher eventPublisher, TEntity entity,
            CancellationToken cancellationToken = default(CancellationToken)) where TEntity : BaseEntity
        {
            await eventPublisher.PublishEventAsync(new EntityDeletedEvent<TEntity>(entity), cancellationToken);
        }
    }
}