using System.Diagnostics.CodeAnalysis;
using static BatchLooper.Core.Helpers.ConvertHelper;

namespace BatchLooper.Core.Models.Batcher
{
    public record BatchOrderModel<TEntity>
    {
        public TEntity? Data { get; init; }
        public int Index { get; init; }

        public int Count { get; init; }

        public int ThreadId { get; init; }

        public BatchOrderModel()
        {
            Data = default;
            Index = 1;
            ThreadId = Environment.CurrentManagedThreadId;
        }

        public BatchOrderModel([NotNull] TEntity data, int index)
            : this()
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(index, 0);

            Data = data;
            Index = index;
            Count = TryGetCountFromData(data);
        }

        public BatchOrderModel([NotNull] TEntity data, int index, int count)
            : this(data, index)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(count, 0);
            Count = count;
        }

        public BatchOrderModel([NotNull] TEntity data, int index, int count, int threadId)
            : this(data, index, count)
        {
            ThreadId = threadId;
        }


        private static int TryGetCountFromData(TEntity data)
        {
            var type = typeof(TEntity);

            var getCounter = (type.GetProperty("Count")?.GetValue(data)
                     ?? type.GetProperty("Length")?.GetValue(data)
                     ?? type.GetMethod("Count")?.Invoke(data, null)).Cast<object, int?>();

            var result = getCounter ?? 1;

            return result;
        }
    }
}
