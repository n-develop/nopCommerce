using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Vendors
{
    /// <summary>
    /// Vendor service interface
    /// </summary>
    public partial interface IVendorService
    {
        /// <summary>
        /// Gets a vendor by vendor identifier
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor</returns>
        Task<Vendor> GetVendorByIdAsync(int vendorId, CancellationToken cancellationToken);

        /// <summary>
        /// Delete a vendor
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task DeleteVendorAsync(Vendor vendor, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all vendors
        /// </summary>
        /// <param name="name">Vendor name</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendors</returns>
        Task<IPagedList<Vendor>> GetAllVendorsAsync(string name = "", int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets vendors
        /// </summary>
        /// <param name="vendorIds">Vendor identifiers</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendors</returns>
        Task<IList<Vendor>> GetVendorsByIdsAsync(int[] vendorIds, CancellationToken cancellationToken);

        /// <summary>
        /// Inserts a vendor
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task InsertVendorAsync(Vendor vendor, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the vendor
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task UpdateVendorAsync(Vendor vendor, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a vendor note note
        /// </summary>
        /// <param name="vendorNoteId">The vendor note identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor note</returns>
        Task<VendorNote> GetVendorNoteByIdAsync(int vendorNoteId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a vendor note
        /// </summary>
        /// <param name="vendorNote">The vendor note</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task DeleteVendorNoteAsync(VendorNote vendorNote, CancellationToken cancellationToken);
    }
}