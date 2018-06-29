using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Vendors;
using Nop.Services.Events;

namespace Nop.Services.Vendors
{
    /// <summary>
    /// Represents a vendor attribute service implementation
    /// </summary>
    public partial class VendorAttributeService : IVendorAttributeService
    {
        #region Fields

        private readonly ICacheManager _cacheManager;
        private readonly IEventPublisher _eventPublisher;
        private readonly IRepository<VendorAttribute> _vendorAttributeRepository;
        private readonly IRepository<VendorAttributeValue> _vendorAttributeValueRepository;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="vendorAttributeRepository">Vendor attribute repository</param>
        /// <param name="vendorAttributeValueRepository">Vendor attribute value repository</param>
        public VendorAttributeService(ICacheManager cacheManager,
            IEventPublisher eventPublisher,
            IRepository<VendorAttribute> vendorAttributeRepository,
            IRepository<VendorAttributeValue> vendorAttributeValueRepository)
        {
            this._cacheManager = cacheManager;
            this._eventPublisher = eventPublisher;
            this._vendorAttributeRepository = vendorAttributeRepository;
            this._vendorAttributeValueRepository = vendorAttributeValueRepository;
        }

        #endregion

        #region Methods

        #region Vendor attributes

        /// <summary>
        /// Gets all vendor attributes
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the list of vendor attributes</returns>
        public virtual async Task<IList<VendorAttribute>> GetAllVendorAttributesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _cacheManager.GetAsync(NopVendorsServiceDefaults.VendorAttributesAllCacheKey, () =>
            {
                return _vendorAttributeRepository.Table
                    .OrderBy(vendorAttribute => vendorAttribute.DisplayOrder).ThenBy(vendorAttribute => vendorAttribute.Id)
                    .ToListAsync(cancellationToken);
            }, cancellationToken);
        }
        
        /// <summary>
        /// Gets a vendor attribute 
        /// </summary>
        /// <param name="vendorAttributeId">Vendor attribute identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor attribute</returns>
        public virtual async Task<VendorAttribute> GetVendorAttributeByIdAsync(int vendorAttributeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorAttributeId == 0)
                return null;

            var key = string.Format(NopVendorsServiceDefaults.VendorAttributesByIdCacheKey, vendorAttributeId);
            return await _cacheManager.GetAsync(key, () => _vendorAttributeRepository.GetByIdAsync(vendorAttributeId, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Inserts a vendor attribute
        /// </summary>
        /// <param name="vendorAttribute">Vendor attribute</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task InsertVendorAttributeAsync(VendorAttribute vendorAttribute, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorAttribute == null)
                throw new ArgumentNullException(nameof(vendorAttribute));

            await _vendorAttributeRepository.InsertAsync(vendorAttribute, cancellationToken);

            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributesPatternCacheKey, cancellationToken);
            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributeValuesPatternCacheKey, cancellationToken);

            //event notification
            _eventPublisher.EntityInserted(vendorAttribute);
        }

        /// <summary>
        /// Updates a vendor attribute
        /// </summary>
        /// <param name="vendorAttribute">Vendor attribute</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task UpdateVendorAttributeAsync(VendorAttribute vendorAttribute, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorAttribute == null)
                throw new ArgumentNullException(nameof(vendorAttribute));

            await _vendorAttributeRepository.UpdateAsync(vendorAttribute, cancellationToken);

            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributesPatternCacheKey, cancellationToken);
            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributeValuesPatternCacheKey, cancellationToken);

            //event notification
            _eventPublisher.EntityUpdated(vendorAttribute);
        }

        /// <summary>
        /// Deletes a vendor attribute
        /// </summary>
        /// <param name="vendorAttribute">Vendor attribute</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task DeleteVendorAttributeAsync(VendorAttribute vendorAttribute, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorAttribute == null)
                throw new ArgumentNullException(nameof(vendorAttribute));

            await _vendorAttributeRepository.DeleteAsync(vendorAttribute, cancellationToken);

            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributesPatternCacheKey, cancellationToken);
            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributeValuesPatternCacheKey, cancellationToken);

            //event notification
            _eventPublisher.EntityDeleted(vendorAttribute);
        }

        #endregion

        #region Vendor attribute values

        /// <summary>
        /// Gets vendor attribute values by vendor attribute identifier
        /// </summary>
        /// <param name="vendorAttributeId">The vendor attribute identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor attribute values</returns>
        public virtual async Task<IList<VendorAttributeValue>> GetVendorAttributeValuesAsync(int vendorAttributeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var key = string.Format(NopVendorsServiceDefaults.VendorAttributeValuesAllCacheKey, vendorAttributeId);
            return await _cacheManager.GetAsync(key, () =>
            {
                return _vendorAttributeValueRepository.Table
                    .OrderBy(vendorAttributeValue => vendorAttributeValue.DisplayOrder).ThenBy(vendorAttributeValue => vendorAttributeValue.Id)
                    .Where(vendorAttributeValue => vendorAttributeValue.VendorAttributeId == vendorAttributeId)
                    .ToListAsync(cancellationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// Gets a vendor attribute value
        /// </summary>
        /// <param name="vendorAttributeValueId">Vendor attribute value identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor attribute value</returns>
        public virtual async Task<VendorAttributeValue> GetVendorAttributeValueByIdAsync(int vendorAttributeValueId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorAttributeValueId == 0)
                return null;

            var key = string.Format(NopVendorsServiceDefaults.VendorAttributeValuesByIdCacheKey, vendorAttributeValueId);
            return await _cacheManager.GetAsync(key, () => _vendorAttributeValueRepository.GetByIdAsync(vendorAttributeValueId, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Inserts a vendor attribute value
        /// </summary>
        /// <param name="vendorAttributeValue">Vendor attribute value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task InsertVendorAttributeValueAsync(VendorAttributeValue vendorAttributeValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorAttributeValue == null)
                throw new ArgumentNullException(nameof(vendorAttributeValue));

            await _vendorAttributeValueRepository.InsertAsync(vendorAttributeValue, cancellationToken);

            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributeValuesPatternCacheKey, cancellationToken);

            //event notification
            _eventPublisher.EntityInserted(vendorAttributeValue);
        }

        /// <summary>
        /// Updates a vendor attribute value
        /// </summary>
        /// <param name="vendorAttributeValue">Vendor attribute value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task UpdateVendorAttributeValueAsync(VendorAttributeValue vendorAttributeValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorAttributeValue == null)
                throw new ArgumentNullException(nameof(vendorAttributeValue));

            await _vendorAttributeValueRepository.UpdateAsync(vendorAttributeValue, cancellationToken);

            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributesPatternCacheKey, cancellationToken);
            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributeValuesPatternCacheKey, cancellationToken);

            //event notification
            _eventPublisher.EntityUpdated(vendorAttributeValue);
        }

        /// <summary>
        /// Deletes a vendor attribute value
        /// </summary>
        /// <param name="vendorAttributeValue">Vendor attribute value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task DeleteVendorAttributeValueAsync(VendorAttributeValue vendorAttributeValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorAttributeValue == null)
                throw new ArgumentNullException(nameof(vendorAttributeValue));

            await _vendorAttributeValueRepository.DeleteAsync(vendorAttributeValue, cancellationToken);

            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributesPatternCacheKey, cancellationToken);
            await _cacheManager.RemoveByPatternAsync(NopVendorsServiceDefaults.VendorAttributeValuesPatternCacheKey, cancellationToken);

            //event notification
            _eventPublisher.EntityDeleted(vendorAttributeValue);
        }

        #endregion

        #endregion
    }
}