using System;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Infrastructure;
using Nop.Services.Seo;

namespace Nop.Services.Affiliates
{
    /// <summary>
    /// Represents an affiliate extensions
    /// </summary>
    public static partial class AffiliateExtensions
    {
        /// <summary>
        /// Get full name
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <returns>Affiliate full name</returns>
        public static string GetFullName(this Affiliate affiliate)
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            var firstName = affiliate.Address.FirstName;
            var lastName = affiliate.Address.LastName;

            var fullName = string.Empty;
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                fullName = $"{firstName} {lastName}";
            else
            {
                if (!string.IsNullOrWhiteSpace(firstName))
                    fullName = firstName;

                if (!string.IsNullOrWhiteSpace(lastName))
                    fullName = lastName;
            }

            return fullName;
        }

        /// <summary>
        /// Generate affiliate URL
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <param name="webHelper">Web helper</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the generated affiliate URL</returns>
        public static async Task<string> GenerateUrlAsync(this Affiliate affiliate, IWebHelper webHelper,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            if (webHelper == null)
                throw new ArgumentNullException(nameof(webHelper));

            var storeUrl = await webHelper.GetStoreLocationAsync(false, cancellationToken);
            var url = await (!string.IsNullOrEmpty(affiliate.FriendlyUrlName) ?
                //use friendly URL
                webHelper.ModifyQueryStringAsync(storeUrl, NopAffiliateDefaults.AffiliateQueryParameter, new[] { affiliate.FriendlyUrlName }, cancellationToken) :
                //use ID
                webHelper.ModifyQueryStringAsync(storeUrl, NopAffiliateDefaults.AffiliateIdQueryParameter, new[] { affiliate.Id.ToString() }, cancellationToken));

            return url;
        }

        /// <summary>
        /// Validate friendly URL name
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        /// <param name="friendlyUrlName">Friendly URL name</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the valid friendly name</returns>
        public static async Task<string> ValidateFriendlyUrlNameAsync(this Affiliate affiliate, string friendlyUrlName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            //ensure we have only valid chars
            friendlyUrlName = await SeoExtensions.GetSeNameAsync(friendlyUrlName, cancellationToken);

            //max length
            //(consider a store URL + probably added {0}-{1} below)
            friendlyUrlName = CommonHelper.EnsureMaximumLength(friendlyUrlName, NopAffiliateDefaults.FriendlyUrlNameLength);

            //ensure this name is not reserved yet
            //empty? nothing to check
            if (string.IsNullOrEmpty(friendlyUrlName))
                return friendlyUrlName;
            //check whether such friendly URL name already exists (and that is not the current affiliate)
            var i = 2;
            var tempName = friendlyUrlName;
            while (true)
            {
                var affiliateService = await EngineContext.Current.ResolveAsync<IAffiliateService>(cancellationToken);
                var affiliateByFriendlyUrlName = await affiliateService.GetAffiliateByFriendlyUrlNameAsync(tempName, cancellationToken);

                var reserved = affiliateByFriendlyUrlName != null && affiliateByFriendlyUrlName.Id != affiliate.Id;
                if (!reserved)
                    break;

                tempName = $"{friendlyUrlName}-{i}";
                i++;
            }

            friendlyUrlName = tempName;

            return friendlyUrlName;
        }
    }
}