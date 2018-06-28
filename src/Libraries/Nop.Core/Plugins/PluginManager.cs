using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Newtonsoft.Json;
using Nop.Core.ComponentModel;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;

//Contributor: Umbraco (http://www.umbraco.com). Thanks a lot! 

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Represents a plugin manager
    /// </summary>
    public partial class PluginManager
    {
        #region Fields

        private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim();
        private static readonly List<string> _baseAppLibraries;
        private static readonly INopFileProvider _fileProvider;
        private static string _shadowCopyFolder;
        private static string _reserveShadowCopyFolder;

        #endregion

        #region Ctor

        static PluginManager()
        {
            //we use the default file provider, since the DI isn't initialized yet
            _fileProvider = CommonHelper.DefaultFileProvider;

            Action<string> addLibraries = async (directoryName) =>
            {
                var dllFiles = await _fileProvider.GetFilesAsync(directoryName, "*.dll");
                var libraryNames = await Task.WhenAll(dllFiles.Select(fi => _fileProvider.GetFileNameAsync(fi)));
                _baseAppLibraries.AddRange(libraryNames);
            };

            //get all libraries from /bin/{version}/ directory
            _baseAppLibraries = new List<string>();
            addLibraries(AppDomain.CurrentDomain.BaseDirectory);

            //get all libraries from base site directory
            if (!AppDomain.CurrentDomain.BaseDirectory.Equals(Environment.CurrentDirectory, StringComparison.InvariantCultureIgnoreCase))
                addLibraries(Environment.CurrentDirectory);

            //get all libraries from refs directory
            var refsPathName = _fileProvider.CombineAsync(new[] { Environment.CurrentDirectory, NopPluginDefaults.RefsPathName }).Result;
            if (_fileProvider.DirectoryExistsAsync(refsPathName).Result)
                addLibraries(refsPathName);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get description files
        /// </summary>
        /// <param name="pluginFolder">Plugin directory info</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains list of plugin description files</returns>
        private static async Task<IEnumerable<(string Name, PluginDescriptor Descriptor)>> GetDescriptionFilesAndDescriptorsAsync(
            string pluginFolder, CancellationToken cancellationToken)
        {
            if (pluginFolder == null)
                throw new ArgumentNullException(nameof(pluginFolder));

            //create list (<file info, parsed plugin descritor>)
            var result = new List<(string Name, PluginDescriptor Descriptor)>();

            //add display order and path to list
            var pluginFiles = await _fileProvider.GetFilesAsync(pluginFolder, NopPluginDefaults.DescriptionFileName, false, cancellationToken);
            foreach (var descriptionFile in pluginFiles)
            {
                var directoryName = await _fileProvider.GetDirectoryNameAsync(descriptionFile, cancellationToken);
                if (!(await IsPackagePluginFolderAsync(directoryName, cancellationToken)))
                    continue;

                //parse file
                var pluginDescriptor = await GetPluginDescriptorFromFileAsync(descriptionFile, cancellationToken);

                //populate list
                result.Add((descriptionFile, pluginDescriptor));
            }

            //sort list by display order. NOTE: Lowest DisplayOrder will be first i.e 0 , 1, 1, 1, 5, 10
            //it's required: https://www.nopcommerce.com/boards/t/17455/load-plugins-based-on-their-displayorder-on-startup.aspx
            result.Sort((firstPair, nextPair) => firstPair.Descriptor.DisplayOrder.CompareTo(nextPair.Descriptor.DisplayOrder));

            return result;
        }

        /// <summary>
        /// Get system names of installed plugins
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains list of plugin system names</returns>
        private static async Task<IList<string>> GetInstalledPluginNamesAsync(string filePath, CancellationToken cancellationToken)
        {
            //check whether file exists
            if (!(await _fileProvider.FileExistsAsync(filePath, cancellationToken)))
            {
                //if not, try to parse the file that was used in previous nopCommerce versions
                filePath = await _fileProvider.MapPathAsync(NopPluginDefaults.ObsoleteInstalledPluginsFilePath, cancellationToken);
                if (!(await _fileProvider.FileExistsAsync(filePath, cancellationToken)))
                    return new List<string>();

                //get plugin system names from the old txt file
                var pluginSystemNames = new List<string>();
                using (var reader = new StringReader(await _fileProvider.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken)))
                {
                    string pluginName;
                    while ((pluginName = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(pluginName))
                            pluginSystemNames.Add(pluginName.Trim());
                    }
                }

                //save system names of installed plugins to the new file
                var pluginsFilePath = await _fileProvider.MapPathAsync(NopPluginDefaults.InstalledPluginsFilePath, cancellationToken);
                await SaveInstalledPluginNamesAsync(pluginSystemNames, pluginsFilePath, cancellationToken);

                //and delete the old one
                await _fileProvider.DeleteFileAsync(filePath, cancellationToken);

                return pluginSystemNames;
            }

            var text = await _fileProvider.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            //get plugin system names from the JSON file
            return JsonConvert.DeserializeObject<IList<string>>(text);
        }

        /// <summary>
        /// Save system names of installed plugins to the file
        /// </summary>
        /// <param name="pluginSystemNames">List of plugin system names</param>
        /// <param name="filePath">Path to the file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that installed plugin names are saved to the file</returns>
        private static async Task SaveInstalledPluginNamesAsync(IList<string> pluginSystemNames, string filePath,
            CancellationToken cancellationToken)
        {
            //save the file
            var text = JsonConvert.SerializeObject(pluginSystemNames, Formatting.Indented);
            await _fileProvider.WriteAllTextAsync(filePath, text, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Indicates whether assembly file is already loaded
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether assembly file is loaded</returns>
        private static async Task<bool> IsAlreadyLoadedAsync(string filePath, CancellationToken cancellationToken)
        {
            //search library file name in base directory to ignore already existing (loaded) libraries
            //(we do it because not all libraries are loaded immediately after application start)
            var fileName = await _fileProvider.GetFileNameAsync(filePath, cancellationToken);
            if (_baseAppLibraries.Any(sli => sli.Equals(fileName, StringComparison.InvariantCultureIgnoreCase)))
                return true;

            //compare full assembly name
            //var fileAssemblyName = AssemblyName.GetAssemblyName(filePath);
            //foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            //{
            //    if (a.FullName.Equals(fileAssemblyName.FullName, StringComparison.InvariantCultureIgnoreCase))
            //        return true;
            //}
            //return false;

            //do not compare the full assembly name, just filename
            try
            {
                var fileNameWithoutExt = await _fileProvider.GetFileNameWithoutExtensionAsync(filePath, cancellationToken);
                if (string.IsNullOrEmpty(fileNameWithoutExt))
                    throw new Exception($"Cannot get file extension for {(await _fileProvider.GetFileNameAsync(filePath, cancellationToken))}");

                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var assemblyName = a.FullName.Split(',').FirstOrDefault();
                    if (fileNameWithoutExt.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Cannot validate whether an assembly is already loaded. " + exc);
            }

            return false;
        }

        /// <summary>
        /// Perform file deploy
        /// </summary>
        /// <param name="plug">Plugin file info</param>
        /// <param name="applicationPartManager">Application part manager</param>
        /// <param name="config">Config</param>
        /// <param name="shadowCopyPath">Shadow copy path</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the assembly</returns>
        private static async Task<Assembly> PerformFileDeployAsync(string plug, ApplicationPartManager applicationPartManager, NopConfig config,
            string shadowCopyPath = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            var parent = string.IsNullOrEmpty(plug) ? string.Empty : await _fileProvider.GetParentDirectoryAsync(plug, cancellationToken);

            if (string.IsNullOrEmpty(parent))
                throw new InvalidOperationException($"The plugin directory for the {(await _fileProvider.GetFileNameAsync(plug, cancellationToken))} file exists in a folder outside of the allowed nopCommerce folder hierarchy");

            if (!config.UsePluginsShadowCopy)
                return RegisterPluginDefinition(config, applicationPartManager, plug);

            //in order to avoid possible issues we still copy libraries into ~/Plugins/bin/ directory
            if (string.IsNullOrEmpty(shadowCopyPath))
                shadowCopyPath = _shadowCopyFolder;

            await _fileProvider.CreateDirectoryAsync(shadowCopyPath, cancellationToken);
            var shadowCopiedPlug = await ShadowCopyFileAsync(plug, shadowCopyPath, cancellationToken);

            Assembly shadowCopiedAssembly = null;

            try
            {
                shadowCopiedAssembly = RegisterPluginDefinition(config, applicationPartManager, shadowCopiedPlug);
            }
            catch (FileLoadException)
            {
                if (!config.CopyLockedPluginAssembilesToSubdirectoriesOnStartup || !shadowCopyPath.Equals(_shadowCopyFolder))
                    throw;
            }

            return shadowCopiedAssembly ?? await PerformFileDeployAsync(plug, applicationPartManager, config, _reserveShadowCopyFolder, cancellationToken);
        }

        /// <summary>
        /// Register the plugin definition
        /// </summary>
        /// <param name="config">Config</param>
        /// <param name="applicationPartManager">Application part manager</param>
        /// <param name="plug">Plugin file info</param>
        /// <returns>Assembly</returns>
        private static Assembly RegisterPluginDefinition(NopConfig config, ApplicationPartManager applicationPartManager, string plug)
        {
            //we can now register the plugin definition
            Assembly pluginAssembly;
            try
            {
                pluginAssembly = Assembly.LoadFrom(plug);
            }
            catch (FileLoadException)
            {
                if (config.UseUnsafeLoadAssembly)
                {
                    //if an application has been copied from the web, it is flagged by Windows as being a web application,
                    //even if it resides on the local computer.You can change that designation by changing the file properties,
                    //or you can use the<loadFromRemoteSources> element to grant the assembly full trust.As an alternative,
                    //you can use the UnsafeLoadFrom method to load a local assembly that the operating system has flagged as
                    //having been loaded from the web.
                    //see http://go.microsoft.com/fwlink/?LinkId=155569 for more information.
                    pluginAssembly = Assembly.UnsafeLoadFrom(plug);
                }
                else
                {
                    throw;
                }
            }

            Debug.WriteLine("Adding to ApplicationParts: '{0}'", pluginAssembly.FullName);
            applicationPartManager.ApplicationParts.Add(new AssemblyPart(pluginAssembly));

            return pluginAssembly;
        }

        /// <summary>
        /// Copy the plugin file to shadow copy directory
        /// </summary>
        /// <param name="pluginFilePath">Plugin file path</param>
        /// <param name="shadowCopyPlugFolder">Path to shadow copy folder</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the path to shadow copy of plugin file</returns>
        private static async Task<string> ShadowCopyFileAsync(string pluginFilePath, string shadowCopyPlugFolder,
            CancellationToken cancellationToken)
        {
            var shouldCopy = true;
            var fileName = await _fileProvider.GetFileNameAsync(pluginFilePath, cancellationToken);
            var shadowCopiedPlug = await _fileProvider.CombineAsync(new[] { shadowCopyPlugFolder, fileName }, cancellationToken);

            //check if a shadow copied file already exists and if it does, check if it's updated, if not don't copy
            if (await _fileProvider.FileExistsAsync(shadowCopiedPlug, cancellationToken))
            {
                //it's better to use LastWriteTimeUTC, but not all file systems have this property
                //maybe it is better to compare file hash?
                var shadowCopiedTime = await _fileProvider.GetCreationTimeAsync(shadowCopiedPlug, cancellationToken);
                var pluginTime = await _fileProvider.GetCreationTimeAsync(pluginFilePath, cancellationToken);
                var areFilesIdentical = shadowCopiedTime.ToUniversalTime().Ticks >= pluginTime.ToUniversalTime().Ticks;
                if (areFilesIdentical)
                {
                    Debug.WriteLine("Not copying; files appear identical: '{0}'", await _fileProvider.GetFileNameAsync(shadowCopiedPlug, cancellationToken));
                    shouldCopy = false;
                }
                else
                {
                    //delete an existing file

                    //More info: https://www.nopcommerce.com/boards/t/11511/access-error-nopplugindiscountrulesbillingcountrydll.aspx?p=4#60838
                    Debug.WriteLine("New plugin found; Deleting the old file: '{0}'", await _fileProvider.GetFileNameAsync(shadowCopiedPlug, cancellationToken));
                    await _fileProvider.DeleteFileAsync(shadowCopiedPlug, cancellationToken);
                }
            }

            if (!shouldCopy)
                return shadowCopiedPlug;

            try
            {
                await _fileProvider.FileCopyAsync(pluginFilePath, shadowCopiedPlug, true, cancellationToken);
            }
            catch (IOException)
            {
                Debug.WriteLine(shadowCopiedPlug + " is locked, attempting to rename");
                //this occurs when the files are locked,
                //for some reason devenv locks plugin files some times and for another crazy reason you are allowed to rename them
                //which releases the lock, so that it what we are doing here, once it's renamed, we can re-shadow copy
                try
                {
                    var oldFile = shadowCopiedPlug + Guid.NewGuid().ToString("N") + ".old";
                    await _fileProvider.FileMoveAsync(shadowCopiedPlug, oldFile, cancellationToken);
                }
                catch (IOException exc)
                {
                    throw new IOException(shadowCopiedPlug + " rename failed, cannot initialize plugin", exc);
                }
                //OK, we've made it this far, now retry the shadow copy
                await _fileProvider.FileCopyAsync(pluginFilePath, shadowCopiedPlug, true, cancellationToken);
            }

            return shadowCopiedPlug;
        }

        /// <summary>
        /// Determines if the folder is a bin plugin folder for a package
        /// </summary>
        /// <param name="folder">Folder</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether the passed folder is a bin plugin folder</returns>
        private static async Task<bool> IsPackagePluginFolderAsync(string folder, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(folder))
                return false;

            var parent = await _fileProvider.GetParentDirectoryAsync(folder, cancellationToken);
            if (string.IsNullOrEmpty(parent))
                return false;

            var parentName = await _fileProvider.GetDirectoryNameOnlyAsync(parent, cancellationToken);
            if (!parentName.Equals(NopPluginDefaults.PathName, StringComparison.InvariantCultureIgnoreCase))
                return false;

            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="applicationPartManager">Application part manager</param>
        /// <param name="config">Config</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that plugins are initialized</returns>
        public static async Task InitializeAsync(ApplicationPartManager applicationPartManager, NopConfig config,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (applicationPartManager == null)
                throw new ArgumentNullException(nameof(applicationPartManager));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            using (new WriteLockDisposable(Locker))
            {
                // TODO: Add verbose exception handling / raising here since this is happening on app startup and could
                // prevent app from starting altogether
                var pluginFolder = await _fileProvider.MapPathAsync(NopPluginDefaults.Path, cancellationToken);
                _shadowCopyFolder = await _fileProvider.MapPathAsync(NopPluginDefaults.ShadowCopyPath, cancellationToken);
                _reserveShadowCopyFolder = await _fileProvider.CombineAsync(new[]
                    { _shadowCopyFolder, $"{NopPluginDefaults.ReserveShadowCopyPathName}{DateTime.Now.ToFileTimeUtc()}" }, cancellationToken);

                var referencedPlugins = new List<PluginDescriptor>();
                var incompatiblePlugins = new List<string>();

                try
                {
                    var installedPluginsFilePath = await _fileProvider.MapPathAsync(NopPluginDefaults.InstalledPluginsFilePath, cancellationToken);
                    var installedPluginSystemNames = await GetInstalledPluginNamesAsync(installedPluginsFilePath, cancellationToken);

                    Debug.WriteLine("Creating shadow copy folder and querying for DLLs");
                    //ensure folders are created
                    await _fileProvider.CreateDirectoryAsync(pluginFolder, cancellationToken);
                    await _fileProvider.CreateDirectoryAsync(_shadowCopyFolder, cancellationToken);

                    //get list of all files in bin
                    var binFiles = await _fileProvider.GetFilesAsync(_shadowCopyFolder, "*", false, cancellationToken);
                    if (config.ClearPluginShadowDirectoryOnStartup)
                    {
                        //clear out shadow copied plugins
                        foreach (var f in binFiles)
                        {
                            var fileName = await _fileProvider.GetFileNameAsync(f, cancellationToken);
                            if (fileName.Equals("placeholder.txt", StringComparison.InvariantCultureIgnoreCase))
                                continue;

                            if (fileName.Equals("index.htm", StringComparison.InvariantCultureIgnoreCase))
                                continue;

                            Debug.WriteLine("Deleting " + f);
                            try
                            {
                                await _fileProvider.DeleteFileAsync(f, cancellationToken);
                            }
                            catch (Exception exc)
                            {
                                Debug.WriteLine("Error deleting file " + f + ". Exception: " + exc);
                            }
                        }

                        //delete all reserve folders
                        var directories = await _fileProvider.GetDirectoriesAsync(_shadowCopyFolder,
                            NopPluginDefaults.ReserveShadowCopyPathNamePattern, cancellationToken: cancellationToken);
                        foreach (var directory in directories)
                        {
                            try
                            {
                                await _fileProvider.DeleteDirectoryAsync(directory, cancellationToken);
                            }
                            catch
                            {
                                //do nothing
                            }
                        }
                    }

                    //load description files
                    foreach (var (descriptionFile, pluginDescriptor) in await GetDescriptionFilesAndDescriptorsAsync(pluginFolder, cancellationToken))
                    {
                        //ensure that version of plugin is valid
                        if (!pluginDescriptor.SupportedVersions.Contains(NopVersion.CurrentVersion, StringComparer.InvariantCultureIgnoreCase))
                        {
                            incompatiblePlugins.Add(pluginDescriptor.SystemName);
                            continue;
                        }

                        //some validation
                        if (string.IsNullOrWhiteSpace(pluginDescriptor.SystemName))
                            throw new Exception($"A plugin '{descriptionFile}' has no system name. Try assigning the plugin a unique name and recompiling.");
                        if (referencedPlugins.Contains(pluginDescriptor))
                            throw new Exception($"A plugin with '{pluginDescriptor.SystemName}' system name is already defined");

                        //set 'Installed' property
                        pluginDescriptor.Installed = installedPluginSystemNames
                            .FirstOrDefault(x => x.Equals(pluginDescriptor.SystemName, StringComparison.InvariantCultureIgnoreCase)) != null;

                        try
                        {
                            var directoryName = await _fileProvider.GetDirectoryNameAsync(descriptionFile, cancellationToken);
                            if (string.IsNullOrEmpty(directoryName))
                                throw new Exception($"Directory cannot be resolved for '{(await _fileProvider.GetFileNameAsync(descriptionFile, cancellationToken))}' description file");

                            //get list of all DLLs in plugins (not in bin!)
                            var pluginFiles = (await _fileProvider.GetFilesAsync(directoryName, "*.dll", false, cancellationToken))
                                //just make sure we're not registering shadow copied plugins
                                .Where(x => !binFiles.Select(q => q).Contains(x))
                                .Where(x => IsPackagePluginFolderAsync(_fileProvider.GetDirectoryNameAsync(x, cancellationToken).Result, cancellationToken).Result)
                                .ToList();

                            //other plugin description info
                            var mainPluginFile = pluginFiles.FirstOrDefault(x =>
                            {
                                var name = _fileProvider.GetFileNameAsync(x, cancellationToken).Result;
                                return name.Equals(pluginDescriptor.AssemblyFileName, StringComparison.InvariantCultureIgnoreCase);
                            });

                            //plugin have wrong directory
                            if (mainPluginFile == null)
                            {
                                incompatiblePlugins.Add(pluginDescriptor.SystemName);
                                continue;
                            }

                            pluginDescriptor.OriginalAssemblyFile = mainPluginFile;

                            //shadow copy main plugin file
                            pluginDescriptor.ReferencedAssembly = await PerformFileDeployAsync(mainPluginFile, applicationPartManager,
                                config, cancellationToken: cancellationToken);

                            //load all other referenced assemblies now
                            foreach (var plugin in pluginFiles
                                .Where(x => !_fileProvider.GetFileNameAsync(x, cancellationToken).Result.Equals(_fileProvider.GetFileNameAsync(mainPluginFile, cancellationToken).Result, StringComparison.InvariantCultureIgnoreCase))
                                .Where(x => !IsAlreadyLoadedAsync(x, cancellationToken).Result))
                            {
                                await PerformFileDeployAsync(plugin, applicationPartManager, config, cancellationToken: cancellationToken);
                            }
                            //init plugin type (only one plugin per assembly is allowed)
                            foreach (var t in pluginDescriptor.ReferencedAssembly.GetTypes())
                                if (typeof(IPlugin).IsAssignableFrom(t))
                                    if (!t.IsInterface)
                                        if (t.IsClass && !t.IsAbstract)
                                        {
                                            pluginDescriptor.PluginType = t;
                                            break;
                                        }

                            referencedPlugins.Add(pluginDescriptor);
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            //add a plugin name. this way we can easily identify a problematic plugin
                            var msg = $"Plugin '{pluginDescriptor.FriendlyName}'. ";
                            foreach (var e in ex.LoaderExceptions)
                                msg += e.Message + Environment.NewLine;

                            var fail = new Exception(msg, ex);
                            throw fail;
                        }
                        catch (Exception ex)
                        {
                            //add a plugin name. this way we can easily identify a problematic plugin
                            var msg = $"Plugin '{pluginDescriptor.FriendlyName}'. {ex.Message}";

                            var fail = new Exception(msg, ex);
                            throw fail;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var msg = string.Empty;
                    for (var e = ex; e != null; e = e.InnerException)
                        msg += e.Message + Environment.NewLine;

                    var fail = new Exception(msg, ex);
                    throw fail;
                }

                ReferencedPlugins = referencedPlugins;
                IncompatiblePlugins = incompatiblePlugins;
            }
        }

        /// <summary>
        /// Mark plugin as installed
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that plugin is installed</returns>
        public static async Task MarkPluginAsInstalledAsync(string systemName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(systemName))
                throw new ArgumentNullException(nameof(systemName));

            var filePath = await _fileProvider.MapPathAsync(NopPluginDefaults.InstalledPluginsFilePath, cancellationToken);

            //create file if not exists
            await _fileProvider.CreateFileAsync(filePath, cancellationToken);

            //get installed plugin names
            var installedPluginSystemNames = await GetInstalledPluginNamesAsync(filePath, cancellationToken);

            //add plugin system name to the list if doesn't already exist
            var alreadyMarkedAsInstalled = installedPluginSystemNames.Any(pluginName => pluginName.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));
            if (!alreadyMarkedAsInstalled)
                installedPluginSystemNames.Add(systemName);

            //save installed plugin names to the file
            await SaveInstalledPluginNamesAsync(installedPluginSystemNames, filePath, cancellationToken);
        }

        /// <summary>
        /// Mark plugin as uninstalled
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that plugin is uninstalled</returns>
        public static async Task MarkPluginAsUninstalledAsync(string systemName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(systemName))
                throw new ArgumentNullException(nameof(systemName));

            var filePath = await _fileProvider.MapPathAsync(NopPluginDefaults.InstalledPluginsFilePath, cancellationToken);

            //create file if not exists
            await _fileProvider.CreateFileAsync(filePath, cancellationToken);

            //get installed plugin names
            var installedPluginSystemNames = await GetInstalledPluginNamesAsync(filePath, cancellationToken);

            //remove plugin system name from the list if exists
            var alreadyMarkedAsInstalled = installedPluginSystemNames.Any(pluginName => pluginName.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));
            if (alreadyMarkedAsInstalled)
                installedPluginSystemNames.Remove(systemName);

            //save installed plugin names to the file
            await SaveInstalledPluginNamesAsync(installedPluginSystemNames, filePath, cancellationToken);
        }

        /// <summary>
        /// Mark plugin as uninstalled
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that all plugins are uninstalled</returns>
        public static async Task MarkAllPluginsAsUninstalledAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var filePath = await _fileProvider.MapPathAsync(NopPluginDefaults.InstalledPluginsFilePath, cancellationToken);
            if (await _fileProvider.FileExistsAsync(filePath, cancellationToken))
                await _fileProvider.DeleteFileAsync(filePath, cancellationToken);
        }

        /// <summary>
        /// Find a plugin descriptor by some type which is located into the same assembly as plugin
        /// </summary>
        /// <param name="typeInAssembly">Type</param>
        /// <returns>Plugin descriptor if exists; otherwise null</returns>
        public static PluginDescriptor FindPlugin(Type typeInAssembly)
        {
            if (typeInAssembly == null)
                throw new ArgumentNullException(nameof(typeInAssembly));

            return ReferencedPlugins?.FirstOrDefault(plugin =>
                plugin.ReferencedAssembly != null &&
                plugin.ReferencedAssembly.FullName.Equals(typeInAssembly.Assembly.FullName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Get plugin descriptor from the plugin description file
        /// </summary>
        /// <param name="filePath">Path to the description file</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the plugin descriptor</returns>
        public static async Task<PluginDescriptor> GetPluginDescriptorFromFileAsync(string filePath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var text = await _fileProvider.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);

            return GetPluginDescriptorFromText(text);
        }

        /// <summary>
        /// Get plugin descriptor from the description text
        /// </summary>
        /// <param name="text">Description text</param>
        /// <returns>Plugin descriptor</returns>
        public static PluginDescriptor GetPluginDescriptorFromText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new PluginDescriptor();

            //get plugin descriptor from the JSON file
            var descriptor = JsonConvert.DeserializeObject<PluginDescriptor>(text);

            //nopCommerce 2.00 didn't have 'SupportedVersions' parameter, so let's set it to "2.00"
            if (!descriptor.SupportedVersions.Any())
                descriptor.SupportedVersions.Add("2.00");

            return descriptor;
        }

        /// <summary>
        /// Save plugin descriptor to the plugin description file
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that plugin descriptor is saved</returns>
        public static async Task SavePluginDescriptorAsync(PluginDescriptor pluginDescriptor,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (pluginDescriptor == null)
                throw new ArgumentException(nameof(pluginDescriptor));

            //get the description file path
            if (pluginDescriptor.OriginalAssemblyFile == null)
                throw new Exception($"Cannot load original assembly path for {pluginDescriptor.SystemName} plugin.");

            var directoryName = await _fileProvider.GetDirectoryNameAsync(pluginDescriptor.OriginalAssemblyFile, cancellationToken);
            var filePath = await _fileProvider.CombineAsync(new[] { directoryName, NopPluginDefaults.DescriptionFileName }, cancellationToken);
            if (!(await _fileProvider.FileExistsAsync(filePath, cancellationToken)))
                throw new Exception($"Description file for {pluginDescriptor.SystemName} plugin does not exist. {filePath}");

            //save the file
            var text = JsonConvert.SerializeObject(pluginDescriptor, Formatting.Indented);
            await _fileProvider.WriteAllTextAsync(filePath, text, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Delete plugin directory from disk storage
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether plugin is deleted</returns>
        public static async Task<bool> DeletePluginAsync(PluginDescriptor pluginDescriptor,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            //no plugin descriptor set
            if (pluginDescriptor == null)
                return false;

            //check whether plugin is installed
            if (pluginDescriptor.Installed)
                return false;

            var directoryName = await _fileProvider.GetDirectoryNameAsync(pluginDescriptor.OriginalAssemblyFile, cancellationToken);

            if (await _fileProvider.DirectoryExistsAsync(directoryName, cancellationToken))
                await _fileProvider.DeleteDirectoryAsync(directoryName, cancellationToken);

            return true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns a collection of all referenced plugin assemblies that have been shadow copied
        /// </summary>
        public static IEnumerable<PluginDescriptor> ReferencedPlugins { get; set; }

        /// <summary>
        /// Returns a collection of all plugin which are not compatible with the current version
        /// </summary>
        public static IEnumerable<string> IncompatiblePlugins { get; set; }

        #endregion
    }
}