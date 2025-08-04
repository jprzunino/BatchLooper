using System.Diagnostics.CodeAnalysis;

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

            Collection = new([.. batch.Select((s, idx) => new BatchItemInfoModel<TEntity>(s, Interlocked.Add(ref idx, 1)))]);
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
