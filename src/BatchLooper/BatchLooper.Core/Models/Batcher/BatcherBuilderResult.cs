using BatchLooper.Core.Enumerations;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Core.Models.Batcher
{
    public record BatcherBuilderResult
    {
        public IEnumerable<Exception> Errors { get; init; }
        public BatcherBuilderStatus Status { get; init; }
        private static readonly IEnumerable<Exception> _emptyItems = [];

        public BatcherBuilderResult()
            : this(BatcherBuilderStatus.None, _emptyItems) { }

        public BatcherBuilderResult([NotNull] BatcherBuilderStatus status, IEnumerable<Exception> errors)
        {
            Errors = errors ?? _emptyItems;
            Status = status;
        }
    }
}
