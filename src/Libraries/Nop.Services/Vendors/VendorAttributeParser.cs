using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Nop.Core.Domain.Vendors;
using Nop.Services.Localization;

namespace Nop.Services.Vendors
{
    /// <summary>
    /// Represents a vendor attribute parser implementation
    /// </summary>
    public partial class VendorAttributeParser : IVendorAttributeParser
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IVendorAttributeService _vendorAttributeService;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="localizationService">Localization service</param>
        /// <param name="vendorAttributeService">Vendor attribute service</param>
        public VendorAttributeParser(ILocalizationService localizationService,
            IVendorAttributeService vendorAttributeService)
        {
            this._localizationService = localizationService;
            this._vendorAttributeService = vendorAttributeService;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets vendor attribute identifiers
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>List of vendor attribute identifiers</returns>
        protected virtual async Task<IList<int>> ParseVendorAttributeIdsAsync(string attributesXml, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() =>
            {
                var ids = new List<int>();
                if (string.IsNullOrEmpty(attributesXml))
                    return ids;

                try
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(attributesXml);

                    foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/VendorAttribute"))
                    {
                        if (node.Attributes?["ID"] == null) 
                            continue;

                        var str1 = node.Attributes["ID"].InnerText.Trim();
                        if (int.TryParse(str1, out var id))
                        {
                            ids.Add(id);
                        }
                    }
                }
                catch (Exception exc)
                {
                    Debug.Write(exc.ToString());
                }

                return ids;
            }, cancellationToken);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets vendor attributes from XML
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>List of vendor attributes</returns>
        public virtual async Task<IList<VendorAttribute>> ParseVendorAttributesAsync(string attributesXml, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new List<VendorAttribute>();
            if (string.IsNullOrEmpty(attributesXml))
                return result;

            var ids = await ParseVendorAttributeIdsAsync(attributesXml, cancellationToken);
            foreach (var id in ids)
            {
                var attribute = await _vendorAttributeService.GetVendorAttributeByIdAsync(id, cancellationToken);
                if (attribute != null)
                {
                    result.Add(attribute);
                }
            }
            return result;
        }

        /// <summary>
        /// Get vendor attribute values from XML
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>List of vendor attribute values</returns>
        public virtual async Task<IList<VendorAttributeValue>> ParseVendorAttributeValuesAsync(string attributesXml, CancellationToken cancellationToken = default(CancellationToken))
        {
            var values = new List<VendorAttributeValue>();
            if (string.IsNullOrEmpty(attributesXml))
                return values;

            var attributes = await ParseVendorAttributesAsync(attributesXml, cancellationToken);
            foreach (var attribute in attributes)
            {
                if (!attribute.ShouldHaveValues())
                    continue;

                var valuesStr = await ParseValuesAsync(attributesXml, attribute.Id, cancellationToken);
                foreach (var valueStr in valuesStr)
                {
                    if (string.IsNullOrEmpty(valueStr)) 
                        continue;

                    if (!int.TryParse(valueStr, out var id)) 
                        continue;

                    var value = await _vendorAttributeService.GetVendorAttributeValueByIdAsync(id, cancellationToken);
                    if (value != null)
                        values.Add(value);
                }
            }
            return values;
        }

