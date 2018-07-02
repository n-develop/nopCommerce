using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;
using Nop.Core.Plugins;

namespace Nop.Services.Plugins
{
    /// <summary>
    /// Plugin finder
    /// </summary>
    public interface IPluginFinder
    {
        /// <summary>
        /// Check whether the plugin is available in a certain store
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="storeId">Store identifier to check</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>true - available; false - no</returns>
        Task<bool> AuthenticateStoreAsync(PluginDescriptor pluginDescriptor, int storeId, CancellationToken cancellationToken);

        /// <summary>
        /// Check that plugin is authorized for the specified customer
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="customer">Customer</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>True if authorized; otherwise, false</returns>
        Task<bool> AuthorizedForUserAsync(PluginDescriptor pluginDescriptor, Customer customer, CancellationToken cancellationToken);

        /// <summary>
        /// Gets plugin groups
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Plugins groups</returns>
        Task<IEnumerable<string>> GetPluginGroupsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets plugins
        /// </summary>
        /// <typeparam name="T">The type of plugins to get.</typeparam>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="customer">Load records allowed only to a specified customer; pass null to ignore ACL permissions</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <param name="group">Filter by plugin group; pass null to load all records</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Plugins</returns>
        Task<IEnumerable<T>> GetPluginsAsync<T>(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            Customer customer = null, int storeId = 0, string group = null, CancellationToken cancellationToken = default(CancellationToken)) where T : class, IPlugin;

        /// <summary>
        /// Get plugin descriptors
        /// </summary>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="customer">Load records allowed only to a specified customer; pass null to ignore ACL permissions</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <param name="group">Filter by plugin group; pass null to load all records</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Plugin descriptors</returns>
        Task<IEnumerable<PluginDescriptor>> GetPluginDescriptorsAsync(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            Customer customer = null, int storeId = 0, string group = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get plugin descriptors
        /// </summary>
        /// <typeparam name="T">The type of plugin to get.</typeparam>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="customer">Load records allowed only to a specified customer; pass null to ignore ACL permissions</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <param name="group">Filter by plugin group; pass null to load all records</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Plugin descriptors</returns>
        Task<IEnumerable<PluginDescriptor>> GetPluginDescriptorsAsync<T>(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            Customer customer = null, int storeId = 0, string group = null, CancellationToken cancellationToken = default(CancellationToken)) where T : class, IPlugin;

        /// <summary>
        /// Get a plugin descriptor by its system name
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>>Plugin descriptor</returns>
        Task<PluginDescriptor> GetPluginDescriptorBySystemNameAsync(string systemName, LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get a plugin descriptor by its system name
        /// </summary>
        /// <typeparam name="T">The type of plugin to get.</typeparam>
        /// <param name="systemName">Plugin system name</param>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>>Plugin descriptor</returns>
        Task<PluginDescriptor> GetPluginDescriptorBySystemNameAsync<T>(string systemName, LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly, CancellationToken cancellationToken = default(CancellationToken))
            where T : class, IPlugin;

        /// <summary>
        /// Reload plugins after updating
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <param name="pluginDescriptor">Updated plugin descriptor</param>
        Task ReloadPluginsAsync(PluginDescriptor pluginDescriptor, CancellationToken cancellationToken);
    }
}
