using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Data
{
    /// <summary>
    /// Represents a data provider
    /// </summary>
    public partial interface IDataProvider
    {
        #region Methods

        /// <summary>
        /// Initialize database
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that database is initialized</returns>
        Task InitializeDatabaseAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get a support database parameter object (used by stored procedures)
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains database parameter object</returns>
        Task<DbParameter> GetParameterAsync(CancellationToken cancellationToken = default(CancellationToken));

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this data provider supports backup
        /// </summary>
        bool BackupSupported { get; }

        /// <summary>
        /// Gets a maximum length of the data for HASHBYTES functions, returns 0 if HASHBYTES function is not supported
        /// </summary>
        int SupportedLengthOfBinaryHash { get; }

        #endregion
    }
}