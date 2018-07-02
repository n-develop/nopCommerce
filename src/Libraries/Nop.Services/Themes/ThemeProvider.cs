using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Infrastructure;
using Nop.Core.Plugins;

namespace Nop.Services.Themes
{
    /// <summary>
    /// Represents a default theme provider implementation
    /// </summary>
    public partial class ThemeProvider : IThemeProvider
    {
        #region Fields

        private IList<ThemeDescriptor> _themeDescriptors;
        private readonly INopFileProvider _fileProvider;

        #endregion

        #region Ctor

        public ThemeProvider(INopFileProvider fileProvider)
        {
            this._fileProvider = fileProvider;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get theme descriptor from the description text
        /// </summary>
        /// <param name="text">Description text</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Theme descriptor</returns>
        public async Task<ThemeDescriptor> GetThemeDescriptorFromTextAsync(string text, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() =>
            {
                //get theme description from the JSON file
                var themeDescriptor = JsonConvert.DeserializeObject<ThemeDescriptor>(text);

                //some validation
                if (_themeDescriptors?.Any(descriptor =>
                        descriptor.SystemName.Equals(themeDescriptor?.SystemName,
                            StringComparison.InvariantCultureIgnoreCase)) ?? false)
                    throw new Exception($"A theme with '{themeDescriptor.SystemName}' system name is already defined");

                return themeDescriptor;
            }, cancellationToken);
        }

        /// <summary>
        /// Get all themes
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>List of the theme descriptor</returns>
        public async Task<IList<ThemeDescriptor>> GetThemesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_themeDescriptors != null)
                return _themeDescriptors;

            //load all theme descriptors
            _themeDescriptors = new List<ThemeDescriptor>();

            var themeDirectoryPath =  await _fileProvider.MapPathAsync(NopPluginDefaults.ThemesPath, cancellationToken);
            foreach (var descriptionFile in await _fileProvider.GetFilesAsync(themeDirectoryPath, NopPluginDefaults.ThemeDescriptionFileName, false, cancellationToken))
            {
                var text =  await _fileProvider.ReadAllTextAsync(descriptionFile, Encoding.UTF8, cancellationToken);
                if (string.IsNullOrEmpty(text))
                    continue;

                //get theme descriptor
                var themeDescriptor = await GetThemeDescriptorFromTextAsync(text, cancellationToken);

                //some validation
                if (string.IsNullOrEmpty(themeDescriptor?.SystemName))
                    throw new Exception($"A theme descriptor '{descriptionFile}' has no system name");

                _themeDescriptors.Add(themeDescriptor);
            }

            return _themeDescriptors;
        }

        /// <summary>
        /// Get a theme by the system name
        /// </summary>
        /// <param name="systemName">Theme system name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Theme descriptor</returns>
        public async Task<ThemeDescriptor> GetThemeBySystemNameAsync(string systemName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(systemName))
                return null;

            return (await GetThemesAsync(cancellationToken)).SingleOrDefault(descriptor => descriptor.SystemName.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Check whether the theme with specified system name exists
        /// </summary>
        /// <param name="systemName">Theme system name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>True if the theme exists; otherwise false</returns>
        public async Task<bool> ThemeExistsAsync(string systemName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(systemName))
                return false;

            return (await GetThemesAsync(cancellationToken)).Any(descriptor => descriptor.SystemName.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion
    }
}