using BatchLooper.Core.Helpers;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using static BatchLooper.Core.Extensions.EnumerableExtensions;

namespace BatchLooper.Core.Models.Batcher
{
    public record BatchInfoModel<TEntity>
    {
        public Memory<BatchItemInfoModel<TEntity>> Collection { get; init; }

        public int ItemsCount { get; init; }
        public int BatchesCount { get; init; }
        public int Index { get; init; }
        public int LinePosition { get; init; }

        public BatchInfoModel()
        {
            BatchesCount = 1;
            Collection = new();
            Index = 1;
            ItemsCount = Collection.Length;
            LinePosition = -1;
        }

        public BatchInfoModel([NotNull] IEnumerable<BatchItemInfoModel<TEntity>> batch, int index, int batchesCount)
            : this()
        {
            ArgumentNullException.ThrowIfNull(batch);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(index, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchesCount, 0);

            Collection = batch.ToArray();
            Index = index;
            ItemsCount = Collection.Length;
            BatchesCount = batchesCount;
        }

        public BatchInfoModel([NotNull] IEnumerable<TEntity> batch, int index, int batchesCount)
           : this()
        {
            ArgumentNullException.ThrowIfNull(batch);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

            Index = index;
            BatchesCount = batchesCount;

            var array = batch as TEntity[] ?? batch.ToPooledArray();
            var buffer = new BatchItemInfoModel<TEntity>[array.Length];
            int i = 0;

            array.IterateSpanAndUnsafe(item => buffer[i] = new BatchItemInfoModel<TEntity>(array[i], ++i));

            Collection = buffer;
            ItemsCount = Collection.Length;
        }


        public BatchInfoModel([NotNull] IEnumerable<TEntity> batch, int index, int batchesCount, int linePosition)
            : this(batch, index, batchesCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(linePosition);
            LinePosition = linePosition;
        }
    }
}
