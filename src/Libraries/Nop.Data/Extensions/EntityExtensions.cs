using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Data.Extensions
{
    /// <summary>
    /// Represents entity extensions
    /// </summary>
    public static partial class EntityExtensions
    {
        /// <summary>
        /// Get unproxied entity type
        /// </summary>
        /// <remarks> If your Entity Framework context is proxy-enabled, 
        /// the runtime will create a proxy instance of your entities, 
        /// i.e. a dynamically generated class which inherits from your entity class 
        /// and overrides its virtual properties by inserting specific code useful for example 
        /// for tracking changes and lazy loading.
        /// </remarks>
        /// <param name="entity">Entity</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the unproxied entity type</returns>
        public static async Task<Type> GetUnproxiedEntityTypeAsync(this BaseEntity entity,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var dbContext = await EngineContext.Current.ResolveAllAsync<IDbContext>(cancellationToken) as DbContext;

            return dbContext?.Model.FindRuntimeEntityType(entity.GetType()).ClrType
                ?? throw new Exception("Original entity type cannot be loaded");
        }
    }
}