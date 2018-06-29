using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Vendors;
using Nop.Services.Events;

namespace Nop.Services.Vendors
{
    /// <summary>
    /// Vendor service
    /// </summary>
    public partial class VendorService : IVendorService
    {
        #region Fields

        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IRepository<VendorNote> _vendorNoteRepository;
        private readonly IEventPublisher _eventPublisher;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="vendorRepository">Vendor repository</param>
        /// <param name="vendorNoteRepository">Vendor note repository</param>
        /// <param name="eventPublisher">Event publisher</param>
        public VendorService(IRepository<Vendor> vendorRepository,
            IRepository<VendorNote> vendorNoteRepository,
            IEventPublisher eventPublisher)
        {
            this._vendorRepository = vendorRepository;
            this._vendorNoteRepository = vendorNoteRepository;
            this._eventPublisher = eventPublisher;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a vendor by vendor identifier
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor</returns>
        public virtual async Task<Vendor> GetVendorByIdAsync(int vendorId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorId == 0)
                return null;

            return await _vendorRepository.GetByIdAsync(vendorId, cancellationToken);
        }


        /// <summary>
        /// Delete a vendor
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task DeleteVendorAsync(Vendor vendor, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            vendor.Deleted = true;
            await UpdateVendorAsync(vendor, cancellationToken);

            //event notification
            _eventPublisher.EntityDeleted(vendor);
        }

        /// <summary>
        /// Gets all vendors
        /// </summary>
        /// <param name="name">Vendor name</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendors</returns>
        public virtual async Task<IPagedList<Vendor>> GetAllVendorsAsync(string name = "", int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = _vendorRepository.Table;
            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(v => v.Name.Contains(name));
            if (!showHidden)
                query = query.Where(v => v.Active);
           
            query = query.Where(v => !v.Deleted);
            query = query.OrderBy(v => v.DisplayOrder).ThenBy(v => v.Name);

            return await Task.Run(() =>
            {
                var vendors = new PagedList<Vendor>(query, pageIndex, pageSize);
                return vendors;
            }, cancellationToken);
        }

        /// <summary>
        /// Gets vendors
        /// </summary>
        /// <param name="vendorIds">Vendor identifiers</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendors</returns>
        public virtual async Task<IList<Vendor>> GetVendorsByIdsAsync(int[] vendorIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = _vendorRepository.Table;
            if (vendorIds != null)
                query = query.Where(v => vendorIds.Contains(v.Id));

            return await query.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Inserts a vendor
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task InsertVendorAsync(Vendor vendor, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            await _vendorRepository.InsertAsync(vendor, cancellationToken);

            //event notification
            _eventPublisher.EntityInserted(vendor);
        }

        /// <summary>
        /// Updates the vendor
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task UpdateVendorAsync(Vendor vendor, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            await _vendorRepository.UpdateAsync(vendor, cancellationToken);

            //event notification
            _eventPublisher.EntityUpdated(vendor);
        }

        /// <summary>
        /// Gets a vendor note note
        /// </summary>
        /// <param name="vendorNoteId">The vendor note identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor note</returns>
        public virtual async Task<VendorNote> GetVendorNoteByIdAsync(int vendorNoteId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorNoteId == 0)
                return null;

            return await _vendorNoteRepository.GetByIdAsync(vendorNoteId, cancellationToken);
        }

        /// <summary>
        /// Deletes a vendor note
        /// </summary>
        /// <param name="vendorNote">The vendor note</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        public virtual async Task DeleteVendorNoteAsync(VendorNote vendorNote, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (vendorNote == null)
                throw new ArgumentNullException(nameof(vendorNote));

            await _vendorNoteRepository.DeleteAsync(vendorNote, cancellationToken);

            //event notification
            _eventPublisher.EntityDeleted(vendorNote);
        }

        #endregion
    }
}