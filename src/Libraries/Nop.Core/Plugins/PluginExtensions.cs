using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Infrastructure;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Represents plugin extensions
    /// </summary>
    public static partial class PluginExtensions
    {
        private static readonly List<string> SupportedLogoImageExtensions = new List<string>
        {
            "jpg",
            "png",
            "gif"
        };

        /// <summary>
        /// Get logo URL
        /// </summary>
        /// <param name="pluginDescriptor">Plugin descriptor</param>
        /// <param name="webHelper">Web helper</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the logo URL</returns>
        public static async Task<string> GetLogoUrlAsync(this PluginDescriptor pluginDescriptor, IWebHelper webHelper,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            if (webHelper == null)
                throw new ArgumentNullException(nameof(webHelper));

            var fileProvider = await EngineContext.Current.ResolveAsync<INopFileProvider>(cancellationToken);

            var pluginDirectory = await fileProvider.GetDirectoryNameAsync(pluginDescriptor.OriginalAssemblyFile, cancellationToken);

            if (string.IsNullOrEmpty(pluginDirectory))
                return null;

            var logoExtension = SupportedLogoImageExtensions.FirstOrDefault(ext =>
            {
                var path = fileProvider.CombineAsync(new[] { pluginDirectory, $"{NopPluginDefaults.LogoFileName}.{ext}" }, cancellationToken).Result;
                return fileProvider.FileExistsAsync(path, cancellationToken).Result;
            });

            //No logo file was found with any of the supported extensions.
            if (string.IsNullOrWhiteSpace(logoExtension))
                return null;

            var storeLocation = await webHelper.GetStoreLocationAsync(cancellationToken: cancellationToken);
            var pluginDirectoryName = await fileProvider.GetDirectoryNameOnlyAsync(pluginDirectory, cancellationToken);
            var logoUrl = $"{storeLocation}plugins/{pluginDirectoryName}/{NopPluginDefaults.LogoFileName}.{logoExtension}";

            return logoUrl;
        }
    }
}