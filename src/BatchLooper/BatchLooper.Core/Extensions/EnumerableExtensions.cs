using BatchLooper.Core.Helpers;
using System.Buffers;

namespace BatchLooper.Core.Extensions
{
    public static class EnumerableExtensions
    {
        public static TEntity[] ToPooledArray<TEntity>(this IEnumerable<TEntity> source)
        {
            var count = source switch
            {
                TEntity[] a => a.Length,
                IList<TEntity> l => l.Count,
                ICollection<TEntity> c => c.Count,
                _ => source.Count()
            };

            var pool = ArrayPool<TEntity>.Shared;
            var buffer = pool.Rent(count);

            int i = 0;

            source.IterateSpanAndUnsafe(item => buffer[i++] = item);

            var result = new TEntity[i];
            Array.Copy(buffer, result, i);
            pool.Return(buffer, clearArray: true);

            return result;
        }
    }
}
