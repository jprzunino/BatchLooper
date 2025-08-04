using BatchLooper.Core.Enumerations;
using BatchLooper.Core.Interfaces.Batcher;
using BatchLooper.Core.Interfaces.Batcher.Printers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static BatchLooper.Core.Helpers.EnumHelper;

namespace BatchLooper.Infrastructure.Services.Batcher.WithSerilog
{
    [DebuggerDisplay("BatchStatistics: Duration = {TotalDuration} | AverageDuration = {AverageDuration}")]
    public class BatchStatisticsSerilog<TEntity> : IBatchStatistics<TEntity>
    {
        #region Variables

        private long _startDurationTricks;
        private readonly ConcurrentQueue<TimeSpan> _batchDuration;
        private readonly IBatchProgressPrinterSerilog<TEntity> _printer;
        internal protected readonly IBatchStatistics<TEntity> _recursive;

        #endregion

        #region Initialize

        private BatchStatisticsSerilog(ConcurrentQueue<TimeSpan>? batchDuration = null)
        {
            _recursive = this;
            _printer = default!;
            _batchDuration = batchDuration ?? new ConcurrentQueue<TimeSpan>();
            _recursive.StartTotalDurationMeter();
        }

        public BatchStatisticsSerilog([NotNull] IBatchProgressPrinterSerilog<TEntity> printer, ConcurrentQueue<TimeSpan>? batchDuration = null)
            : this(batchDuration) => _printer = printer ?? throw new ArgumentNullException(nameof(printer));

        #endregion

        #region Properties

        public TimeSpan TotalDuration => TimeProvider.System.GetElapsedTime(_startDurationTricks);

        public TimeSpan AverageDuration => TimeSpan.FromTicks(Convert.ToInt64(_batchDuration.Average(ts => ts.Ticks)));

        #endregion

        void IBatchStatistics<TEntity>.Enqueue(TimeSpan batchTime)
        {
            ArgumentNullException.ThrowIfNull(_batchDuration);
            _batchDuration?.Enqueue(batchTime);
        }

        public virtual void PrintSummary([NotNull] BatcherBuilderStatus executionStatus)
        {
            ArgumentNullException.ThrowIfNull(_printer);

            _printer.PrintInformation($"Status: {executionStatus.GetDescription()}.", inDebugToo: true);
            _printer.PrintInformation($"Total Duration: {_recursive.TotalDuration}.", inDebugToo: true);

            if (!_batchDuration.IsEmpty)
                _printer.PrintInformation($"Average Duration: {_recursive.AverageDuration}.", inDebugToo: true);
        }

        public virtual void PrintSummaryManual([NotNull] BatcherBuilderStatus executionStatus) => PrintSummary(executionStatus);

        void IBatchStatistics<TEntity>.StartTotalDurationMeter() => _startDurationTricks = TimeProvider.System.GetTimestamp();

        void IBatchStatistics<TEntity>.CollectDuration([NotNull] Action action)
        {
            ArgumentNullException.ThrowIfNull(_recursive);
            ArgumentNullException.ThrowIfNull(action);

            var start = TimeProvider.System.GetTimestamp();

            action();

            var duration = TimeProvider.System.GetElapsedTime(start);

            _recursive.Enqueue(duration);
        }

        void IBatchStatistics<TEntity>.CollectDuration([NotNull] Action<long> action)
        {
            ArgumentNullException.ThrowIfNull(_recursive);
            ArgumentNullException.ThrowIfNull(action);

            var start = TimeProvider.System.GetTimestamp();

            action(start);

            var duration = TimeProvider.System.GetElapsedTime(start);

            _recursive.Enqueue(duration);
        }

        async Task IBatchStatistics<TEntity>.CollectDurationAsync([NotNull] Func<Task> action)
        {
            ArgumentNullException.ThrowIfNull(_recursive);
            ArgumentNullException.ThrowIfNull(action);

            var start = TimeProvider.System.GetTimestamp();

            await action();

            var duration = TimeProvider.System.GetElapsedTime(start);

            _recursive.Enqueue(duration);
        }

        async Task IBatchStatistics<TEntity>.CollectDurationAsync([NotNull] Func<long, Task> action)
        {
            ArgumentNullException.ThrowIfNull(_recursive);
            ArgumentNullException.ThrowIfNull(action);

            var start = TimeProvider.System.GetTimestamp();

            await action(start);

            var duration = TimeProvider.System.GetElapsedTime(start);

            _recursive.Enqueue(duration);
        }
    }
}
