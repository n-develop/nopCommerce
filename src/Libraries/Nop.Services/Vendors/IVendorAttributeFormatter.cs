using System.Threading;
using System.Threading.Tasks;

namespace Nop.Services.Vendors
{
    /// <summary>
    /// Represents a vendor attribute formatter
    /// </summary>
    public partial interface IVendorAttributeFormatter
    {
        /// <summary>
        /// Format vendor attributes
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="separator">Separator</param>
        /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Formatted attributes</returns>
        Task<string> FormatAttributesAsync(string attributesXml, string separator = "<br />", bool htmlEncode = true, CancellationToken cancellationToken=default(CancellationToken));
    }
}