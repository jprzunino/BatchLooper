using BatchLooper.Core.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BatchLooper.Core.Helpers
{
    /// <summary>
    /// Attention: Span and Unsafe do not work when source mutate, because their anchor in reference to allocate memory.
    /// </summary>
    public static class IterateHelper
    {
        //https://www.youtube.com/watch?v=cwBrWn4m9y8
        //https://www.youtube.com/watch?v=jUZ3VKFyB-Ahttps://www.youtube.com/watch?v=jUZ3VKFyB-A
        //https://www.youtube.com/watch?v=KLtMtxUihBk

        public static void InterateUnsafeWithForByArrayDataReference<TValue>([NotNull] this IEnumerable<TValue> source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(handle);

            ref var searchSpace = ref MemoryMarshal.GetArrayDataReference(source.ToPooledArray());
            for (int i = 0; i < source.Count(); i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                TValue item = Unsafe.Add(ref searchSpace, i);
                handle(item);
            }
        }

        public static void IterateUnsafeWithForByMemoryMarshalReference<TValue>([NotNull] this IEnumerable<TValue> source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(handle);

            ref var searchSpace = ref MemoryMarshal.GetReference<TValue>(source.ToPooledArray());
            for (int i = 0; i < source.Count(); i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                TValue item = Unsafe.Add(ref searchSpace, i);
                handle(item);
            }
        }

        public static void IterateUnsafeWithWhileByArrayDataReference<TValue>([NotNull] this IEnumerable<TValue> source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(handle);

            ref var start = ref MemoryMarshal.GetArrayDataReference(source.ToPooledArray());
            ref var end = ref Unsafe.Add(ref start, source.Count());

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                if (skipAction is not null && skipAction())
                    break;

                handle(start);
                start = ref Unsafe.Add(ref start, 1)!;
            }
        }

        public static void IterateSpan<TValue>([NotNull] this IEnumerable<TValue> source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(handle);

            source
                .ToPooledArray()
                .IterateSpan(handle, skipAction);
        }

        public static void IterateSpan<TValue>([NotNull] this TValue[] source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(handle);

            var spanCollection = source.AsSpan();
            for (var i = 0; i < spanCollection.Length; i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                handle(spanCollection[i]);
            }
        }

        public static void IterateSpan<TValue>([NotNull] this List<TValue> source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(handle);

            var spanCollection = CollectionsMarshal.AsSpan(source);
            for (var i = 0; i < spanCollection.Length; i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                handle(spanCollection[i]);
            }
        }

        public static void IterateSpan<TValue>([NotNull] this Span<TValue> source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(handle);

            for (var i = 0; i < source.Length; i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                handle(source[i]);
            }
        }

        public static void IterateSpan<TValue>([NotNull] this ReadOnlySpan<TValue> source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(handle);

            for (var i = 0; i < source.Length; i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                handle(source[i]);
            }
        }

        public static void IterateSpanAndUnsafe<TValue>([NotNull] this IEnumerable<TValue> source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(handle);


            Span<TValue> lstAsSpan = source switch
            {
                TValue[] array => array,
                List<TValue> list => CollectionsMarshal.AsSpan(list),
                _ => source.ToPooledArray()
            };
            ref var searchSpace = ref MemoryMarshal.GetReference(lstAsSpan);

            for (var i = 0; i < lstAsSpan.Length; i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                var item = Unsafe.Add(ref searchSpace, i);
                handle(item);
            }
        }

        public static void IterateSpanAndUnsafe<TValue>([NotNull] this Span<TValue> source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(handle);

            ref var searchSpace = ref MemoryMarshal.GetReference(source);

            for (var i = 0; i < source.Length; i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                var item = Unsafe.Add(ref searchSpace, i);
                handle(item);
            }
        }

        public static void IterateSpanAndUnsafe<TValue>([NotNull] this ReadOnlySpan<TValue> source, [NotNull] Action<TValue> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(handle);

            ref var searchSpace = ref MemoryMarshal.GetReference(source);

            for (var i = 0; i < source.Length; i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                var item = Unsafe.Add(ref searchSpace, i);
                handle(item);
            }
        }

        public static Task IterateSpanAndUnsafeAsync<TValue>([NotNull] this IEnumerable<TValue> source, [NotNull] Func<TValue, Task> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(handle);

            Span<TValue> lstAsSpan = source switch
            {
                TValue[] array => array,
                List<TValue> list => CollectionsMarshal.AsSpan(list),
                _ => source.ToPooledArray()
            };
            ref var searchSpace = ref MemoryMarshal.GetReference(lstAsSpan);

            for (var i = 0; i < lstAsSpan.Length; i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                var item = Unsafe.Add(ref searchSpace, i);
                handle(item).Sync();
            }

            return Task.CompletedTask;
        }

        public static Task IterateSpanAndUnsafeAsync<TValue>([NotNull] this Span<TValue> source, [NotNull] Func<TValue, Task> handle, Func<bool>? skipAction = null)
        {
            ArgumentNullException.ThrowIfNull(handle);

            ref var searchSpace = ref MemoryMarshal.GetReference(source);

            for (var i = 0; i < source.Length; i++)
            {
                if (skipAction is not null && skipAction())
                    break;

                var item = Unsafe.Add(ref searchSpace, i);
                handle(item)/*.Sync(ConfigureAwaitOptions.None)*/;
            }

            return Task.CompletedTask;
        }
    }
}
