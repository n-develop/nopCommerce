using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Html;
using Nop.Services.Localization;

namespace Nop.Services.Vendors
{
    /// <summary>
    /// Represents a vendor attribute formatter implementation
    /// </summary>
    public partial class VendorAttributeFormatter : IVendorAttributeFormatter
    {
        #region Fields

        private readonly IVendorAttributeParser _vendorAttributeParser;
        private readonly IVendorAttributeService _vendorAttributeService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="vendorAttributeParser">Vendor attribute parser</param>
        /// <param name="vendorAttributeService">Vendor attribute service</param>
        /// <param name="workContext">Work context</param>
        public VendorAttributeFormatter(IVendorAttributeParser vendorAttributeParser,
            IVendorAttributeService vendorAttributeService,
            IWorkContext workContext)
        {
            this._vendorAttributeParser = vendorAttributeParser;
            this._vendorAttributeService = vendorAttributeService;
            this._workContext = workContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Format vendor attributes
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="separator">Separator</param>
        /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Formatted attributes</returns>
        public virtual async Task<string> FormatAttributesAsync(string attributesXml, string separator = "<br />", bool htmlEncode = true, CancellationToken cancellationToken=default(CancellationToken))
        {
            var result = new StringBuilder();

            var attributes = await _vendorAttributeParser.ParseVendorAttributesAsync(attributesXml, cancellationToken);
            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                var valuesStr = await _vendorAttributeParser.ParseValuesAsync(attributesXml, attribute.Id, cancellationToken);
                for (var j = 0; j < valuesStr.Count; j++)
                {
                    var valueStr = valuesStr[j];
                    var formattedAttribute = "";
                    if (!attribute.ShouldHaveValues())
                    {
                        //no values
                        if (attribute.AttributeControlType == AttributeControlType.MultilineTextbox)
                        {
                            //multiline textbox
                            var attributeName = attribute.GetLocalized(a => a.Name, (await _workContext.GetWorkingLanguageAsync(cancellationToken)).Id);
                            //encode (if required)
                            if (htmlEncode)
                                attributeName = WebUtility.HtmlEncode(attributeName);
                            formattedAttribute = $"{attributeName}: {HtmlHelper.FormatText(valueStr, false, true, false, false, false, false)}";
                            //we never encode multiline textbox input
                        }
                        else if (attribute.AttributeControlType == AttributeControlType.FileUpload)
                        {
                            //file upload
                            //not supported for vendor attributes
                        }
                        else
                        {
                            //other attributes (textbox, datepicker)
                            formattedAttribute = $"{attribute.GetLocalized(a => a.Name, (await _workContext.GetWorkingLanguageAsync(cancellationToken)).Id)}: {valueStr}";
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                        }
                    }
                    else
                    {
                        if (int.TryParse(valueStr, out var attributeValueId))
                        {
                            var attributeValue = await _vendorAttributeService.GetVendorAttributeValueByIdAsync(attributeValueId, cancellationToken);
                            if (attributeValue != null)
                            {
                                formattedAttribute = $"{attribute.GetLocalized(a => a.Name, (await _workContext.GetWorkingLanguageAsync(cancellationToken)).Id)}: {attributeValue.GetLocalized(a => a.Name, (await _workContext.GetWorkingLanguageAsync(cancellationToken)).Id)}";
                            }
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                        }
                    }

                    if (string.IsNullOrEmpty(formattedAttribute)) 
                        continue;

                    if (i != 0 || j != 0)
                        result.Append(separator);
                    result.Append(formattedAttribute);
                }
            }

            return result.ToString();
        }

        #endregion
    }
}