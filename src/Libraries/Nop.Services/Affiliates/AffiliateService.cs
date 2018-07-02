using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;

namespace Nop.Services.Affiliates
{
    /// <summary>
    /// Affiliate service
    /// </summary>
    public partial class AffiliateService : IAffiliateService
    {
        #region Fields

        private readonly IRepository<Affiliate> _affiliateRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IEventPublisher _eventPublisher;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="affiliateRepository">Affiliate repository</param>
        /// <param name="orderRepository">Order repository</param>
        /// <param name="eventPublisher">Event publisher</param>
        public AffiliateService(IRepository<Affiliate> affiliateRepository,
            IRepository<Order> orderRepository,
            IEventPublisher eventPublisher)
        {
            this._affiliateRepository = affiliateRepository;
            this._orderRepository = orderRepository;
            this._eventPublisher = eventPublisher;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets an affiliate by affiliate identifier
        /// </summary>
        /// <param name="affiliateId">Affiliate identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the affiliate</returns>
        public virtual async Task<Affiliate> GetAffiliateByIdAsync(int affiliateId, CancellationToken cancellationToken)
        {
            if (affiliateId == 0)
                return null;

            return await _affiliateRepository.GetByIdAsync(affiliateId, cancellationToken);
        }

        /// <summary>
        /// Gets an affiliate by friendly URL name
        /// </summary>
        /// <param name="friendlyUrlName">Friendly URL name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the affiliate</returns>
        public virtual async Task<Affiliate> GetAffiliateByFriendlyUrlNameAsync(string friendlyUrlName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(friendlyUrlName))
                return null;

            var query = from a in _affiliateRepository.Table
                        orderby a.Id
                        where a.FriendlyUrlName == friendlyUrlName
                        select a;
            var affiliate = await query.FirstOrDefaultAsync(cancellationToken);
            return affiliate;
        }

        /// <summary>
        /// Gets all affiliates
        /// </summary>
        /// <param name="friendlyUrlName">Friendly URL name; null to load all records</param>
        /// <param name="firstName">First name; null to load all records</param>
        /// <param name="lastName">Last name; null to load all records</param>
        /// <param name="loadOnlyWithOrders">Value indicating whether to load affiliates only with orders placed (by affiliated customers)</param>
        /// <param name="ordersCreatedFromUtc">Orders created date from (UTC); null to load all records. It's used only with "loadOnlyWithOrders" parameter st to "true".</param>
        /// <param name="ordersCreatedToUtc">Orders created date to (UTC); null to load all records. It's used only with "loadOnlyWithOrders" parameter st to "true".</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the list of affiliates</returns>
        public virtual async Task<IPagedList<Affiliate>> GetAllAffiliatesAsync(string friendlyUrlName = null,
            string firstName = null, string lastName = null,
            bool loadOnlyWithOrders = false,
            DateTime? ordersCreatedFromUtc = null, DateTime? ordersCreatedToUtc = null,
            int pageIndex = 0, int pageSize = int.MaxValue,
            bool showHidden = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = _affiliateRepository.Table;
            if (!string.IsNullOrWhiteSpace(friendlyUrlName))
                query = query.Where(a => a.FriendlyUrlName.Contains(friendlyUrlName));
            if (!string.IsNullOrWhiteSpace(firstName))
                query = query.Where(a => a.Address.FirstName.Contains(firstName));
            if (!string.IsNullOrWhiteSpace(lastName))
                query = query.Where(a => a.Address.LastName.Contains(lastName));
            if (!showHidden)
                query = query.Where(a => a.Active);
            query = query.Where(a => !a.Deleted);

            if (loadOnlyWithOrders)
            {
                var ordersQuery = _orderRepository.Table;
                if (ordersCreatedFromUtc.HasValue)
                    ordersQuery = ordersQuery.Where(o => ordersCreatedFromUtc.Value <= o.CreatedOnUtc);
                if (ordersCreatedToUtc.HasValue)
                    ordersQuery = ordersQuery.Where(o => ordersCreatedToUtc.Value >= o.CreatedOnUtc);
                ordersQuery = ordersQuery.Where(o => !o.Deleted);

                query = from a in query
                        join o in ordersQuery on a.Id equals o.AffiliateId into a_o
                        where a_o.Any()
                        select a;
            }

            query = query.OrderByDescending(a => a.Id);

            var affiliates = await query.ToPagedListAsync(pageIndex, pageSize, cancellationToken);
            return affiliates;
        }

        /// <summary>
        /// Inserts an affiliate
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the affiliate is inserted</returns>
        public virtual async Task InsertAffiliateAsync(Affiliate affiliate, CancellationToken cancellationToken)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            await _affiliateRepository.InsertAsync(affiliate, cancellationToken);

            //event notification
            await _eventPublisher.EntityInsertedAsync(affiliate, cancellationToken);
        }

        /// <summary>
        /// Updates the affiliate
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the affiliate is updated</returns>
        public virtual async Task UpdateAffiliateAsync(Affiliate affiliate, CancellationToken cancellationToken)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            await _affiliateRepository.UpdateAsync(affiliate, cancellationToken);

            //event notification
            await _eventPublisher.EntityUpdatedAsync(affiliate, cancellationToken);
        }

        /// <summary>
        /// Marks affiliate as deleted 
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the affiliate is deleted</returns>
        public virtual async Task DeleteAffiliateAsync(Affiliate affiliate, CancellationToken cancellationToken)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            affiliate.Deleted = true;
            await UpdateAffiliateAsync(affiliate, cancellationToken);

            //event notification
            await _eventPublisher.EntityDeletedAsync(affiliate, cancellationToken);
        }

        #endregion
    }
}