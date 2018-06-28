using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nop.Core
{
    /// <summary>
    /// Represents a web helper
    /// </summary>
    public partial interface IWebHelper
    {
        /// <summary>
        /// Get URL referrer if exists
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the URL referrer</returns>
        Task<string> GetUrlReferrerAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get IP address from HTTP context
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the IP address</returns>
        Task<string> GetCurrentIpAddressAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets this page URL
        /// </summary>
        /// <param name="includeQueryString">Value indicating whether to include query strings</param>
        /// <param name="useSsl">Value indicating whether to get SSL secured page URL. Pass null to determine automatically</param>
        /// <param name="lowercaseUrl">Value indicating whether to lowercase URL</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the page URL</returns>
        Task<string> GetThisPageUrlAsync(bool includeQueryString, bool? useSsl = null, bool lowercaseUrl = false,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets a value indicating whether current connection is secured
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether current connection is secured</returns>
        Task<bool> IsCurrentConnectionSecuredAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets store host location
        /// </summary>
        /// <param name="useSsl">Whether to get SSL secured URL</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the store host location</returns>
        Task<string> GetStoreHostAsync(bool useSsl, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets store location
        /// </summary>
        /// <param name="useSsl">Whether to get SSL secured URL; pass null to determine automatically</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the store location</returns>
        Task<string> GetStoreLocationAsync(bool? useSsl = null,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns true if the requested resource is one of the typical resources that needn't be processed by the cms engine.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether a static resource is requested</returns>
        Task<bool> IsStaticResourceAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Modify query string of the URL
        /// </summary>
        /// <param name="url">Url to modify</param>
        /// <param name="key">Query parameter key to add</param>
        /// <param name="values">Query parameter values to add</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the URL with passed query parameter</returns>
        Task<string> ModifyQueryStringAsync(string url, string key, IEnumerable<string> values,
            CancellationToken cancellationToken);

        /// <summary>
        /// Remove query parameter from the URL
        /// </summary>
        /// <param name="url">Url to modify</param>
        /// <param name="key">Query parameter key to remove</param>
        /// <param name="value">Query parameter value to remove; pass null to remove all query parameters with the specified key</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the URL without passed query parameter</returns>
        Task<string> RemoveQueryStringAsync(string url, string key, string value = null,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets query string value by name
        /// </summary>
        /// <typeparam name="T">Returned value type</typeparam>
        /// <param name="name">Query parameter name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the query string value</returns>
        Task<T> QueryStringAsync<T>(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Restart application domain
        /// </summary>
        /// <param name="makeRedirect">A value indicating whether we should made redirection after restart</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that app domain is restarted</returns>
        Task RestartAppDomainAsync(bool makeRedirect = false, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets a value that indicates whether the client is being redirected to a new location
        /// </summary>
        bool IsRequestBeingRedirected { get; }

        /// <summary>
        /// Gets or sets a value that indicates whether the client is being redirected to a new location using POST
        /// </summary>
        bool IsPostBeingDone { get; set; }

        /// <summary>
        /// Gets current HTTP request protocol
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains current HTTP request protocol</returns>
        Task<string> GetCurrentRequestProtocolAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets whether the specified HTTP request URI references the local host.
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether the request is local</returns>
        Task<bool> IsLocalRequestAsync(HttpRequest req, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the raw path and full query of request
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the raw URL</returns>
        Task<string> GetRawUrlAsync(HttpRequest request, CancellationToken cancellationToken = default(CancellationToken));
    }
}