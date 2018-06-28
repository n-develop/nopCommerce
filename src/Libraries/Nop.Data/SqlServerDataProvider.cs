using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Infrastructure;
using Nop.Data.Extensions;

namespace Nop.Data
{
    /// <summary>
    /// Represents SQL Server data provider
    /// </summary>
    public partial class SqlServerDataProvider : IDataProvider
    {
        #region Methods

        /// <summary>
        /// Initialize database
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that database is initialized</returns>
        public virtual async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
        {
            var context = await EngineContext.Current.ResolveAsync<IDbContext>(cancellationToken);

            //check some of table names to ensure that we have nopCommerce 2.00+ installed
            var tableNamesToValidate = new List<string> { "Customer", "Discount", "Order", "Product", "ShoppingCartItem" };
            var existingTableNames = (await context
                .QueryFromSqlAsync<StringQueryType>("SELECT table_name AS Value FROM INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE'", cancellationToken))
                .Select(stringValue => stringValue.Value).ToList();
            var createTables = !existingTableNames.Intersect(tableNamesToValidate, StringComparer.InvariantCultureIgnoreCase).Any();
            if (!createTables)
                return;

            var fileProvider = await EngineContext.Current.ResolveAsync<INopFileProvider>(cancellationToken);

            //create tables
            //EngineContext.Current.Resolve<IRelationalDatabaseCreator>().CreateTables();
            //(context as DbContext).Database.EnsureCreated();
            var createScript = await context.GenerateCreateScriptAsync(cancellationToken);
            await context.ExecuteSqlScriptAsync(createScript, cancellationToken);

            //create indexes
            var indexesFilePath = await fileProvider.MapPathAsync(NopDataDefaults.SqlServerIndexesFilePath, cancellationToken);
            await context.ExecuteSqlScriptFromFileAsync(indexesFilePath, cancellationToken);

            //create stored procedures 
            var storedProceduresFilePath = await fileProvider.MapPathAsync(NopDataDefaults.SqlServerStoredProceduresFilePath, cancellationToken);
            await context.ExecuteSqlScriptFromFileAsync(storedProceduresFilePath, cancellationToken);
        }

        /// <summary>
        /// Get a support database parameter object (used by stored procedures)
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains database parameter object</returns>
        public virtual async Task<DbParameter> GetParameterAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() => new SqlParameter(), cancellationToken);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this data provider supports backup
        /// </summary>
        public virtual bool BackupSupported => true;

        /// <summary>
        /// Gets a maximum length of the data for HASHBYTES functions, returns 0 if HASHBYTES function is not supported
        /// </summary>
        public virtual int SupportedLengthOfBinaryHash => 8000; //for SQL Server 2008 and above HASHBYTES function has a limit of 8000 characters.

        #endregion
    }
}