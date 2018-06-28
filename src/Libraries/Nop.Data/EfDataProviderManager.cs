using System.Threading;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Data;

namespace Nop.Data
{
    /// <summary>
    /// Represents the Entity Framework data provider manager
    /// </summary>
    public partial class EfDataProviderManager : IDataProviderManager
    {
        #region Methods

        /// <summary>
        /// Get data provider
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains data provider</returns>
        public virtual async Task<IDataProvider> GetDataProviderAsync(CancellationToken cancellationToken)
        {
            var dataSettings = await DataSettingsManager.LoadSettingsAsync(cancellationToken: cancellationToken);
            var providerName = dataSettings?.DataProvider;
            switch (providerName)
            {
                case DataProviderType.SqlServer:
                    return new SqlServerDataProvider();

                //starting version 4.10 we support MS SQL Server only. SQL Server Compact is not supported anymore
                //but we leave this code because we plan to support other databases soon (e.g. MySQL)

                //case "sqlce":
                //    return new SqlCeDataProvider();

                default:
                    throw new NopException($"Not supported data provider name: '{providerName}'");
            }
        }

        #endregion
    }
}