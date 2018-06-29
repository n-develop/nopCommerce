using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Vendors
{
    /// <summary>
    /// Represents a vendor attribute service
    /// </summary>
    public partial interface IVendorAttributeService
    {
        #region Vendor attributes

        /// <summary>
        /// Gets all vendor attributes
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor attributes</returns>
        Task<IList<VendorAttribute>> GetAllVendorAttributesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets a vendor attribute 
        /// </summary>
        /// <param name="vendorAttributeId">Vendor attribute identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor attribute</returns>
        Task<VendorAttribute> GetVendorAttributeByIdAsync(int vendorAttributeId, CancellationToken cancellationToken);

        /// <summary>
        /// Inserts a vendor attribute
        /// </summary>
        /// <param name="vendorAttribute">Vendor attribute</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task InsertVendorAttributeAsync(VendorAttribute vendorAttribute, CancellationToken cancellationToken);

        /// <summary>
        /// Updates a vendor attribute
        /// </summary>
        /// <param name="vendorAttribute">Vendor attribute</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task UpdateVendorAttributeAsync(VendorAttribute vendorAttribute, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a vendor attribute
        /// </summary>
        /// <param name="vendorAttribute">Vendor attribute</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task DeleteVendorAttributeAsync(VendorAttribute vendorAttribute, CancellationToken cancellationToken);

        #endregion

        #region Vendor attribute vallues

        /// <summary>
        /// Gets vendor attribute values by vendor attribute identifier
        /// </summary>
        /// <param name="vendorAttributeId">The vendor attribute identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor attribute values</returns>
        Task<IList<VendorAttributeValue>> GetVendorAttributeValuesAsync(int vendorAttributeId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a vendor attribute value
        /// </summary>
        /// <param name="vendorAttributeValueId">Vendor attribute value identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Vendor attribute value</returns>
        Task<VendorAttributeValue> GetVendorAttributeValueByIdAsync(int vendorAttributeValueId, CancellationToken cancellationToken);

        /// <summary>
        /// Inserts a vendor attribute value
        /// </summary>
        /// <param name="vendorAttributeValue">Vendor attribute value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task InsertVendorAttributeValueAsync(VendorAttributeValue vendorAttributeValue, CancellationToken cancellationToken);

        /// <summary>
        /// Updates a vendor attribute value
        /// </summary>
        /// <param name="vendorAttributeValue">Vendor attribute value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task UpdateVendorAttributeValueAsync(VendorAttributeValue vendorAttributeValue, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a vendor attribute value
        /// </summary>
        /// <param name="vendorAttributeValue">Vendor attribute value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        Task DeleteVendorAttributeValueAsync(VendorAttributeValue vendorAttributeValue, CancellationToken cancellationToken);

        #endregion
    }
}