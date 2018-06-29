using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;

namespace Nop.Services
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Convert to select list
        /// </summary>
        /// <typeparam name="TEnum">Enum type</typeparam>
        /// <param name="enumObj">Enum</param>
        /// <param name="markCurrentAsSelected">Mark current value as selected</param>
        /// <param name="valuesToExclude">Values to exclude</param>
        /// <param name="useLocalization">Localize</param>
        /// <returns>SelectList</returns>
        public static SelectList ToSelectList<TEnum>(this TEnum enumObj,
           bool markCurrentAsSelected = true, int[] valuesToExclude = null, bool useLocalization = true) where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum) throw new ArgumentException("An Enumeration type is required.", "enumObj");

            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            var workContext = EngineContext.Current.Resolve<IWorkContext>();

            var values = from TEnum enumValue in Enum.GetValues(typeof(TEnum))
                         where valuesToExclude == null || !valuesToExclude.Contains(Convert.ToInt32(enumValue))
                         select new { ID = Convert.ToInt32(enumValue), Name = useLocalization ? enumValue.GetLocalizedEnum(localizationService, workContext) : CommonHelper.ConvertEnum(enumValue.ToString()) };
            object selectedValue = null;
            if (markCurrentAsSelected)
                selectedValue = Convert.ToInt32(enumObj);
            return new SelectList(values, "ID", "Name", selectedValue);
        }

        /// <summary>
        /// Convert to select list
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="objList">List of objects</param>
        /// <param name="selector">Selector for name</param>
        /// <returns>SelectList</returns>
        public static SelectList ToSelectList<T>(this T objList, Func<BaseEntity, string> selector) where T : IEnumerable<BaseEntity>
        {
            return new SelectList(objList.Select(p => new { ID = p.Id, Name = selector(p) }), "ID", "Name");
        }

        /// <summary>
        /// Creates a paged list from an IQueryable source
        /// </summary>
        /// <typeparam name="T">The type of the elements of source</typeparam>
        /// <param name="source">The IQueryable to create a paged list from</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
        /// <returns>The asynchronous task whose result contains the paged list that contains elements from the input sequence</returns>
        public static async Task<IPagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int pageIndex, int pageSize,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            //get total items number
            var totalItems = await source.CountAsync(cancellationToken);

            //calculate total pages number by the total items and page size
            var totalPages = totalItems / pageSize + (totalItems % pageSize > 0 ? 1 : 0);

            //create paged list and populate it with passed elements
            var pagedList = new PagedList<T>(totalItems, totalPages, pageIndex, pageSize);
            var items = await source.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            pagedList.AddRange(items);

            return pagedList;
        }

        /// <summary>
        /// Creates a paged list from an IList source
        /// </summary>
        /// <typeparam name="T">The type of the elements of source</typeparam>
        /// <param name="source">The IList to create a paged list from</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>A paged list that contains elements from the input sequence</returns>
        public static IPagedList<T> ToPagedList<T>(this IList<T> source, int pageIndex, int pageSize)
        {
            //get total items number
            var totalItems = source.Count;

            //calculate total pages number by the total items and page size
            var totalPages = totalItems / pageSize + (totalItems % pageSize > 0 ? 1 : 0);

            //create paged list and populate it with passed elements
            var pagedList = new PagedList<T>(totalItems, totalPages, pageIndex, pageSize);
            var items = source.Skip(pageIndex * pageSize).Take(pageSize);
            pagedList.AddRange(items);

            return pagedList;
        }

        /// <summary>
        /// Creates a paged list from an IEnumerable source
        /// </summary>
        /// <typeparam name="T">The type of the elements of source</typeparam>
        /// <param name="source">The IEnumerable to create a paged list from</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="totalCount">Total items number</param>
        /// <returns>A paged list that contains elements from the input sequence</returns>
        public static IPagedList<T> ToPagedList<T>(this IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
        {
            //get total items number
            var totalItems = totalCount;

            //calculate total pages number by the total items and page size
            var totalPages = totalItems / pageSize + (totalItems % pageSize > 0 ? 1 : 0);

            //create paged list and populate it with passed elements
            var pagedList = new PagedList<T>(totalItems, totalPages, pageIndex, pageSize);
            var items = source;
            pagedList.AddRange(items);

            return pagedList;
        }
    }
}