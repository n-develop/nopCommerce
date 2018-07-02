using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Topics;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Services.Topics
{
    /// <summary>
    /// Topic service
    /// </summary>
    public partial class TopicService : ITopicService
    {
        #region Fields

        private readonly IRepository<Topic> _topicRepository;
        private readonly IRepository<StoreMapping> _storeMappingRepository;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<AclRecord> _aclRepository;
        private readonly CatalogSettings _catalogSettings;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICacheManager _cacheManager;

        #endregion

        #region Ctor

        /// <summary>
        /// Topic service
        /// </summary>
        /// <param name="topicRepository">Topic repository</param>
        /// <param name="storeMappingRepository">Store mapping repository</param>
        /// <param name="aclService">ACL service</param>
        /// <param name="storeMappingService">Store mapping service</param>
        /// <param name="workContext">Work context</param>
        /// <param name="aclRepository">ACL repository</param>
        /// <param name="catalogSettings">Catalog settings</param>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="cacheManager">Cache manager</param>
        public TopicService(IRepository<Topic> topicRepository, 
            IRepository<StoreMapping> storeMappingRepository,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IWorkContext workContext,
            IRepository<AclRecord> aclRepository,
            CatalogSettings catalogSettings,
            IEventPublisher eventPublisher,
            ICacheManager cacheManager)
        {
            this._topicRepository = topicRepository;
            this._storeMappingRepository = storeMappingRepository;
            this._aclService = aclService;
            this._storeMappingService = storeMappingService;
            this._workContext = workContext;
            this._aclRepository = aclRepository;
            this._catalogSettings = catalogSettings;
            this._eventPublisher = eventPublisher;
            this._cacheManager = cacheManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Deletes a topic
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task DeleteTopicAsync(Topic topic, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            await _topicRepository.DeleteAsync(topic, cancellationToken);

            //cache
            await _cacheManager.RemoveByPatternAsync(NopTopicDefaults.TopicsPatternCacheKey, cancellationToken);

            //event notification
            _eventPublisher.EntityDeleted(topic);
        }

        /// <summary>
        /// Gets a topic
        /// </summary>
        /// <param name="topicId">The topic identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Topic</returns>
        public virtual async Task<Topic> GetTopicByIdAsync(int topicId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (topicId == 0)
                return null;

            var key = string.Format(NopTopicDefaults.TopicsByIdCacheKey, topicId);
            return await _cacheManager.GetAsync(key, async () => await _topicRepository.GetByIdAsync(topicId, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Gets a topic
        /// </summary>
        /// <param name="systemName">The topic system name</param>
        /// <param name="storeId">Store identifier; pass 0 to ignore filtering by store and load the first one</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Topic</returns>
        public virtual async Task<Topic> GetTopicBySystemNameAsync(string systemName, int storeId = 0, bool showHidden = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(systemName))
                return null;

            var query = _topicRepository.Table;
            query = query.Where(t => t.SystemName == systemName);
            if (!showHidden)
                query = query.Where(c => c.Published);
            query = query.OrderBy(t => t.Id);
            var topics = await query.ToListAsync(cancellationToken);

            if (storeId > 0)
            {
                //filter by store
                topics = topics.Where(x => _storeMappingService.Authorize(x, storeId)).ToList();
            }

            if (!showHidden)
            {
                //ACL (access control list)
                topics = topics.Where(x => _aclService.Authorize(x)).ToList();
            }

            return topics.FirstOrDefault();
        }

        /// <summary>
        /// Gets all topics
        /// </summary>
        /// <param name="storeId">Store identifier; pass 0 to load all records</param>
        /// <param name="ignorAcl">A value indicating whether to ignore ACL rules</param>
        /// <param name="showHidden">A value indicating whether to show hidden topics</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Topics</returns>
        public virtual async Task<IList<Topic>> GetAllTopicsAsync(int storeId, bool ignorAcl = false,
            bool showHidden = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var key = string.Format(NopTopicDefaults.TopicsAllCacheKey, storeId, ignorAcl, showHidden);
            return await _cacheManager.GetAsync(key, async () =>
            {
                var query = _topicRepository.Table;
                query = query.OrderBy(t => t.DisplayOrder).ThenBy(t => t.SystemName);

                if (!showHidden)
                    query = query.Where(t => t.Published);

                if ((storeId <= 0 || _catalogSettings.IgnoreStoreLimitations) && (ignorAcl || _catalogSettings.IgnoreAcl))
                    return await query.ToListAsync(cancellationToken);

                if (!ignorAcl && !_catalogSettings.IgnoreAcl)
                {
                    //ACL (access control list)
                    var allowedCustomerRolesIds = (await _workContext.GetCurrentCustomerAsync(cancellationToken))
                        .GetCustomerRoleIds();
                    query = from c in query
                        join acl in _aclRepository.Table
                            on new {c1 = c.Id, c2 = "Topic"} equals new {c1 = acl.EntityId, c2 = acl.EntityName} into
                            cAcl
                        from acl in cAcl.DefaultIfEmpty()
                        where !c.SubjectToAcl || allowedCustomerRolesIds.Contains(acl.CustomerRoleId)
                        select c;
                }

                if (!_catalogSettings.IgnoreStoreLimitations && storeId > 0)
                {
                    //Store mapping
                    query = from c in query
                        join sm in _storeMappingRepository.Table
                            on new {c1 = c.Id, c2 = "Topic"} equals new {c1 = sm.EntityId, c2 = sm.EntityName} into cSm
                        from sm in cSm.DefaultIfEmpty()
                        where !c.LimitedToStores || storeId == sm.StoreId
                        select c;
                }

                query = query.Distinct().OrderBy(t => t.DisplayOrder).ThenBy(t => t.SystemName);

                return await query.ToListAsync(cancellationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// Inserts a topic
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task InsertTopicAsync(Topic topic, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            await _topicRepository.InsertAsync(topic, cancellationToken);

            //cache
            await _cacheManager.RemoveByPatternAsync(NopTopicDefaults.TopicsPatternCacheKey, cancellationToken);

            //event notification
            _eventPublisher.EntityInserted(topic);
        }

        public virtual async Task UpdateTopicAsync(Topic topic, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            await _topicRepository.UpdateAsync(topic, cancellationToken);

            //cache
            await _cacheManager.RemoveByPatternAsync(NopTopicDefaults.TopicsPatternCacheKey, cancellationToken);

            //event notification
            _eventPublisher.EntityUpdated(topic);
        }

        #endregion
    }
}
