using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Vendors
{
    /// <summary>
    /// Represents a vendor attribute parser
    /// </summary>
    public partial interface IVendorAttributeParser
    {
        /// <summary>
        /// Gets vendor attributes from XML
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>List of vendor attributes</returns>
        Task<IList<VendorAttribute>> ParseVendorAttributesAsync(string attributesXml, CancellationToken cancellationToken);

        /// <summary>
        /// Get vendor attribute values from XML
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>List of vendor attribute values</returns>
        Task<IList<VendorAttributeValue>> ParseVendorAttributeValuesAsync(string attributesXml, CancellationToken cancellationToken);

        /// <summary>
        /// Gets values of the selected vendor attribute
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="vendorAttributeId">Vendor attribute identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Values of the vendor attribute</returns>
        Task<IList<string>> ParseValuesAsync(string attributesXml, int vendorAttributeId, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a vendor attribute
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="vendorAttribute">Vendor attribute</param>
        /// <param name="value">Value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Attributes in XML format</returns>
        Task<string> AddVendorAttributeAsync(string attributesXml, VendorAttribute vendorAttribute, string value, CancellationToken cancellationToken);

        /// <summary>
        /// Validates vendor attributes
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Warnings</returns>
        Task<IList<string>> GetAttributeWarningsAsync(string attributesXml, CancellationToken cancellationToken);
    }
}