namespace BatchLooper.Core.Helpers
{
    public static class PaginationHelper
    {

        public static IEnumerable<TValue> Paginate<TValue>(this IEnumerable<TValue> source, int pageIndex, int pageSize)
            where TValue : class
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(pageSize);

            var result = source
                .Skip(pageIndex)
                .Take(pageSize)
                .ToList();

            return result;
        }

        public static IEnumerable<IEnumerable<TValue>> Paginate<TValue>(this IEnumerable<TValue> source, int pageSize)
            where TValue : class
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfNegative(pageSize);

            var result = source.Any() ?
                source.Chunk(pageSize) :
                [];

            return result;
        }

        public static IEnumerable<(int BatchCount, int BatchSize)> GetPaginatePartitionPossibility<TValue>(this IEnumerable<TValue> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var total = source.Count();
            var maxLimit = total - 1;

            var result = total > 0 ?
                source.GetPaginatePartitionPossibility(maxLimit) :
               [];

            return result;
        }

        public static IEnumerable<(int BatchCount, int BatchSize)> GetPaginatePartitionPossibility<TValue>(this IEnumerable<TValue> source, int maxLimit)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (maxLimit <= 0)
                throw new Exception("Max limit could not be 0 or less.");

            var total = source.Count();
            var result = Enumerable.Range(2, maxLimit)
                .Where(batch => (total % batch) == 0)
                .Select(batch => { return (batch, total / batch); })
                .ToList();

            return result;
        }
    }
}
