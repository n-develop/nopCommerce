using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Infrastructure
{
    /// <summary>
    /// Classes implementing this interface provide information about types 
    /// to various services in the Nop engine.
    /// </summary>
    public partial interface ITypeFinder
    {
        /// <summary>
        /// Find classes of type
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="onlyConcreteClasses">A value indicating whether to find only concrete classes</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains classes of passed type</returns>
        Task<IEnumerable<Type>> FindClassesOfTypeAsync<T>(bool onlyConcreteClasses = true,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Find classes of type
        /// </summary>
        /// <param name="assignTypeFrom">Assign type from</param>
        /// <param name="onlyConcreteClasses">A value indicating whether to find only concrete classes</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains classes of passed type</returns>
        Task<IEnumerable<Type>> FindClassesOfTypeAsync(Type assignTypeFrom, bool onlyConcreteClasses = true,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Find classes of type
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="assemblies">Assemblies</param>
        /// <param name="onlyConcreteClasses">A value indicating whether to find only concrete classes</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains classes of passed type</returns>
        Task<IEnumerable<Type>> FindClassesOfTypeAsync<T>(IEnumerable<Assembly> assemblies, bool onlyConcreteClasses = true,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Find classes of type
        /// </summary>
        /// <param name="assignTypeFrom">Assign type from</param>
        /// <param name="assemblies">Assemblies</param>
        /// <param name="onlyConcreteClasses">A value indicating whether to find only concrete classes</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains classes of passed type</returns>
        Task<IEnumerable<Type>> FindClassesOfTypeAsync(Type assignTypeFrom, IEnumerable<Assembly> assemblies,
            bool onlyConcreteClasses = true, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the assemblies related to the current implementation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains a list of assemblies</returns>
        Task<IList<Assembly>> GetAssembliesAsync(CancellationToken cancellationToken);
    }
}