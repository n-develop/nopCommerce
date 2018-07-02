using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Infrastructure;
using Nop.Core.Plugins;
using Nop.Services.Logging;

namespace Nop.Services.Events
{
    /// <summary>
    /// Represents the event publisher implementation
    /// </summary>
    public partial class EventPublisher : IEventPublisher
    {
        #region Fields

        private readonly IEventSubscriptionService _eventSubscriptionService;

        #endregion

        #region Ctor

        public EventPublisher(IEventSubscriptionService eventSubscriptionService)
        {
            this._eventSubscriptionService = eventSubscriptionService;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Publish event to consumer
        /// </summary>
        /// <typeparam name="TEvent">Type of event</typeparam>
        /// <param name="consumer">Event consumer</param>
        /// <param name="eventObject">Event object</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the event is published to the passed consumer</returns>
        protected virtual async Task PublishEventAsync<TEvent>(IConsumer<TEvent> consumer, TEvent eventObject, CancellationToken cancellationToken)
        {
            try
            {
                await consumer.HandleEventAsync(eventObject, cancellationToken);
            }
            catch (Exception exc)
            {
                //log error
                var logger = await EngineContext.Current.ResolveAsync<ILogger>(cancellationToken);
                //we put in to nested try-catch to prevent possible cyclic (if some error occurs)
                try
                {
                    logger.Error(exc.Message, exc);
                }
                catch (Exception)
                {
                    //do nothing
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Publish event
        /// </summary>
        /// <typeparam name="TEvent">Type of event</typeparam>
        /// <param name="eventObject">Event object</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the event is published</returns>
        public virtual async Task PublishEventAsync<TEvent>(TEvent eventObject, CancellationToken cancellationToken)
        {
            //get all event consumers, excluding from not installed plugins
            var eventConsumers = await _eventSubscriptionService.GetEventConsumersAsync<TEvent>(cancellationToken);
            eventConsumers = eventConsumers.Where(consumer => PluginManager.FindPlugin(consumer.GetType())?.Installed ?? true).ToList();

            //publish event to consumers
            Task.WaitAll(eventConsumers.Select(consumer => PublishEventAsync(consumer, eventObject, cancellationToken)).ToArray(), cancellationToken);
        }

        #endregion
    }
}