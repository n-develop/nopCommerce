using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Nop.Core.Configuration;

namespace Nop.Core.Infrastructure.DependencyManagement
{
    /// <summary>
    /// Represents a dependency registrar
    /// </summary>
    public partial interface IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Config</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that components are registered</returns>
        Task RegisterAsync(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets order of this dependency registrar implementation
        /// </summary>
        int Order { get; }
    }
}