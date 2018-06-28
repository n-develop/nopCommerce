
namespace Nop.Core.Infrastructure.Mapper
{
    /// <summary>
    /// Represents an ordered mapper profile
    /// </summary>
    public partial interface IOrderedMapperProfile
    {
        /// <summary>
        /// Gets order of this configuration implementation
        /// </summary>
        int Order { get; }
    }
}