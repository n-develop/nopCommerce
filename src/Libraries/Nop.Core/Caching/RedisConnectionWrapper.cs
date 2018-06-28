using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Configuration;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents Redis connection wrapper implementation
    /// </summary>
    public partial class RedisConnectionWrapper : IRedisConnectionWrapper, ILocker
    {
        #region Fields

        private readonly object _lock = new object();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Lazy<string> _connectionString;
        private volatile ConnectionMultiplexer _connection;
        private volatile RedLockFactory _redisLockFactory;

        #endregion

        #region Ctor

        public RedisConnectionWrapper(NopConfig config)
        {
            this._connectionString = new Lazy<string>(() => config.RedisCachingConnectionString);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get connection to Redis servers
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains connection to Redis servers</returns>
        protected virtual async Task<ConnectionMultiplexer> GetConnectionAsync(CancellationToken cancellationToken)
        {
            if (_connection?.IsConnected ?? false)
                return _connection;

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_connection?.IsConnected ?? false)
                    return _connection;

                //Connection disconnected. Disposing connection...
                _connection?.Dispose();

                //Creating new instance of Redis Connection
                _connection = await ConnectionMultiplexer.ConnectAsync(_connectionString.Value);
            }
            finally
            {
                _semaphore.Release();
            }

            return _connection;
        }

        /// <summary>
        /// Create instance of RedLock factory
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains RedLock factory</returns>
        protected virtual async Task<RedLockFactory> CreateRedisLockFactoryAsync(CancellationToken cancellationToken)
        {
            if (_redisLockFactory != null)
                return _redisLockFactory;

            //get RedLock endpoints
            var endpoints = await GetEndPointsAsync(cancellationToken);
            var configurationOptions = ConfigurationOptions.Parse(_connectionString.Value);
            var redLockEndPoints = endpoints.Select(endPoint => new RedLockEndPoint
            {
                EndPoint = endPoint,
                Password = configurationOptions.Password,
                Ssl = configurationOptions.Ssl,
                RedisDatabase = configurationOptions.DefaultDatabase,
                ConfigCheckSeconds = configurationOptions.ConfigCheckSeconds,
                ConnectionTimeout = configurationOptions.ConnectTimeout,
                SyncTimeout = configurationOptions.SyncTimeout
            }).ToList();

            //create RedLock factory to use RedLock distributed lock algorithm
            _redisLockFactory = RedLockFactory.Create(redLockEndPoints);
            return _redisLockFactory;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Obtain an interactive connection to a database inside Redis
        /// </summary>
        /// <param name="db">Database number; pass null to use the default value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains Redis cache database</returns>
        public virtual async Task<IDatabase> GetDatabaseAsync(int? db = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (await GetConnectionAsync(cancellationToken)).GetDatabase(db ?? -1);
        }

        /// <summary>
        /// Obtain a configuration API for an individual server
        /// </summary>
        /// <param name="endPoint">The network endpoint</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains Redis server</returns>
        public virtual async Task<IServer> GetServerAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            return (await GetConnectionAsync(cancellationToken)).GetServer(endPoint);
        }

        /// <summary>
        /// Gets all endpoints defined on the server
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains array of endpoints</returns>
        public virtual async Task<EndPoint[]> GetEndPointsAsync(CancellationToken cancellationToken)
        {
            return (await GetConnectionAsync(cancellationToken)).GetEndPoints();
        }

        /// <summary>
        /// Delete all the keys of the database
        /// </summary>
        /// <param name="db">Database number; pass null to use the default value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that all keys in the database are deleted</returns>
        public virtual async Task FlushDatabaseAsync(int? db = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var endPoints = await GetEndPointsAsync(cancellationToken);
            var servers = await Task.WhenAll(endPoints.Select(endPoint => GetServerAsync(endPoint, cancellationToken)));
            Task.WaitAll(servers.Select(server => server.FlushDatabaseAsync(db ?? -1)).ToArray(), cancellationToken);
        }

        /// <summary>
        /// Perform some action with Redis distributed lock
        /// </summary>
        /// <param name="resource">The thing we are locking on</param>
        /// <param name="expirationTime">The time after which the lock will automatically be expired by Redis</param>
        /// <param name="action">Action to be performed with locking</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether lock is acquired and action is performed</returns>
        public virtual async Task<bool> PerformActionWithLockAsync(string resource, TimeSpan expirationTime, Func<CancellationToken, Task> action,
            CancellationToken cancellationToken)
        {
            //use RedLock library
            var redisLockFactory = await CreateRedisLockFactoryAsync(cancellationToken);
            using (var redisLock = await redisLockFactory.CreateLockAsync(resource, expirationTime))
            {
                //ensure that lock is acquired
                if (!redisLock.IsAcquired)
                    return false;

                //perform action
                await action(cancellationToken);

                return true;
            }
        }

        /// <summary>
        /// Release all resources associated with this object
        /// </summary>
        public void Dispose()
        {
            //dispose ConnectionMultiplexer
            _connection?.Dispose();

            //dispose RedLock factory
            _redisLockFactory?.Dispose();
        }

        #endregion
    }
}