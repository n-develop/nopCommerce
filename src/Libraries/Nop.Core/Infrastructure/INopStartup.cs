using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Core.Infrastructure
{
    /// <summary>
    /// Represents object for the configuring services and middleware on application startup
    /// </summary>
    public partial interface INopStartup
    {
        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that services are configured</returns>
        Task ConfigureServicesAsync(IServiceCollection services, IConfiguration configuration,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that request pipeline is configured</returns>
        Task ConfigureAsync(IApplicationBuilder application, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        int Order { get; }
    }
}