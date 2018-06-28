using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Represents the base plugin
    /// </summary>
    public abstract partial class BasePlugin : IPlugin
    {
        /// <summary>
        /// Gets or sets the plugin descriptor
        /// </summary>
        public virtual PluginDescriptor PluginDescriptor { get; set; }

        /// <summary>
        /// Get a configuration page URL
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the configuration page URL</returns>
        public virtual async Task<string> GetConfigurationPageUrlAsync(CancellationToken cancellationToken)
        {
            return await Task.FromResult<string>(null);
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that plugin is installed</returns>
        public virtual async Task InstallAsync(CancellationToken cancellationToken)
        {
            await PluginManager.MarkPluginAsInstalledAsync(PluginDescriptor.SystemName, cancellationToken);
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that plugin is uninstalled</returns>
        public virtual async Task UninstallAsync(CancellationToken cancellationToken)
        {
            await PluginManager.MarkPluginAsUninstalledAsync(PluginDescriptor.SystemName, cancellationToken);
        }
    }
}