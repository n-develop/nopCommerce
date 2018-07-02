using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Services.Themes
{
    /// <summary>
    /// Represents a theme provider
    /// </summary>
    public partial interface IThemeProvider
    {
        /// <summary>
        /// Get theme descriptor from the description text
        /// </summary>
        /// <param name="text">Description text</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Theme descriptor</returns>
        Task<ThemeDescriptor> GetThemeDescriptorFromTextAsync(string text, CancellationToken cancellationToken);

        /// <summary>
        /// Get all themes
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>List of the theme descriptor</returns>
        Task<IList<ThemeDescriptor>> GetThemesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Get a theme by the system name
        /// </summary>
        /// <param name="systemName">Theme system name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>Theme descriptor</returns>
        Task<ThemeDescriptor> GetThemeBySystemNameAsync(string systemName, CancellationToken cancellationToken);

        /// <summary>
        /// Check whether the theme with specified system name exists
        /// </summary>
        /// <param name="systemName">Theme system name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>True if the theme exists; otherwise false</returns>
        Task<bool> ThemeExistsAsync(string systemName, CancellationToken cancellationToken);
    }
}