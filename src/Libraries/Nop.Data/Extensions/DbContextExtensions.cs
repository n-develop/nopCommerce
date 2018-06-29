using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Data.Extensions
{
    /// <summary>
    /// Represents database context extensions
    /// </summary>
    public static class DbContextExtensions
    {
        #region Fields

        private static string databaseName;
        private static readonly ConcurrentDictionary<string, string> tableNames = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, IEnumerable<(string, int?)>> columnsMaxLength = new ConcurrentDictionary<string, IEnumerable<(string, int?)>>();
        private static readonly ConcurrentDictionary<string, IEnumerable<(string, decimal?)>> decimalColumnsMaxValue = new ConcurrentDictionary<string, IEnumerable<(string, decimal?)>>();

        #endregion

        #region Utilities

        /// <summary>
        /// Loads a copy of the entity using the passed function
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">Database context</param>
        /// <param name="entity">Entity</param>
        /// <param name="getValuesFunction">Function to get the values of the tracked entity</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains a copy of the passed entity</returns>
        private static async Task<TEntity> LoadEntityCopyAsync<TEntity>(IDbContext context, TEntity entity,
            Func<EntityEntry<TEntity>, Task<PropertyValues>> getValuesFunction, CancellationToken cancellationToken) where TEntity : BaseEntity
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            //try to get the EF database context
            if (!(context is DbContext dbContext))
                throw new InvalidOperationException("Context does not support operation");

            //try to get the entity tracking object
            var entityEntry = dbContext.ChangeTracker.Entries<TEntity>().FirstOrDefault(entry => entry.Entity == entity);
            if (entityEntry == null)
                return null;

            //get a copy of the entity
            var propertyValues = await getValuesFunction(entityEntry);
            var entityCopy = propertyValues?.ToObject() as TEntity;

            return entityCopy;
        }

        /// <summary>
        /// Get SQL commands from the script
        /// </summary>
        /// <param name="sql">SQL script</param>
        /// <returns>List of commands</returns>
        private static IList<string> GetCommandsFromScript(string sql)
        {
            var commands = new List<string>();

            //origin from the Microsoft.EntityFrameworkCore.Migrations.SqlServerMigrationsSqlGenerator.Generate method
            sql = Regex.Replace(sql, @"\\\r?\n", string.Empty);
            var batches = Regex.Split(sql, @"^\s*(GO[ \t]+[0-9]+|GO)(?:\s+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            for (var i = 0; i < batches.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(batches[i]) || batches[i].StartsWith("GO", StringComparison.OrdinalIgnoreCase))
                    continue;

                var count = 1;
                if (i != batches.Length - 1 && batches[i + 1].StartsWith("GO", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(batches[i + 1], "([0-9]+)");
                    if (match.Success)
                        count = int.Parse(match.Value);
                }

                var builder = new StringBuilder();
                for (var j = 0; j < count; j++)
                {
                    builder.Append(batches[i]);
                    if (i == batches.Length - 1)
                        builder.AppendLine();
                }

                commands.Add(builder.ToString());
            }

            return commands;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the original copy of the entity
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">Database context</param>
        /// <param name="entity">Entity</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains a copy of the passed entity</returns>
        public static async Task<TEntity> LoadOriginalCopyAsync<TEntity>(this IDbContext context, TEntity entity,
            CancellationToken cancellationToken = default(CancellationToken)) where TEntity : BaseEntity
        {
            return await LoadEntityCopyAsync(context, entity,
                (entityEntry) => Task.Run(() => entityEntry.OriginalValues, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Loads the database copy of the entity
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">Database context</param>
        /// <param name="entity">Entity</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains a copy of the passed entity</returns>
        public static async Task<TEntity> LoadDatabaseCopyAsync<TEntity>(this IDbContext context, TEntity entity,
            CancellationToken cancellationToken = default(CancellationToken)) where TEntity : BaseEntity
        {
            return await LoadEntityCopyAsync(context, entity,
                (entityEntry) => entityEntry.GetDatabaseValuesAsync(cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Drop a plugin table
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="tableName">Table name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that plugin table is dropped</returns>
        public static async Task DropPluginTableAsync(this IDbContext context, string tableName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            //drop the table
            var dbScript = $"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE [{tableName}]";
            await context.ExecuteSqlCommandAsync(dbScript, cancellationToken: cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Get table name of entity
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">Database context</param>
        /// <returns>Table name</returns>
        public static string GetTableName<TEntity>(this IDbContext context) where TEntity : BaseEntity
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            //try to get the EF database context
            if (!(context is DbContext dbContext))
                throw new InvalidOperationException("Context does not support operation");

            var entityTypeFullName = typeof(TEntity).FullName;
            if (!tableNames.ContainsKey(entityTypeFullName))
            {
                //get entity type
                var entityType = dbContext.Model.FindRuntimeEntityType(typeof(TEntity));

                //get the name of the table to which the entity type is mapped
                tableNames.TryAdd(entityTypeFullName, entityType.Relational().TableName);
            }

            tableNames.TryGetValue(entityTypeFullName, out var tableName);

            return tableName;
        }

        /// <summary>
        /// Gets the maximum lengths of data that is allowed for the entity properties
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">Database context</param>
        /// <returns>Collection of name - max length pairs</returns>
        public static IEnumerable<(string Name, int? MaxLength)> GetColumnsMaxLength<TEntity>(this IDbContext context) where TEntity : BaseEntity
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            //try to get the EF database context
            if (!(context is DbContext dbContext))
                throw new InvalidOperationException("Context does not support operation");

            var entityTypeFullName = typeof(TEntity).FullName;
            if (!columnsMaxLength.ContainsKey(entityTypeFullName))
            {
                //get entity type
                var entityType = dbContext.Model.FindEntityType(typeof(TEntity));

                //get property name - max length pairs
                columnsMaxLength.TryAdd(entityTypeFullName,
                    entityType.GetProperties().Select(property => (property.Name, property.GetMaxLength())));
            }

            columnsMaxLength.TryGetValue(entityTypeFullName, out var result);

            return result;
        }

        /// <summary>
        /// Get maximum decimal values
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="context">Database context</param>
        /// <returns>Collection of name - max decimal value pairs</returns>
        public static IEnumerable<(string Name, decimal? MaxValue)> GetDecimalColumnsMaxValue<TEntity>(this IDbContext context)
            where TEntity : BaseEntity
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            //try to get the EF database context
            if (!(context is DbContext dbContext))
                throw new InvalidOperationException("Context does not support operation");

            var entityTypeFullName = typeof(TEntity).FullName;
            if (!decimalColumnsMaxValue.ContainsKey(entityTypeFullName))
            {
                //get entity type
                var entityType = dbContext.Model.FindEntityType(typeof(TEntity));

                //get entity decimal properties
                var properties = entityType.GetProperties().Where(property => property.ClrType == typeof(decimal));

                //return property name - max decimal value pairs
                decimalColumnsMaxValue.TryAdd(entityTypeFullName, properties.Select(property =>
                {
                    var mapping = new RelationalTypeMappingInfo(property);
                    if (!mapping.Precision.HasValue || !mapping.Scale.HasValue)
                        return (property.Name, null);

                    return (property.Name, new decimal?((decimal)Math.Pow(10, mapping.Precision.Value - mapping.Scale.Value)));
                }));
            }

            decimalColumnsMaxValue.TryGetValue(entityTypeFullName, out var result);

            return result;
        }

        /// <summary>
        /// Get database name
        /// </summary>
        /// <param name="context">Database context</param>
        /// <returns>Database name</returns>
        public static string DbName(this IDbContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            //try to get the EF database context
            if (!(context is DbContext dbContext))
                throw new InvalidOperationException("Context does not support operation");

            if (!string.IsNullOrEmpty(databaseName))
                return databaseName;

            //get database connection
            var dbConnection = dbContext.Database.GetDbConnection();

            //return the database name
            databaseName = dbConnection.Database;

            return databaseName;
        }

        /// <summary>
        /// Execute commands from the SQL script against the context database
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="sql">SQL script</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that SQL script is executed</returns>
        public static async Task ExecuteSqlScriptAsync(this IDbContext context, string sql,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var sqlCommands = GetCommandsFromScript(sql);
            foreach (var command in sqlCommands)
                await context.ExecuteSqlCommandAsync(command, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Execute commands from a file with SQL script against the context database
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="filePath">Path to the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that SQL script is executed</returns>
        public static async Task ExecuteSqlScriptFromFileAsync(this IDbContext context, string filePath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var fileProvider = await EngineContext.Current.ResolveAsync<INopFileProvider>(cancellationToken);
            if (!(await fileProvider.FileExistsAsync(filePath, cancellationToken)))
                return;

            await context.ExecuteSqlScriptAsync(await fileProvider.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken), cancellationToken);
        }

        #endregion
    }
}