using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Data
{
    /// <summary>
    /// Represents a data provider manager
    /// </summary>
    public partial interface IDataProviderManager
    {
        /// <summary>
        /// Get data provider
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains data provider</returns>
        Task<IDataProvider> GetDataProviderAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}