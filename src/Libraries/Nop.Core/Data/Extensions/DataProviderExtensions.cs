using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Data.Extensions
{
    /// <summary>
    /// Represents data provider extensions
    /// </summary>
    public static partial class DataProviderExtensions
    {
        #region Utilities

        /// <summary>
        /// Get DB parameter
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <param name="dbType">Data type</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="parameterValue">Parameter value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the database parameter</returns>
        private static async Task<DbParameter> GetParameterAsync(this IDataProvider dataProvider, DbType dbType, string parameterName,
            object parameterValue, CancellationToken cancellationToken)
        {
            var parameter = await dataProvider.GetParameterAsync(cancellationToken);

            parameter.ParameterName = parameterName;
            parameter.Value = parameterValue;
            parameter.DbType = dbType;

            return parameter;
        }

        /// <summary>
        /// Get output DB parameter
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <param name="dbType">Data type</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the database parameter</returns>
        private static async Task<DbParameter> GetOutputParameterAsync(this IDataProvider dataProvider, DbType dbType, string parameterName,
            CancellationToken cancellationToken)
        {
            var parameter = await dataProvider.GetParameterAsync(cancellationToken);

            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;
            parameter.Direction = ParameterDirection.Output;

            return parameter;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get string parameter
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="parameterValue">Parameter value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the database parameter</returns>
        public static async Task<DbParameter> GetStringParameterAsync(this IDataProvider dataProvider, string parameterName, string parameterValue,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dataProvider.GetParameterAsync(DbType.String, parameterName, (object)parameterValue ?? DBNull.Value, cancellationToken);
        }

        /// <summary>
        /// Get output string parameter
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the database parameter</returns>
        public static async Task<DbParameter> GetOutputStringParameterAsync(this IDataProvider dataProvider, string parameterName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dataProvider.GetOutputParameterAsync(DbType.String, parameterName, cancellationToken);
        }

        /// <summary>
        /// Get int parameter
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="parameterValue">Parameter value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the database parameter</returns>
        public static async Task<DbParameter> GetInt32ParameterAsync(this IDataProvider dataProvider, string parameterName, int? parameterValue,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dataProvider.GetParameterAsync(DbType.Int32, parameterName,
                parameterValue.HasValue ? (object)parameterValue.Value : DBNull.Value, cancellationToken);
        }

        /// <summary>
        /// Get output int32 parameter
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the database parameter</returns>
        public static async Task<DbParameter> GetOutputInt32ParameterAsync(this IDataProvider dataProvider, string parameterName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dataProvider.GetOutputParameterAsync(DbType.Int32, parameterName, cancellationToken);
        }

        /// <summary>
        /// Get boolean parameter
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="parameterValue">Parameter value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the database parameter</returns>
        public static async Task<DbParameter> GetBooleanParameterAsync(this IDataProvider dataProvider, string parameterName, bool? parameterValue,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dataProvider.GetParameterAsync(DbType.Boolean, parameterName,
                parameterValue.HasValue ? (object)parameterValue.Value : DBNull.Value, cancellationToken);
        }

        /// <summary>
        /// Get decimal parameter
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="parameterValue">Parameter value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the database parameter</returns>
        public static async Task<DbParameter> GetDecimalParameterAsync(this IDataProvider dataProvider, string parameterName,
            decimal? parameterValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dataProvider.GetParameterAsync(DbType.Decimal, parameterName,
                parameterValue.HasValue ? (object)parameterValue.Value : DBNull.Value, cancellationToken);
        }

        /// <summary>
        /// Get datetime parameter
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="parameterValue">Parameter value</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the database parameter</returns>
        public static async Task<DbParameter> GetDateTimeParameterAsync(this IDataProvider dataProvider, string parameterName,
            DateTime? parameterValue, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await dataProvider.GetParameterAsync(DbType.DateTime, parameterName,
                parameterValue.HasValue ? (object)parameterValue.Value : DBNull.Value, cancellationToken);
        }

        #endregion
    }
}