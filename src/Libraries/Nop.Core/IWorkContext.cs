using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;

namespace Nop.Core
{
    /// <summary>
    /// Represents work context
    /// </summary>
    public partial interface IWorkContext
    {
        /// <summary>
        /// Get the current customer
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the current customer</returns>
        Task<Customer> GetCurrentCustomerAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Set the current customer
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that current customer is set</returns>
        Task SetCurrentCustomerAsync(Customer customer, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the original customer (in case the current one is impersonated)
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the original customer</returns>
        Task<Customer> GetOriginalCustomerIfImpersonatedAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the current vendor (logged-in manager)
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the current vendor</returns>
        Task<Vendor> GetCurrentVendorAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the working language
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the working language</returns>
        Task<Language> GetWorkingLanguageAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Set the working language
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that working language is set</returns>
        Task SetWorkingLanguageAsync(Language language, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the working currency
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the working currency</returns>
        Task<Currency> GetWorkingCurrencyAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Set the working currency
        /// </summary>
        /// <param name="currency">Currency</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that working currency is set</returns>
        Task SetWorkingCurrencyAsync(Currency currency, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the current tax display type
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the current tax display type</returns>
        Task<TaxDisplayType> GetTaxDisplayTypeAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Set the current tax display type
        /// </summary>
        /// <param name="taxDisplayType">Tax display type</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result determines that current tax display type is set</returns>
        Task SetTaxDisplayTypeAsync(TaxDisplayType taxDisplayType, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets or sets value indicating whether we're in admin area
        /// </summary>
        bool IsAdmin { get; set; }
    }
}