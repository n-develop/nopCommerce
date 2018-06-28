using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Infrastructure
{
    /// <summary>
    /// Represents a task that runs on startup
    /// </summary>
    public partial interface IStartupTask
    {
        /// <summary>
        /// Executes a task
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that startup task is executed</returns>
        Task ExecuteAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets order of this startup task implementation
        /// </summary>
        int Order { get; }
    }
}