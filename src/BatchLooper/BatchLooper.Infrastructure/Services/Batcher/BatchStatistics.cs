using BatchLooper.Core.Enumerations;
using BatchLooper.Core.Interfaces.Batcher;
using BatchLooper.Core.Interfaces.Batcher.Printers;
using BatchLooper.Core.Interfaces.Patterns;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static BatchLooper.Core.Helpers.EnumHelper;

namespace BatchLooper.Infrastructure.Services.Batcher
{
    [DebuggerDisplay("BatchStatistics: Duration = {TotalDuration} | AverageDuration = {AverageDuration}")]
    public class BatchStatistics<TEntity> : IBatchStatistics<TEntity>
    {
        #region Variables

        private long _startDurationTricks;
        private readonly ConcurrentQueue<TimeSpan> _batchDuration;
        private readonly IBatchProgressPrinterConsole<TEntity> _printer;
        private readonly ISemaphorePattern _semaphore;
        internal protected readonly IBatchStatistics<TEntity> _recursive;

        #endregion

        #region Initialize

        private BatchStatistics([NotNull] ISemaphorePattern semaphore, ConcurrentQueue<TimeSpan>? batchDuration = null)
        {
            _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
            _recursive = this;
            _printer = default!;
            _batchDuration = batchDuration ?? new ConcurrentQueue<TimeSpan>();
            _recursive.StartTotalDurationMeter();
        }

        public BatchStatistics([NotNull] IBatchProgressPrinterConsole<TEntity> printer, ConcurrentQueue<TimeSpan>? batchDuration = null, ISemaphorePattern? semaphore = null)
            : this(semaphore ?? printer.Semaphore ?? throw new ArgumentNullException(nameof(semaphore)), batchDuration) =>
            _printer = printer ?? throw new ArgumentNullException(nameof(printer));

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
            ArgumentNullException.ThrowIfNull(_semaphore);

            var errors = _semaphore.HandleConcurencyMultithread(TimeSpan.FromSeconds(2), () =>
            {
                if (_printer.Configuration.PrintProgress)
                {
                    _printer.SynchronizeCursor();

                    _printer.PrintConsole("Status: ", useWriteLine: false);
                    _printer.PrintConsole(executionStatus.GetDescription(), useWriteLine: false,
                        textcolor: executionStatus switch
                        {
                            BatcherBuilderStatus.Success => ConsoleColor.Green,
                            BatcherBuilderStatus.Running => ConsoleColor.Yellow,
                            BatcherBuilderStatus.Canceled => ConsoleColor.Magenta,
                            _ => ConsoleColor.Red
                        });
                    _printer.PrintConsole(".");

                    _printer.PrintConsole($"Total Duration: {_recursive.TotalDuration}.");

                    if (!_batchDuration.IsEmpty)
                        _printer.PrintConsole($"Average Duration: {_recursive.AverageDuration}.");
                }

                _printer.PrintDebug($"Status: {executionStatus.GetDescription()}.");
                _printer.PrintDebug($"Total Duration: {_recursive.TotalDuration}.");

                if (!_batchDuration.IsEmpty)
                    _printer.PrintDebug($"Average Duration: {_recursive.AverageDuration}.");
            });

            if (errors is not null && !errors.IsEmpty)
                throw new AggregateException(message: $"Error in {nameof(PrintSummary)}, see inner exceptions from more details.", innerExceptions: errors);
        }

        public virtual void PrintSummaryManual([NotNull] BatcherBuilderStatus executionStatus)
        {
            ArgumentNullException.ThrowIfNull(_printer);
            ArgumentNullException.ThrowIfNull(_semaphore);

            var errors = _semaphore.HandleConcurencyMultithread(TimeSpan.FromSeconds(2), () =>
            {
                if (_printer.Configuration.PrintProgressManual)
                {
                    _printer.SynchronizeCursor();

                    _printer.PrintConsole("Status: ", useWriteLine: false);
                    _printer.PrintConsole(executionStatus.GetDescription(), useWriteLine: false,
                        textcolor: executionStatus switch
                        {
                            BatcherBuilderStatus.Success => ConsoleColor.Green,
                            BatcherBuilderStatus.Running => ConsoleColor.Yellow,
                            BatcherBuilderStatus.Canceled => ConsoleColor.Magenta,
                            _ => ConsoleColor.Red
                        });
                    _printer.PrintConsole(".");

                    _printer.PrintConsole($"Total Duration: {_recursive.TotalDuration}.");

                    if (!_batchDuration.IsEmpty)
                        _printer.PrintConsole($"Average Duration: {_recursive.AverageDuration}.");
                }

                _printer.PrintDebug($"Status: {executionStatus.GetDescription()}.");
                _printer.PrintDebug($"Total Duration: {_recursive.TotalDuration}.");

                if (!_batchDuration.IsEmpty)
                    _printer.PrintDebug($"Average Duration: {_recursive.AverageDuration}.");
            });

            if (errors is not null && !errors.IsEmpty)
                throw new AggregateException(message: $"Error in {nameof(PrintSummary)}, see inner exceptions from more details.", innerExceptions: errors);
        }

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
