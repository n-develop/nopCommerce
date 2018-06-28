using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents a memory cache manager 
    /// </summary>
    public partial class MemoryCacheManager : ILocker, IStaticCacheManager
    {
        #region Fields

        /// <summary>
        /// All keys of cache
        /// </summary>
        /// <remarks>Dictionary value indicating whether a key still exists in cache</remarks> 
        protected static readonly ConcurrentDictionary<string, bool> _allKeys;

        private readonly IMemoryCache _cache;

        /// <summary>
        /// Cancellation token for clear cache
        /// </summary>
        protected CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Ctor

        static MemoryCacheManager()
        {
            _allKeys = new ConcurrentDictionary<string, bool>();
        }

        public MemoryCacheManager(IMemoryCache cache)
        {
            _cache = cache;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Create entry options to item of memory cache
        /// </summary>
        /// <param name="cacheTime">Cache time</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains options</returns>
        protected virtual async Task<MemoryCacheEntryOptions> GetMemoryCacheEntryOptionsAsync(TimeSpan cacheTime, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var options = new MemoryCacheEntryOptions()
                    // add cancellation token for clear cache
                    .AddExpirationToken(new CancellationChangeToken(_cancellationTokenSource.Token))
                    //add post eviction callback
                    .RegisterPostEvictionCallback(PostEviction);

                //set cache time
                options.AbsoluteExpirationRelativeToNow = cacheTime;

                return options;
            }, cancellationToken);
        }

        /// <summary>
        /// Add key to dictionary
        /// </summary>
        /// <param name="key">Key of cached item</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains cache key</returns>
        protected virtual async Task<string> AddKeyAsync(string key, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                _allKeys.TryAdd(key, true);
                return key;
            }, cancellationToken);
        }

        /// <summary>
        /// Remove key from dictionary
        /// </summary>
        /// <param name="key">Key of cached item</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains cache key</returns>
        protected virtual async Task<string> RemoveKeyAsync(string key, CancellationToken cancellationToken)
        {
            await TryRemoveKeyAsync(key, cancellationToken);
            return key;
        }

        /// <summary>
        /// Try to remove a key from dictionary, or mark a key as not existing in cache
        /// </summary>
        /// <param name="key">Key of cached item</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that cache item is removed</returns>
        protected virtual async Task TryRemoveKeyAsync(string key, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                //try to remove key from dictionary
                if (!_allKeys.TryRemove(key, out _))
                    //if not possible to remove key from dictionary, then try to mark key as not existing in cache
                    _allKeys.TryUpdate(key, false, true);
            }, cancellationToken);
        }

        /// <summary>
        /// Remove all keys marked as not existing
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that all cache items are removed</returns>
        protected virtual async Task ClearKeysAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(_allKeys.Where(pair => !pair.Value).Select(pair => pair.Key)
                .Select(key => RemoveKeyAsync(key, cancellationToken)));
        }

        /// <summary>
        /// Post eviction
        /// </summary>
        /// <param name="key">Key of cached item</param>
        /// <param name="value">Value of cached item</param>
        /// <param name="reason">Eviction reason</param>
        /// <param name="state">State</param>
        protected virtual async void PostEviction(object key, object value, EvictionReason reason, object state)
        {
            //if cached item just change, then nothing doing
            if (reason == EvictionReason.Replaced)
                return;

            //try to remove all keys marked as not existing
            await ClearKeysAsync(default(CancellationToken));

            //try to remove this key from dictionary
            await TryRemoveKeyAsync(key.ToString(), default(CancellationToken));
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
            return await Task.Run(() => _cache.Get<T>(key), cancellationToken);
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
            if (data != null)
            {
                _cache.Set(await AddKeyAsync(key, cancellationToken), data,
                    await GetMemoryCacheEntryOptionsAsync(TimeSpan.FromMinutes(cacheTime), cancellationToken));
            }
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
            return await Task.Run(() => _cache.TryGetValue(key, out object _), cancellationToken);
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">Key of cached item</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that item is deleted</returns>
        public virtual async Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            _cache.Remove(await RemoveKeyAsync(key, cancellationToken));
        }

        /// <summary>
        /// Removes items by key pattern
        /// </summary>
        /// <param name="pattern">String key pattern</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that items are deleted by key pattern</returns>
        public virtual async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken)
        {
            await this.RemoveByPatternAsync(pattern, _allKeys.Where(pair => pair.Value).Select(pair => pair.Key), cancellationToken);
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that all items are deleted</returns>
        public virtual async Task ClearAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                //send cancellation request
                _cancellationTokenSource.Cancel();

                //releases all resources used by this cancellation token
                _cancellationTokenSource.Dispose();

                //recreate cancellation token
                _cancellationTokenSource = new CancellationTokenSource();
            }, cancellationToken);
        }

        /// <summary>
        /// Perform some action with exclusive in-memory lock
        /// </summary>
        /// <param name="resource">The thing we are locking on</param>
        /// <param name="expirationTime">The time after which the lock will automatically be expired by Redis</param>
        /// <param name="action">Action to be performed with locking</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether lock is acquired and action is performed</returns>
        public virtual async Task<bool> PerformActionWithLockAsync(string resource, TimeSpan expirationTime, Func<CancellationToken, Task> action,
            CancellationToken cancellationToken)
        {
            //ensure that lock is acquired
            if (!_allKeys.TryAdd(resource, true))
                return false;

            try
            {
                _cache.Set(resource, resource, await GetMemoryCacheEntryOptionsAsync(expirationTime, cancellationToken));

                //perform action
                await action(cancellationToken);

                return true;
            }
            finally
            {
                //release lock even if action fails
                await RemoveAsync(resource, cancellationToken);
            }
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