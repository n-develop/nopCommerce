using System.Collections.Generic;

namespace Nop.Core
{
    /// <summary>
    /// Represents a paged list
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list</typeparam>
    public partial interface IPagedList<T> : IList<T>
    {
        /// <summary>
        /// Gets a page index
        /// </summary>
        int PageIndex { get; }

        /// <summary>
        /// Gets a page size
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Gets a total count
        /// </summary>
        int TotalCount { get; }

        /// <summary>
        /// Gets a total pages
        /// </summary>
        int TotalPages { get; }

        /// <summary>
        /// Gets a value indicating whether the list has a previous page
        /// </summary>
        bool HasPreviousPage { get; }

        /// <summary>
        /// Gets a value indicating whether the list has a next page
        /// </summary>
        bool HasNextPage { get; }
    }
}