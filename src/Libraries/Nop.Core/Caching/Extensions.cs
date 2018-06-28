using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents extensions of the cache manager
    /// </summary>
    public static partial class CacheExtensions
    {
        /// <summary>
        /// Get a cached item. If it's not in the cache yet, then load and cache it
        /// </summary>
        /// <typeparam name="T">Type of cached item</typeparam>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="key">Cache key</param>
        /// <param name="acquire">Function to load item if it's not in the cache yet</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains cached item</returns>
        public static async Task<T> GetAsync<T>(this ICacheManager cacheManager, string key,
            Func<CancellationToken, Task<T>> acquire, CancellationToken cancellationToken = default(CancellationToken))
        {
            //use default cache time
            return await GetAsync(cacheManager, key, NopCachingDefaults.CacheTime, acquire, cancellationToken);
        }

        /// <summary>
        /// Get a cached item. If it's not in the cache yet, then load and cache it
        /// </summary>
        /// <typeparam name="T">Type of cached item</typeparam>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="key">Cache key</param>
        /// <param name="cacheTime">Cache time in minutes (0 - do not cache)</param>
        /// <param name="acquire">Function to load item if it's not in the cache yet</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains cached item</returns>
        public static async Task<T> GetAsync<T>(this ICacheManager cacheManager, string key, int cacheTime,
            Func<CancellationToken, Task<T>> acquire, CancellationToken cancellationToken = default(CancellationToken))
        {
            //item already is in cache, so return it
            if (await cacheManager.IsSetAsync(key, cancellationToken))
                return await cacheManager.GetAsync<T>(key, cancellationToken);

            //or create it using passed function
            var result = await acquire(cancellationToken);

            //and set in cache (if cache time is defined)
            if (cacheTime > 0)
                await cacheManager.SetAsync(key, result, cacheTime, cancellationToken);

            return result;
        }

        /// <summary>
        /// Removes items by pattern
        /// </summary>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="pattern">Pattern</param>
        /// <param name="keys">All keys in the cache</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that items are deleted by the pattern</returns>
        public static async Task RemoveByPatternAsync(this ICacheManager cacheManager, string pattern, IEnumerable<string> keys,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

            //remove cache keys that matches pattern
            await Task.WhenAll(keys.Where(key => regex.IsMatch(key)).Select(key => cacheManager.RemoveAsync(key, cancellationToken)));
        }
    }
}