        /// <summary>
        /// Gets values of the selected vendor attribute
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="vendorAttributeId">Vendor attribute identifier</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Values of the vendor attribute</returns>
        public virtual async Task<IList<string>> ParseValuesAsync(string attributesXml, int vendorAttributeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() =>
            {
                var selectedVendorAttributeValues = new List<string>();
                if (string.IsNullOrEmpty(attributesXml))
                    return selectedVendorAttributeValues;

                try
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(attributesXml);

                    var nodeList1 = xmlDoc.SelectNodes(@"//Attributes/VendorAttribute");
                    foreach (XmlNode node1 in nodeList1)
                    {
                        if (node1.Attributes?["ID"] == null) 
                            continue;

                        var str1 = node1.Attributes["ID"].InnerText.Trim();
                        if (!int.TryParse(str1, out var id)) 
                            continue;

                        if (id != vendorAttributeId) 
                            continue;

                        var nodeList2 = node1.SelectNodes(@"VendorAttributeValue/Value");
                        foreach (XmlNode node2 in nodeList2)
                        {
                            var value = node2.InnerText.Trim();
                            selectedVendorAttributeValues.Add(value);
                        }
                    }
                }
                catch (Exception exc)
                {
                    Debug.Write(exc.ToString());
                }

                return selectedVendorAttributeValues;
            }, cancellationToken);
        }

        /// <summary>
        /// Adds a vendor attribute
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="vendorAttribute">Vendor attribute</param>
        /// <param name="value">Value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Attributes in XML format</returns>
        public virtual async Task<string> AddVendorAttributeAsync(string attributesXml, VendorAttribute vendorAttribute, string value, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() =>
            {
                var result = string.Empty;
                try
                {
                    var xmlDoc = new XmlDocument();
                    if (string.IsNullOrEmpty(attributesXml))
                    {
                        var element1 = xmlDoc.CreateElement("Attributes");
                        xmlDoc.AppendChild(element1);
                    }
                    else
                    {
                        xmlDoc.LoadXml(attributesXml);
                    }

                    var rootElement = (XmlElement) xmlDoc.SelectSingleNode(@"//Attributes");

                    XmlElement attributeElement = null;
                    //find existing
                    var nodeList1 = xmlDoc.SelectNodes(@"//Attributes/VendorAttribute");
                    foreach (XmlNode node1 in nodeList1)
                    {
                        if (node1.Attributes?["ID"] == null)
                            continue;

                        var str1 = node1.Attributes["ID"].InnerText.Trim();
                        if (!int.TryParse(str1, out var id))
                            continue;

                        if (id != vendorAttribute.Id)
                            continue;

                        attributeElement = (XmlElement) node1;
                        break;
                    }

                    //create new one if not found
                    if (attributeElement == null)
                    {
                        attributeElement = xmlDoc.CreateElement("VendorAttribute");
                        attributeElement.SetAttribute("ID", vendorAttribute.Id.ToString());
                        rootElement.AppendChild(attributeElement);
                    }

                    var attributeValueElement = xmlDoc.CreateElement("VendorAttributeValue");
                    attributeElement.AppendChild(attributeValueElement);

                    var attributeValueValueElement = xmlDoc.CreateElement("Value");
                    attributeValueValueElement.InnerText = value;
                    attributeValueElement.AppendChild(attributeValueValueElement);

                    result = xmlDoc.OuterXml;
                }
                catch (Exception exc)
                {
                    Debug.Write(exc.ToString());
                }

                return result;
            }, cancellationToken);
        }

        /// <summary>
        /// Validates vendor attributes
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Warnings</returns>
        public virtual async Task<IList<string>> GetAttributeWarningsAsync(string attributesXml, CancellationToken cancellationToken = default(CancellationToken))
        {
            var warnings = new List<string>();

            //ensure it's our attributes
            var attributes1 = await ParseVendorAttributesAsync(attributesXml, cancellationToken);

            //validate required vendor attributes (whether they're chosen/selected/entered)
            var attributes2 = await _vendorAttributeService.GetAllVendorAttributesAsync(cancellationToken);
            foreach (var a2 in attributes2)
            {
                if (!a2.IsRequired) 
                    continue;

                var found = false;
                //selected vendor attributes
                foreach (var a1 in attributes1)
                {
                    if (a1.Id != a2.Id) 
                        continue;

                    var valuesStr = await ParseValuesAsync(attributesXml, a1.Id, cancellationToken);
                    if (valuesStr.Any(str1 => !string.IsNullOrEmpty(str1.Trim())))
                    {
                        found = true;
                    }
                }
                
                if (found) 
                    continue;

                //if not found
                var notFoundWarning = string.Format(_localizationService.GetResource("ShoppingCart.SelectAttribute"), a2.GetLocalized(a => a.Name));

                warnings.Add(notFoundWarning);
            }

            return warnings;
        }

        #endregion
    }
}