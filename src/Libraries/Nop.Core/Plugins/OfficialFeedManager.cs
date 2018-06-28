using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Nop.Core.Extensions;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Represents the official feed manager implementation (official plugins from https://www.nopCommerce.com site)
    /// </summary>
    public partial class OfficialFeedManager : IOfficialFeedManager
    {
        #region Utilities

        private static string MakeUrl(string query, params object[] args)
        {
            var url = "https://www.nopcommerce.com/extensionsxml.aspx?" + query;

            return string.Format(url, args);
        }

        /// <summary>
        /// Get XML document with plugin feed
        /// </summary>
        /// <param name="feedQuery">Query string</param>
        /// <param name="args">Parameters</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains XML document</returns>
        private async Task<XmlDocument> GetDocumentAsync(string feedQuery, IEnumerable<object> args = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = WebRequest.Create(MakeUrl(feedQuery, args));
            request.Timeout = 5000;
            using (var response = await request.GetResponseAsync())
            {
                using (var dataStream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(dataStream ?? throw new ArgumentNullException(nameof(dataStream))))
                    {
                        var responseFromServer = await reader.ReadToEndAsync();

                        var xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(responseFromServer);
                        return xmlDoc;
                    }
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get categories
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains list of plugin categories</returns>
        public virtual async Task<IList<OfficialFeedCategory>> GetCategoriesAsync(CancellationToken cancellationToken)
        {
            var xmlDocument = await GetDocumentAsync("getCategories=1", cancellationToken: cancellationToken);
            return xmlDocument.SelectNodes(@"//categories/category").Cast<XmlNode>().Select(node => new OfficialFeedCategory
            {
                Id = int.Parse(node.ElText(@"id")),
                ParentCategoryId = int.Parse(node.ElText(@"parentCategoryId")),
                Name = node.ElText(@"name")
            }).ToList();
        }

        /// <summary>
        /// Get versions
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains list of plugin versions</returns>
        public virtual async Task<IList<OfficialFeedVersion>> GetVersionsAsync(CancellationToken cancellationToken)
        {
            var xmlDocument = await GetDocumentAsync("getVersions=1", cancellationToken: cancellationToken);
            return xmlDocument.SelectNodes(@"//versions/version").Cast<XmlNode>().Select(node => new OfficialFeedVersion
            {
                Id = int.Parse(node.ElText(@"id")),
                Name = node.ElText(@"name")
            }).ToList();
        }

        /// <summary>
        /// Get all plugins
        /// </summary>
        /// <param name="categoryId">Category identifier</param>
        /// <param name="versionId">Version identifier</param>
        /// <param name="price">Price; 0 - all, 10 - free, 20 - paid</param>
        /// <param name="searchTerm">Search term</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains list of plugins</returns>
        public virtual async Task<IPagedList<OfficialFeedPlugin>> GetAllPluginsAsync(int categoryId = 0, int versionId = 0, int price = 0,
            string searchTerm = "", int pageIndex = 0, int pageSize = int.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            //pageSize parameter is currently ignored by official site (set to 15)
            var xmlDoc = await GetDocumentAsync("category={0}&version={1}&price={2}&pageIndex={3}&pageSize={4}&searchTerm={5}",
                new object[] { categoryId, versionId, price, pageIndex, pageSize, WebUtility.UrlEncode(searchTerm) }, cancellationToken);

            var list = xmlDoc.SelectNodes(@"//extensions/extension").Cast<XmlNode>().Select(node => new OfficialFeedPlugin
            {
                Name = node.ElText(@"name"),
                Url = node.ElText(@"url"),
                PictureUrl = node.ElText(@"picture"),
                Category = node.ElText(@"category"),
                SupportedVersions = node.ElText(@"versions"),
                Price = node.ElText(@"price")
            }).ToList();

            var totalRecords = int.Parse(xmlDoc.SelectNodes(@"//totalRecords")[0].ElText(@"value"));

            return new PagedList<OfficialFeedPlugin>(list, pageIndex, pageSize, totalRecords);
        }

        #endregion
    }
}