using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Interface denoting plug-in attributes that are displayed throughout 
    /// the editing interface.
    /// </summary>
    public partial interface IPlugin
    {
        /// <summary>
        /// Gets or sets the plugin descriptor
        /// </summary>
        PluginDescriptor PluginDescriptor { get; set; }

        /// <summary>
        /// Get a configuration page URL
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the configuration page URL</returns>
        Task<string> GetConfigurationPageUrlAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Install plugin
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that plugin is installed</returns>
        Task InstallAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that plugin is uninstalled</returns>
        Task UninstallAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}