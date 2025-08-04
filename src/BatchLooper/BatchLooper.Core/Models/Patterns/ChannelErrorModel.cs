using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Core.Models.Patterns
{
    [DebuggerDisplay("Item = {Item} | Error = {Error.Message, nq}")]
    public record ChannelErrorModel<TEntity>
    {
        public TEntity? Item { get; init; }
        public Exception Error { get; init; }

        public ChannelErrorModel(TEntity? item, [NotNull] Exception exception)
        {
            Item = item;
            Error = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }
}
