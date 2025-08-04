using BatchLooper.Core.Enumerations;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Core.Interfaces.Batcher
{
    public interface IBatchStatistics<TEntity>
    {
        public TimeSpan TotalDuration { get; }
        public TimeSpan AverageDuration { get; }

        void Enqueue(TimeSpan batchTime);
        void StartTotalDurationMeter();

        void CollectDuration([NotNull] Action action);
        void CollectDuration([NotNull] Action<long> action);
        Task CollectDurationAsync([NotNull] Func<Task> action);
        Task CollectDurationAsync([NotNull] Func<long, Task> action);

        void PrintSummary([NotNull] BatcherBuilderStatus executionStatus);
        void PrintSummaryManual([NotNull] BatcherBuilderStatus executionStatus);
    }
}
