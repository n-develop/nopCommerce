using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Data;
using Nop.Core.Domain.Topics;
using Nop.Services.Events;

namespace Nop.Services.Topics
{
    /// <summary>
    /// Topic template service
    /// </summary>
    public partial class TopicTemplateService : ITopicTemplateService
    {
        #region Fields

        private readonly IRepository<TopicTemplate> _topicTemplateRepository;
        private readonly IEventPublisher _eventPublisher;

        #endregion
        
        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="topicTemplateRepository">Topic template repository</param>
        /// <param name="eventPublisher">Event publisher</param>
        public TopicTemplateService(IRepository<TopicTemplate> topicTemplateRepository, 
            IEventPublisher eventPublisher)
        {
            this._topicTemplateRepository = topicTemplateRepository;
            this._eventPublisher = eventPublisher;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Delete topic template
        /// </summary>
        /// <param name="topicTemplate">Topic template</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task DeleteTopicTemplateAsync(TopicTemplate topicTemplate, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (topicTemplate == null)
                throw new ArgumentNullException(nameof(topicTemplate));

            await _topicTemplateRepository.DeleteAsync(topicTemplate, cancellationToken);

            //event notification
            _eventPublisher.EntityDeleted(topicTemplate);
        }

        /// <summary>
        /// Gets all topic templates
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Topic templates</returns>
        public virtual async Task<IList<TopicTemplate>> GetAllTopicTemplatesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = from pt in _topicTemplateRepository.Table
                        orderby pt.DisplayOrder, pt.Id
                        select pt;

            var templates = await query.ToListAsync(cancellationToken);
            return templates;
        }
 
        /// <summary>
        /// Gets a topic template
        /// </summary>
        /// <param name="topicTemplateId">Topic template identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Topic template</returns>
        public virtual async Task<TopicTemplate> GetTopicTemplateByIdAsync(int topicTemplateId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (topicTemplateId == 0)
                return null;

            return await _topicTemplateRepository.GetByIdAsync(topicTemplateId, cancellationToken);
        }

        /// <summary>
        /// Inserts topic template
        /// </summary>
        /// <param name="topicTemplate">Topic template</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task InsertTopicTemplateAsync(TopicTemplate topicTemplate, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (topicTemplate == null)
                throw new ArgumentNullException(nameof(topicTemplate));

            await _topicTemplateRepository.InsertAsync(topicTemplate, cancellationToken);

            //event notification
            _eventPublisher.EntityInserted(topicTemplate);
        }

        /// <summary>
        /// Updates the topic template
        /// </summary>
        /// <param name="topicTemplate">Topic template</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task UpdateTopicTemplateAsync(TopicTemplate topicTemplate, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (topicTemplate == null)
                throw new ArgumentNullException(nameof(topicTemplate));

            await _topicTemplateRepository.UpdateAsync(topicTemplate, cancellationToken);

            //event notification
            _eventPublisher.EntityUpdated(topicTemplate);
        }
        
        #endregion
    }
}
