using BatchLooper.Core.Helpers;
using BatchLooper.Core.Interfaces.Batcher;
using BatchLooper.Core.Interfaces.Patterns;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static BatchLooper.Core.Helpers.InterateParallelHelper;

namespace BatchLooper.Core.Models.Batcher
{
    public record BatchManagerModel<TEntity>
    {
        private readonly ConcurrentDictionary<int, BatchItemInfoModel<TEntity>> _dictItemsInfo;
        private readonly ISemaphorePattern _semaphore;

        public BatchInfoModel<TEntity>? BatchInfo { get; init; }

        public PrintDataInfoModel<TEntity> this[int ItemIndex]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ItemIndex);
                ArgumentNullException.ThrowIfNull(BatchInfo);

                if (!_dictItemsInfo.TryGetValue(ItemIndex, out var itemInfo))
                    throw new ArgumentOutOfRangeException(nameof(ItemIndex));

                var result = new PrintDataInfoModel<TEntity>(_semaphore, BatchInfo, itemInfo);
                return result;

            }
        }

        public PrintDataInfoModel<TEntity> TotalProgress
        {
            get
            {
                ArgumentNullException.ThrowIfNull(BatchInfo);
                return new(_semaphore, BatchInfo);
            }
        }

        public BatchManagerModel([NotNull] ISemaphorePattern semaphore)
        {
            _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
            BatchInfo = null;
            _dictItemsInfo = new();
        }

        public BatchManagerModel([NotNull] ISemaphorePattern semaphore, [NotNull] BatchInfoModel<TEntity> batchInfo, ParallelOptions? parallelOptions = null)
            : this(semaphore)
        {
            BatchInfo = batchInfo ?? throw new ArgumentNullException(nameof(batchInfo));
            BatchInfo.Collection
                 .ProcessInParallelWithPartitioner(parallelOptions: parallelOptions ?? new ParallelOptions(),
                     action: item => _dictItemsInfo[item.Index] = item);

        }
        public BatchManagerModel([NotNull] ISemaphorePattern semaphore, [NotNull] BatchInfoModel<TEntity> batchInfo, [NotNull] IBatcherConfiguration<TEntity> configuration)
            : this(semaphore ?? throw new ArgumentNullException(nameof(semaphore)))
        {
            ArgumentNullException.ThrowIfNull(batchInfo);


            BatchInfo = batchInfo ?? throw new ArgumentNullException(nameof(batchInfo));
            BatchInfo.Collection
                 .ProcessInParallelWhenAllAsync(batchesInParallel: configuration.BatchCount, degreeOfParallelism: configuration is not { MaxDegreeOfParallelism: -1 } ? (int)Math.Ceiling(configuration.MaxDegreeOfParallelism * 0.25M) : null, partitionerOptions: EnumerablePartitionerOptions.NoBuffering, cancellationToken: configuration.CancellationToken,
                 body: async item => { _dictItemsInfo[item.Index] = item; await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.None); });
        }

        public BatchManagerModel([NotNull] ISemaphorePattern semaphore, [NotNull] IEnumerable<TEntity> batchItems, int index, int batchesCount, int? linePosition = null, ParallelOptions? parallelOptions = null)
            : this(semaphore: semaphore,
                  batchInfo: linePosition is not null and >= 0 ?
                    new BatchInfoModel<TEntity>(batchItems, index, batchesCount, linePosition.Value) :
                    new BatchInfoModel<TEntity>(batchItems, index, batchesCount),
                  parallelOptions: parallelOptions)
        { }

        public BatchManagerModel([NotNull] ISemaphorePattern semaphore, [NotNull] IEnumerable<TEntity> batchItems, int index, int batchesCount, [NotNull] IBatcherConfiguration<TEntity> configuration, int? linePosition = null)
            : this(semaphore: semaphore,
                  batchInfo: linePosition is not null and >= 0 ?
                    new BatchInfoModel<TEntity>(batchItems, index, batchesCount, linePosition.Value) :
                    new BatchInfoModel<TEntity>(batchItems, index, batchesCount),
                  configuration: configuration)
        { }
    }
}
