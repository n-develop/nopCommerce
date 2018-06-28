using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Core.Data;

namespace Nop.Core.Http
{
    /// <summary>
    /// Represents middleware that checks whether request is for keep alive
    /// </summary>
    public partial class KeepAliveMiddleware
    {
        #region Fields

        private readonly RequestDelegate _next;

        #endregion

        #region Ctor

        public KeepAliveMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invoke middleware actions
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="webHelper">Web helper</param>
        /// <returns>Task</returns>
        public async Task InvokeAsync(HttpContext context, IWebHelper webHelper)
        {
            //TODO test. ensure that no guest record is created

            //whether database is installed
            if (!(await DataSettingsManager.DatabaseIsInstalledAsync()))
            {
                //keep alive page requested (we ignore it to prevent creating a guest customer records)
                var keepAliveUrl = $"{(await webHelper.GetStoreLocationAsync())}{NopHttpDefaults.KeepAlivePath}";
                if ((await webHelper.GetThisPageUrlAsync(false)).StartsWith(keepAliveUrl, StringComparison.InvariantCultureIgnoreCase))
                    return;
            }

            //or call the next middleware in the request pipeline
            await _next(context);
        }

        #endregion
    }
}