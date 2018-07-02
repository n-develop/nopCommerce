using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Topics;

namespace Nop.Services.Topics
{
    /// <summary>
    /// Topic service interface
    /// </summary>
    public partial interface ITopicService
    {
        /// <summary>
        /// Deletes a topic
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task DeleteTopicAsync(Topic topic, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a topic
        /// </summary>
        /// <param name="topicId">The topic identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Topic</returns>
        Task<Topic> GetTopicByIdAsync(int topicId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a topic
        /// </summary>
        /// <param name="systemName">The topic system name</param>
        /// <param name="storeId">Store identifier; pass 0 to ignore filtering by store and load the first one</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Topic</returns>
        Task<Topic> GetTopicBySystemNameAsync(string systemName, int storeId = 0, bool showHidden = false, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets all topics
        /// </summary>
        /// <param name="storeId">Store identifier; pass 0 to load all records</param>
        /// <param name="ignorAcl">A value indicating whether to ignore ACL rules</param>
        /// <param name="showHidden">A value indicating whether to show hidden topics</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Topics</returns>
        Task<IList<Topic>> GetAllTopicsAsync(int storeId, bool ignorAcl = false, bool showHidden = false, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Inserts a topic
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task InsertTopicAsync(Topic topic, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the topic
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task UpdateTopicAsync(Topic topic, CancellationToken cancellationToken);
    }
}
