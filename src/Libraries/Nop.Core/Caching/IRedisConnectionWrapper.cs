using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents Redis connection wrapper
    /// </summary>
    public partial interface IRedisConnectionWrapper : IDisposable
    {
        /// <summary>
        /// Obtain an interactive connection to a database inside Redis
        /// </summary>
        /// <param name="db">Database number; pass null to use the default value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains Redis cache database</returns>
        Task<IDatabase> GetDatabaseAsync(int? db = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Obtain a configuration API for an individual server
        /// </summary>
        /// <param name="endPoint">The network endpoint</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains Redis server</returns>
        Task<IServer> GetServerAsync(EndPoint endPoint, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets all endpoints defined on the server
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains array of endpoints</returns>
        Task<EndPoint[]> GetEndPointsAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Delete all the keys of the database
        /// </summary>
        /// <param name="db">Database number; pass null to use the default value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that all keys in the database are deleted</returns>
        Task FlushDatabaseAsync(int? db = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}