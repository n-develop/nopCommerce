using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Directory;
using Nop.Core.Plugins;

namespace Nop.Services.Directory
{
    /// <summary>
    /// Exchange rate provider interface
    /// </summary>
    public partial interface IExchangeRateProvider : IPlugin
    {
        /// <summary>
        /// Gets currency live rates
        /// </summary>
        /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
        /// <returns>Exchange rates</returns>
        IList<ExchangeRate> GetCurrencyLiveRates(string exchangeRateCurrencyCode);

        /// <summary>
        /// Gets currency live rates
        /// </summary>
        /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
        /// <returns>Exchange rates</returns>
        Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(string exchangeRateCurrencyCode);
    }
}