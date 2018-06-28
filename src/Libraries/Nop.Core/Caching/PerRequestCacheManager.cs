using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents a manager for caching during an HTTP request (short term caching)
    /// </summary>
    public partial class PerRequestCacheManager : ICacheManager
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;

        #endregion

        #region Ctor

        public PerRequestCacheManager(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get a key/value collection that can be used to share data within the scope of this request 
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains request items</returns>
        protected virtual async Task<IDictionary<object, object>> GetItemsAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() => _httpContextAccessor.HttpContext?.Items, cancellationToken);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">Type of cached item</typeparam>
        /// <param name="key">Key of cached item</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains cached value associated with the specified key</returns>
        public virtual async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            var items = await GetItemsAsync(cancellationToken);
            if (items == null)
                return default(T);

            return (T)items[key];
        }

        /// <summary>
        /// Adds the specified key and object to the cache
        /// </summary>
        /// <param name="key">Key of cached item</param>
        /// <param name="data">Value for caching</param>
        /// <param name="cacheTime">Cache time in minutes</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that item is addded to the cache</returns>
        public virtual async Task SetAsync(string key, object data, int cacheTime, CancellationToken cancellationToken)
        {
            var items = await GetItemsAsync(cancellationToken);
            if (items == null)
                return;

            if (data != null)
                items[key] = data;
        }

        /// <summary>
        /// Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key">Key of cached item</param>
        /// <returns>True if item already is in cache; otherwise false</returns>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether item is already in the cache</returns>
        public virtual async Task<bool> IsSetAsync(string key, CancellationToken cancellationToken)
        {
            var items = await GetItemsAsync(cancellationToken);
            return items?[key] != null;
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">Key of cached item</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that item is deleted</returns>
        public virtual async Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            var items = await GetItemsAsync(cancellationToken);
            items?.Remove(key);
        }

        /// <summary>
        /// Removes items by key pattern
        /// </summary>
        /// <param name="pattern">String key pattern</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that items are deleted by key pattern</returns>
        public virtual async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken)
        {
            var items = await GetItemsAsync(cancellationToken);
            if (items == null)
                return;

            await this.RemoveByPatternAsync(pattern, items.Keys.Select(key => key.ToString()), cancellationToken);
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that all items are deleted</returns>
        public virtual async Task ClearAsync(CancellationToken cancellationToken)
        {
            var items = await GetItemsAsync(cancellationToken);
            items?.Clear();
        }

        /// <summary>
        /// Dispose cache manager
        /// </summary>
        public virtual void Dispose()
        {
            //nothing special
        }

        #endregion
    }
}