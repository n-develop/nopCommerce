using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data.Mapping;

namespace Nop.Data
{
    /// <summary>
    /// Represents base object context
    /// </summary>
    public partial class NopObjectContext : DbContext, IDbContext
    {
        #region Ctor

        public NopObjectContext(DbContextOptions<NopObjectContext> options) : base(options)
        {
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Further configuration the model
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //dynamically load all entity and query type configurations
            var typeConfigurations = Assembly.GetExecutingAssembly().GetTypes().Where(type =>
                (type.BaseType?.IsGenericType ?? false)
                    && (type.BaseType.GetGenericTypeDefinition() == typeof(NopEntityTypeConfiguration<>)
                        || type.BaseType.GetGenericTypeDefinition() == typeof(NopQueryTypeConfiguration<>)));

            foreach (var typeConfiguration in typeConfigurations)
            {
                var configuration = (IMappingConfiguration)Activator.CreateInstance(typeConfiguration);
                configuration.ApplyConfiguration(modelBuilder);
            }

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Modify the input SQL query by adding passed parameters
        /// </summary>
        /// <param name="sql">The raw SQL query</param>
        /// <param name="parameters">The values to be assigned to parameters</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the modified raw SQL query</returns>
        protected virtual async Task<string> CreateSqlWithParametersAsync(string sql, IEnumerable<object> parameters,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                //add parameters to sql
                var parametersArray = parameters?.ToArray();
                for (var i = 0; i <= (parametersArray?.Length ?? 0) - 1; i++)
                {
                    if (!(parametersArray[i] is DbParameter parameter))
                        continue;

                    sql = $"{sql}{(i > 0 ? "," : string.Empty)} @{parameter.ParameterName}";

                    //whether parameter is output
                    if (parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Output)
                        sql = $"{sql} output";
                }

                return sql;
            }, cancellationToken);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a DbSet that can be used to query and save instances of entity
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the set for the given entity type</returns>
        public virtual async Task<DbSet<TEntity>> GetDbSetAsync<TEntity>(CancellationToken cancellationToken) where TEntity : BaseEntity
        {
            return await Task.Run(() => base.Set<TEntity>(), cancellationToken);
        }

        /// <summary>
        /// Generate a script to create all tables for the current model
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the SQL script</returns>
        public virtual async Task<string> GenerateCreateScriptAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() => this.Database.GenerateCreateScript(), cancellationToken);
        }

        /// <summary>
        /// Creates a LINQ query for the query type based on a raw SQL query
        /// </summary>
        /// <typeparam name="TQuery">Query type</typeparam>
        /// <param name="sql">The raw SQL query</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains an IQueryable representing the raw SQL query</returns>
        public virtual async Task<IQueryable<TQuery>> QueryFromSqlAsync<TQuery>(string sql, CancellationToken cancellationToken)
            where TQuery : class
        {
            return await Task.Run(() => this.Query<TQuery>().FromSql(sql), cancellationToken);
        }

        /// <summary>
        /// Creates a LINQ query for the entity based on a raw SQL query
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="sql">The raw SQL query</param>
        /// <param name="parameters">The values to be assigned to parameters</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains an IQueryable representing the raw SQL query</returns>
        public virtual async Task<IQueryable<TEntity>> EntityFromSqlAsync<TEntity>(string sql, IEnumerable<object> parameters,
            CancellationToken cancellationToken) where TEntity : BaseEntity
        {
            var sqlString = await CreateSqlWithParametersAsync(sql, parameters, cancellationToken);
            return (await GetDbSetAsync<TEntity>(cancellationToken)).FromSql(sqlString, parameters);
        }

        /// <summary>
        /// Executes the given SQL against the database
        /// </summary>
        /// <param name="sql">The SQL to execute</param>
        /// <param name="doNotEnsureTransaction">true - the transaction creation is not ensured; false - the transaction creation is ensured.</param>
        /// <param name="timeout">The timeout to use for command. Note that the command timeout is distinct from the connection timeout, which is commonly set on the database connection string</param>
        /// <param name="parameters">Parameters to use with the SQL</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the number of rows affected</returns>
        public virtual async Task<int> ExecuteSqlCommandAsync(RawSqlString sql, bool doNotEnsureTransaction = false, int? timeout = null,
            IEnumerable<object> parameters = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            //set specific command timeout
            var previousTimeout = this.Database.GetCommandTimeout();
            this.Database.SetCommandTimeout(timeout);

            var result = 0;
            parameters = parameters ?? new List<object>();
            if (!doNotEnsureTransaction)
            {
                //use with transaction
                using (var transaction = await this.Database.BeginTransactionAsync(cancellationToken))
                {
                    result = await this.Database.ExecuteSqlCommandAsync(sql, parameters, cancellationToken);
                    transaction.Commit();
                }
            }
            else
                result = await this.Database.ExecuteSqlCommandAsync(sql, parameters, cancellationToken);

            //return previous timeout back
            this.Database.SetCommandTimeout(previousTimeout);

            return result;
        }

        /// <summary>
        /// Detach an entity from the context
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that the passed entity is detached</returns>
        public virtual async Task DetachAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : BaseEntity
        {
            await Task.Run(() =>
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var entityEntry = this.Entry(entity);
                if (entityEntry == null)
                    return;

                //set the entity is not being tracked by the context
                entityEntry.State = EntityState.Detached;
            }, cancellationToken);
        }

        #endregion
    }
}