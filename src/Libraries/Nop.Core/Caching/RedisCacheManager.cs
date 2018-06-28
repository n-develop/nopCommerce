using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Configuration;
using StackExchange.Redis;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents a manager for caching in Redis store (http://redis.io/).
    /// Mostly it'll be used when running in a web farm or Azure. But of course it can be also used on any server or environment
    /// </summary>
    public partial class RedisCacheManager : IStaticCacheManager
    {
        #region Fields

        private readonly ICacheManager _perRequestCacheManager;
        private readonly IRedisConnectionWrapper _connectionWrapper;
        private IDatabase _db;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="perRequestCacheManager">Cache manager</param>
        /// <param name="connectionWrapper">ConnectionW wrapper</param>
        /// <param name="config">Config</param>
        public RedisCacheManager(ICacheManager perRequestCacheManager,
            IRedisConnectionWrapper connectionWrapper,
            NopConfig config)
        {
            if (string.IsNullOrEmpty(config.RedisCachingConnectionString))
                throw new Exception("Redis connection string is empty");

            this._perRequestCacheManager = perRequestCacheManager;

            // ConnectionMultiplexer.Connect should only be called once and shared between callers
            this._connectionWrapper = connectionWrapper;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get Redis database
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains Redis database</returns>
        protected virtual async Task<IDatabase> GetDbAsync(CancellationToken cancellationToken)
        {
            if (_db == null)
                _db = await _connectionWrapper.GetDatabaseAsync(cancellationToken: cancellationToken);

            return _db;
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
            //little performance workaround here:
            //we use "PerRequestCacheManager" to cache a loaded object in memory for the current HTTP request.
            //this way we won't connect to Redis server many times per HTTP request (e.g. each time to load a locale or setting)
            if (await _perRequestCacheManager.IsSetAsync(key, cancellationToken))
                return await _perRequestCacheManager.GetAsync<T>(key, cancellationToken);

            //get serialized item from cache
            var serializedItem = await (await GetDbAsync(cancellationToken)).StringGetAsync(key);
            if (!serializedItem.HasValue)
                return default(T);

            //deserialize item
            var item = JsonConvert.DeserializeObject<T>(serializedItem);
            if (item == null)
                return default(T);

            //set item in the per-request cache
            await _perRequestCacheManager.SetAsync(key, item, 0, cancellationToken);

            return item;
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
            if (data == null)
                return;

            //set cache time
            var expiresIn = TimeSpan.FromMinutes(cacheTime);

            //serialize item
            var serializedItem = JsonConvert.SerializeObject(data);

            //and set it to cache
            await (await GetDbAsync(cancellationToken)).StringSetAsync(key, serializedItem, expiresIn);
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
            //little performance workaround here:
            //we use "PerRequestCacheManager" to cache a loaded object in memory for the current HTTP request.
            //this way we won't connect to Redis server many times per HTTP request (e.g. each time to load a locale or setting)
            if (await _perRequestCacheManager.IsSetAsync(key, cancellationToken))
                return true;

            return await (await GetDbAsync(cancellationToken)).KeyExistsAsync(key);
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">Key of cached item</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that item is deleted</returns>
        public virtual async Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            //we should always persist the data protection key list
            if (key.Equals(NopCachingDefaults.RedisDataProtectionKey, StringComparison.OrdinalIgnoreCase))
                return;

            //remove item from caches
            await (await GetDbAsync(cancellationToken)).KeyDeleteAsync(key);
            await _perRequestCacheManager.RemoveAsync(key, cancellationToken);
        }

        /// <summary>
        /// Removes items by key pattern
        /// </summary>
        /// <param name="pattern">String key pattern</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that items are deleted by key pattern</returns>
        public virtual async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken)
        {
            await _perRequestCacheManager.RemoveByPatternAsync(pattern, cancellationToken);

            var db = await GetDbAsync(cancellationToken);
            var endPoints = await _connectionWrapper.GetEndPointsAsync(cancellationToken);
            var servers = await Task.WhenAll(endPoints.Select(endPoint => _connectionWrapper.GetServerAsync(endPoint, cancellationToken)));
            await Task.WhenAll(servers.Select(server =>
            {
                var keys = server.Keys(database: db.Database, pattern: $"*{pattern}*");

                //we should always persist the data protection key list
                keys = keys.Where(key => !key.ToString().Equals(NopCachingDefaults.RedisDataProtectionKey, StringComparison.OrdinalIgnoreCase));

                return db.KeyDeleteAsync(keys.ToArray());
            }));
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that all items are deleted</returns>
        public virtual async Task ClearAsync(CancellationToken cancellationToken)
        {
            await _perRequestCacheManager.ClearAsync(cancellationToken);

            var db = await GetDbAsync(cancellationToken);
            var endPoints = await _connectionWrapper.GetEndPointsAsync(cancellationToken);
            var servers = await Task.WhenAll(endPoints.Select(endPoint => _connectionWrapper.GetServerAsync(endPoint, cancellationToken)));
            await Task.WhenAll(servers.Select(server =>
            {
                //we can use the code below (commented), but it requires administration permission - ",allowAdmin=true"
                //return server.FlushDatabaseAsync();

                //that's why we manually delete all elements
                var keys = server.Keys(database: db.Database);

                //we should always persist the data protection key list
                keys = keys.Where(key => !key.ToString().Equals(NopCachingDefaults.RedisDataProtectionKey, StringComparison.OrdinalIgnoreCase));

                return db.KeyDeleteAsync(keys.ToArray());
            }));
        }

        /// <summary>
        /// Dispose cache manager
        /// </summary>
        public virtual void Dispose()
        {
            //if (_connectionWrapper != null)
            //    _connectionWrapper.Dispose();
        }

        #endregion
    }
}