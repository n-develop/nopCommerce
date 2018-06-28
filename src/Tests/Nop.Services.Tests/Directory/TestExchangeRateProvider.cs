using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Directory;
using Nop.Core.Plugins;
using Nop.Services.Directory;

namespace Nop.Services.Tests.Directory
{
    public class TestExchangeRateProvider : BasePlugin, IExchangeRateProvider
    {
        /// <summary>
        /// Gets currency live rates
        /// </summary>
        /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
        /// <returns>Exchange rates</returns>
        public IList<ExchangeRate> GetCurrencyLiveRates(string exchangeRateCurrencyCode)
        {
            return new List<ExchangeRate>();
        }

        /// <summary>
        /// Gets currency live rates
        /// </summary>
        /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
        /// <returns>Exchange rates</returns>
        public async Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(string exchangeRateCurrencyCode)
        {
            return await Task.Run(() => new List<ExchangeRate>());
        }
    }
}
