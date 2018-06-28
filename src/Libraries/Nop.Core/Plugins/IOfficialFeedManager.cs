using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Represents an official feed manager (official plugins from https://www.nopCommerce.com site)
    /// </summary>
    public partial interface IOfficialFeedManager
    {
        /// <summary>
        /// Get categories
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains list of plugin categories</returns>
        Task<IList<OfficialFeedCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get versions
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains list of plugin versions</returns>
        Task<IList<OfficialFeedVersion>> GetVersionsAsync(CancellationToken cancellationToken = default(CancellationToken));

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
        Task<IPagedList<OfficialFeedPlugin>> GetAllPluginsAsync(int categoryId = 0, int versionId = 0, int price = 0, 
            string searchTerm = "", int pageIndex = 0, int pageSize = int.MaxValue, CancellationToken cancellationToken = default(CancellationToken));
    }
}