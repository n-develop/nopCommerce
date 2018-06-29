using System;
using System.Collections.Generic;

namespace Nop.Core
{
    /// <summary>
    /// Represents the paged list implementation
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list</typeparam>
    [Serializable]
    public partial class PagedList<T> : List<T>, IPagedList<T>
    {
        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="totalCount">Total item number</param>
        /// <param name="totalPages">Total page number</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        public PagedList(int totalCount, int totalPages, int pageIndex, int pageSize)
        {
            this.TotalCount = totalCount;
            this.TotalPages = totalPages;
            this.PageSize = pageSize;
            this.PageIndex = pageIndex;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a page index
        /// </summary>
        public virtual int PageIndex { get; }

        /// <summary>
        /// Gets a page size
        /// </summary>
        public virtual int PageSize { get; }

        /// <summary>
        /// Gets a total count
        /// </summary>
        public virtual int TotalCount { get; }

        /// <summary>
        /// Gets a total pages
        /// </summary>
        public virtual int TotalPages { get; }

        /// <summary>
        /// Gets a value indicating whether the list has a previous page
        /// </summary>
        public virtual bool HasPreviousPage => PageIndex > 0;

        /// <summary>
        /// Gets a value indicating whether the list has a next page
        /// </summary>
        public virtual bool HasNextPage => PageIndex + 1 < TotalPages;

        #endregion
    }
}