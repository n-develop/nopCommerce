using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;
using Nop.Core.Plugins;
using Nop.Services.Events;

namespace Nop.Services.Plugins
{
    /// <summary>
    /// Plugin finder
    /// </summary>
    public class PluginFinder : IPluginFinder
    {
        #region Fields

        private readonly IEventPublisher _eventPublisher;

        private IList<PluginDescriptor> _plugins;
        private bool _arePluginsLoaded;

        #endregion

        #region Ctor

        public PluginFinder(IEventPublisher eventPublisher)
        {
            this._eventPublisher = eventPublisher;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Ensure plugins are loaded
        /// </summary>
        protected virtual Task EnsurePluginsAreLoadedAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                if (_arePluginsLoaded) 
                    return;

                var foundPlugins = PluginManager.ReferencedPlugins.ToList();
                foundPlugins.Sort();
                _plugins = foundPlugins.ToList();

                _arePluginsLoaded = true;
            }, cancellationToken);
        }

        /// <summary>
        /// Check whether the plugin is available in a certain store
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="loadMode">Load plugins mode</param>
        /// <returns>true - available; false - no</returns>
        protected virtual bool CheckLoadMode(PluginDescriptor pluginDescriptor, LoadPluginsMode loadMode)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            switch (loadMode)
            {
                case LoadPluginsMode.All:
                    //no filtering
                    return true;
                case LoadPluginsMode.InstalledOnly:
                    return pluginDescriptor.Installed;
                case LoadPluginsMode.NotInstalledOnly:
                    return !pluginDescriptor.Installed;
                default:
                    throw new Exception("Not supported LoadPluginsMode");
            }
        }

        /// <summary>
        /// Check whether the plugin is in a certain group
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="group">Group</param>
        /// <returns>true - available; false - no</returns>
        protected virtual bool CheckGroup(PluginDescriptor pluginDescriptor, string group)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            if (string.IsNullOrEmpty(group))
                return true;

            return group.Equals(pluginDescriptor.Group, StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion
        
        #region Methods

        /// <summary>
        /// Check whether the plugin is available in a certain store
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="storeId">Store identifier to check</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>true - available; false - no</returns>
        public virtual async Task<bool> AuthenticateStoreAsync(PluginDescriptor pluginDescriptor, int storeId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() =>
            {
                if (pluginDescriptor == null)
                    throw new ArgumentNullException(nameof(pluginDescriptor));

                //no validation required
                if (storeId == 0)
                    return true;

                if (!pluginDescriptor.LimitedToStores.Any())
                    return true;

                return pluginDescriptor.LimitedToStores.Contains(storeId);
            }, cancellationToken);
        }

        /// <summary>
        /// Check that plugin is authorized for the specified customer
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor to check</param>
        /// <param name="customer">Customer</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>True if authorized; otherwise, false</returns>
        public virtual async Task<bool> AuthorizedForUserAsync(PluginDescriptor pluginDescriptor, Customer customer, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() =>
            {
                if (pluginDescriptor == null)
                    throw new ArgumentNullException(nameof(pluginDescriptor));

                if (customer == null || !pluginDescriptor.LimitedToCustomerRoles.Any())
                    return true;

                var customerRoleIds = customer.CustomerRoles.Where(role => role.Active).Select(role => role.Id);

                return pluginDescriptor.LimitedToCustomerRoles.Intersect(customerRoleIds).Any();
            }, cancellationToken);
        }

        /// <summary>
        /// Gets plugin groups
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Plugins groups</returns>
        public virtual async Task<IEnumerable<string>> GetPluginGroupsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return (await GetPluginDescriptorsAsync(LoadPluginsMode.All, cancellationToken: cancellationToken)).Select(x => x.Group).Distinct().OrderBy(x => x);
        }

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
        public virtual async Task<IEnumerable<T>> GetPluginsAsync<T>(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            Customer customer = null, int storeId = 0, string group = null, CancellationToken cancellationToken = default(CancellationToken)) where T : class, IPlugin
        {
            var descriptors = await GetPluginDescriptorsAsync<T>(loadMode, customer, storeId, group, cancellationToken);

            return await Task.Run(()=>descriptors.Select(p => p.InstanceAsync<T>(cancellationToken).Result), cancellationToken);
        }

        /// <summary>
        /// Get plugin descriptors
        /// </summary>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="customer">Load records allowed only to a specified customer; pass null to ignore ACL permissions</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <param name="group">Filter by plugin group; pass null to load all records</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Plugin descriptors</returns>
        public virtual async Task<IEnumerable<PluginDescriptor>> GetPluginDescriptorsAsync(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            Customer customer = null, int storeId = 0, string group = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            //ensure plugins are loaded
            await EnsurePluginsAreLoadedAsync(cancellationToken);

            return await Task.Run(
                () => _plugins.Where(p =>
                    CheckLoadMode(p, loadMode) && AuthorizedForUserAsync(p, customer, cancellationToken).Result &&
                    AuthenticateStoreAsync(p, storeId, cancellationToken).Result && CheckGroup(p, group)),
                cancellationToken);
        }


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
        public virtual async Task<IEnumerable<PluginDescriptor>> GetPluginDescriptorsAsync<T>(LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly,
            Customer customer = null, int storeId = 0, string group = null, CancellationToken cancellationToken = default(CancellationToken)) 
            where T : class, IPlugin
        {
            return (await GetPluginDescriptorsAsync(loadMode, customer, storeId, group, cancellationToken))
                .Where(p => typeof(T).IsAssignableFrom(p.PluginType));
        }

        /// <summary>
        /// Get a plugin descriptor by its system name
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>>Plugin descriptor</returns>
        public virtual async Task<PluginDescriptor> GetPluginDescriptorBySystemNameAsync(string systemName, LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (await GetPluginDescriptorsAsync(loadMode, cancellationToken: cancellationToken))
                .SingleOrDefault(p => p.SystemName.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Get a plugin descriptor by its system name
        /// </summary>
        /// <typeparam name="T">The type of plugin to get.</typeparam>
        /// <param name="systemName">Plugin system name</param>
        /// <param name="loadMode">Load plugins mode</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>>Plugin descriptor</returns>
        public virtual async Task<PluginDescriptor> GetPluginDescriptorBySystemNameAsync<T>(string systemName, LoadPluginsMode loadMode = LoadPluginsMode.InstalledOnly, CancellationToken cancellationToken = default(CancellationToken))
            where T : class, IPlugin
        {
            return (await GetPluginDescriptorsAsync<T>(loadMode, cancellationToken: cancellationToken))
                .SingleOrDefault(p => p.SystemName.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Reload plugins after updating
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <param name="pluginDescriptor">Updated plugin descriptor</param>
        public virtual async Task ReloadPluginsAsync(PluginDescriptor pluginDescriptor, CancellationToken cancellationToken = default(CancellationToken))
        {
            _arePluginsLoaded = false;
            await EnsurePluginsAreLoadedAsync(cancellationToken);

            //raise event
            await _eventPublisher.PublishEventAsync(new PluginUpdatedEvent(pluginDescriptor), cancellationToken);
        }

        #endregion
    }
}
