using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Stores;

namespace Nop.Core
{
    /// <summary>
    /// Represents a store context
    /// </summary>
    public partial interface IStoreContext
    {
        /// <summary>
        /// Get the current store
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the current store</returns>
        Task<Store> GetCurrentStoreAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get active store scope configuration
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the active store scope configuration</returns>
        Task<int> GetActiveStoreScopeConfigurationAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}