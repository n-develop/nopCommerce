using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Represents a locker
    /// </summary>
    public partial interface ILocker
    {
        /// <summary>
        /// Perform some action with exclusive lock
        /// </summary>
        /// <param name="resource">The key we are locking on</param>
        /// <param name="expirationTime">The time after which the lock will automatically be expired</param>
        /// <param name="action">Action to be performed with locking</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether lock is acquired and action is performed</returns>
        Task<bool> PerformActionWithLockAsync(string resource, TimeSpan expirationTime, Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}