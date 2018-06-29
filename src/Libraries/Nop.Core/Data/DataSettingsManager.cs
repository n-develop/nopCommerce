using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Infrastructure;

namespace Nop.Core.Data
{
    /// <summary>
    /// Represents the data settings manager
    /// </summary>
    public partial class DataSettingsManager
    {
        #region Fields

        private static bool? _databaseIsInstalled;

        #endregion

        #region Methods

        /// <summary>
        /// Load data settings
        /// </summary>
        /// <param name="filePath">File path; pass null to use the default settings file</param>
        /// <param name="reloadSettings">Whether to reload data, if they already loaded</param>
        /// <param name="fileProvider">File provider</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains loaded settings</returns>
        public static async Task<DataSettings> LoadSettingsAsync(string filePath = null, bool reloadSettings = false, 
            INopFileProvider fileProvider = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!reloadSettings && Singleton<DataSettings>.Instance != null)
                return Singleton<DataSettings>.Instance;

            fileProvider = fileProvider ?? CommonHelper.DefaultFileProvider;
            filePath = filePath ?? await fileProvider.MapPathAsync(NopDataSettingsDefaults.FilePath, cancellationToken);

            //check whether file exists
            if (!(await fileProvider.FileExistsAsync(filePath, cancellationToken)))
            {
                //if not, try to parse the file that was used in previous nopCommerce versions
                filePath = await fileProvider.MapPathAsync(NopDataSettingsDefaults.ObsoleteFilePath, cancellationToken);
                if (!(await fileProvider.FileExistsAsync(filePath, cancellationToken)))
                    return new DataSettings();

                //get data settings from the old txt file
                var dataSettings = new DataSettings();
                using (var reader = new StringReader(await fileProvider.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken)))
                {
                    string settingsLine;
                    while ((settingsLine = await reader.ReadLineAsync()) != null)
                    {
                        var separatorIndex = settingsLine.IndexOf(':');
                        if (separatorIndex == -1)
                            continue;

                        var key = settingsLine.Substring(0, separatorIndex).Trim();
                        var value = settingsLine.Substring(separatorIndex + 1).Trim();

                        switch (key)
                        {
                            case "DataProvider":
                                dataSettings.DataProvider = Enum.TryParse(value, true, out DataProviderType providerType)
                                    ? providerType : DataProviderType.Unknown;
                                continue;
                            case "DataConnectionString":
                                dataSettings.DataConnectionString = value;
                                continue;
                            default:
                                dataSettings.RawDataSettings.Add(key, value);
                                continue;
                        }
                    }
                }

                //save data settings to the new file
                await SaveSettingsAsync(dataSettings, fileProvider, cancellationToken);

                //and delete the old one
                await fileProvider.DeleteFileAsync(filePath, cancellationToken);

                Singleton<DataSettings>.Instance = dataSettings;
                return Singleton<DataSettings>.Instance;
            }

            var text = await fileProvider.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
            if (string.IsNullOrEmpty(text))
                return new DataSettings();

            //get data settings from the JSON file
            Singleton<DataSettings>.Instance = JsonConvert.DeserializeObject<DataSettings>(text);

            return Singleton<DataSettings>.Instance;
        }

        /// <summary>
        /// Save data settings to the file
        /// </summary>
        /// <param name="settings">Data settings</param>
        /// <param name="fileProvider">File provider</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that settings are saved</returns>
        public static async Task SaveSettingsAsync(DataSettings settings, INopFileProvider fileProvider = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Singleton<DataSettings>.Instance = settings ?? throw new ArgumentNullException(nameof(settings));

            fileProvider = fileProvider ?? CommonHelper.DefaultFileProvider;
            var filePath = await fileProvider.MapPathAsync(NopDataSettingsDefaults.FilePath, cancellationToken);

            //create file if not exists
            await fileProvider.CreateFileAsync(filePath, cancellationToken);

            //save data settings to the file
            var text = JsonConvert.SerializeObject(Singleton<DataSettings>.Instance, Formatting.Indented);
            await fileProvider.WriteAllTextAsync(filePath, text, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Get a value indicating whether database is already installed
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines whether database is already installed</returns>
        public static async Task<bool> DatabaseIsInstalledAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_databaseIsInstalled.HasValue)
            {
                var settings = await LoadSettingsAsync(reloadSettings: true, cancellationToken: cancellationToken);
                _databaseIsInstalled = !string.IsNullOrEmpty(settings?.DataConnectionString);
            }

            return _databaseIsInstalled.Value;
        }

        /// <summary>
        /// Reset "database is installed" cached information
        /// </summary>
        public static void ResetCache()
        {
            _databaseIsInstalled = null;
        }

        #endregion
    }
}