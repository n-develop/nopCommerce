using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Topics;

namespace Nop.Services.Topics
{
    /// <summary>
    /// Topic template service interface
    /// </summary>
    public partial interface ITopicTemplateService
    {
        /// <summary>
        /// Delete topic template
        /// </summary>
        /// <param name="topicTemplate">Topic template</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task DeleteTopicTemplateAsync(TopicTemplate topicTemplate, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all topic templates
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Topic templates</returns>
        Task<IList<TopicTemplate>> GetAllTopicTemplatesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets a topic template
        /// </summary>
        /// <param name="topicTemplateId">Topic template identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Topic template</returns>
        Task<TopicTemplate> GetTopicTemplateByIdAsync(int topicTemplateId, CancellationToken cancellationToken);

        /// <summary>
        /// Inserts topic template
        /// </summary>
        /// <param name="topicTemplate">Topic template</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task InsertTopicTemplateAsync(TopicTemplate topicTemplate, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the topic template
        /// </summary>
        /// <param name="topicTemplate">Topic template</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task UpdateTopicTemplateAsync(TopicTemplate topicTemplate, CancellationToken cancellationToken);
    }
}
