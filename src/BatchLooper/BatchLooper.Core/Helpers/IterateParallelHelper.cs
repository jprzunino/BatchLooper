using BatchLooper.Core.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static BatchLooper.Core.Extensions.EnumerableExtensions;
using static BatchLooper.Core.Extensions.ParallelExtension;
using static BatchLooper.Core.Helpers.MemoryLeakHelper;

namespace BatchLooper.Core.Helpers
{
    public static class IterateParallelHelper
    {
        #region IAsyncEnumerable

        /// <summary>
        /// Execute action in parallel using stream.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The stream collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this IAsyncEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, Task> action)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = new ConcurrentQueue<Exception>();
            await Parallel.ForEachAsync(source: collection, parallelOptions: parallelOptions,
                body: async (entity, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        await action(entity);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel using stream.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The stream collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this IAsyncEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, int, Task> action)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = new ConcurrentQueue<Exception>();
            int idx = 0;

            await Parallel.ForEachAsync(source: collection, parallelOptions: parallelOptions,
                body: async (entity, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        await action(entity, Interlocked.Increment(ref idx));
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel using stream.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The stream collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this IAsyncEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, long, Task> action)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = new ConcurrentQueue<Exception>();

            long idx = 0;
            await Parallel.ForEachAsync(source: collection, parallelOptions: parallelOptions,
                body: async (entity, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        await action(entity, Interlocked.Increment(ref idx));
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        #endregion

        #region IEnumerable

        /// <summary>
        /// Execute action in parallel.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <param name="partitionerOptions">Options to be executed the partition, default is "NoBuffering".</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ConcurrentQueue<Exception> ProcessInParallel<TEntity>([NotNull] this IEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity> action)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = new ConcurrentQueue<Exception>();
            Parallel.ForEach(source: collection, parallelOptions: parallelOptions,
                body: (entity, state, index) =>
                {
                    if (state.SkipInteration(index))
                        return;

                    try
                    {
                        action(entity);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <param name="partitionerOptions">Options to be executed the partition, default is "NoBuffering".</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ConcurrentQueue<Exception> ProcessInParallel<TEntity>([NotNull] this IEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, long> action)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = new ConcurrentQueue<Exception>();
            Parallel.ForEach(source: collection, parallelOptions: parallelOptions,
                body: (entity, state, index) =>
                {
                    if (state.SkipInteration(index))
                        return;

                    try
                    {
                        action(entity, index);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <param name="partitionerOptions">Options to be executed the partition, default is "NoBuffering".</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ConcurrentQueue<Exception> ProcessInParallelWithPartitioner<TEntity>([NotNull] this IEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity> action, EnumerablePartitionerOptions partitionerOptions = EnumerablePartitionerOptions.NoBuffering)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = new ConcurrentQueue<Exception>();
            OrderablePartitioner<TEntity> partitioner = Partitioner.Create(collection, partitionerOptions);

            Parallel.ForEach(source: partitioner, parallelOptions: parallelOptions,
                body: (entity, state, index) =>
                {
                    if (state.SkipInteration(index))
                        return;

                    try
                    {
                        action(entity);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation, with index.</param>
        /// <param name="partitionerOptions">Options to be executed the partition, default is "NoBuffering".</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ConcurrentQueue<Exception> ProcessInParallelWithPartitioner<TEntity>([NotNull] this IEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, long> action, EnumerablePartitionerOptions partitionerOptions = EnumerablePartitionerOptions.NoBuffering)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = new ConcurrentQueue<Exception>();
            OrderablePartitioner<TEntity> partitioner = Partitioner.Create(collection, partitionerOptions);

            Parallel.ForEach(source: partitioner, parallelOptions: parallelOptions,
                body: (entity, state, index) =>
                {
                    if (state.SkipInteration(index))
                        return;

                    try
                    {
                        action(entity, index);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel Async.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this IEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, Task> action)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = new ConcurrentQueue<Exception>();

            await Parallel.ForEachAsync(source: collection, parallelOptions: parallelOptions,
                body: async (entity, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        await action(entity);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel Async.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation, with index.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this IEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, long, Task> action)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = new ConcurrentQueue<Exception>();

            long idx = 0;
            await Parallel.ForEachAsync(source: collection, parallelOptions: parallelOptions,
                body: async (entity, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        await action(entity, Interlocked.Increment(ref idx));
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelWithPartitionerAsync<TEntity>([NotNull] this IEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, Task> action, EnumerablePartitionerOptions partitionerOptions = EnumerablePartitionerOptions.NoBuffering)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = await collection.PartitionerHandleAsync(parallelOptions, action, partitionerOptions);
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to interate, this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelWithPartitionerAsync<TEntity>([NotNull] this IEnumerable<TEntity> collection, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, int, Task> action, EnumerablePartitionerOptions partitionerOptions = EnumerablePartitionerOptions.NoBuffering)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(action);

            var errors = await collection.PartitionerHandleAsync(parallelOptions, action, partitionerOptions);
            return errors;
        }

        #endregion

        #region Memory

        /// <summary>
        /// Execute action in parallel with collection by memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallel<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            Parallel.For(fromInclusive: 0, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: (memoryIndex, state) =>
                {
                    if (state.SkipInteration())
                        return;

                    try
                    {
                        action(memory.Span[memoryIndex]);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with collection by memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallel<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, int> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            Parallel.For(fromInclusive: 0, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: (index, state) =>
                {
                    if (state.SkipInteration())
                        return;

                    try
                    {
                        action(memory.Span[index], index);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with collection by memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallel<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, long> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            Parallel.For(fromInclusive: 0L, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: (index, state) =>
                {
                    if (state.SkipInteration())
                        return;

                    try
                    {
                        action(memory.Span[(int)index], index);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection by memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallelWithPartitioner<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = memory.PartitionerHandle(parallelOptions, action);

            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection by memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallelWithPartitioner<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, int> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = memory.PartitionerHandle(parallelOptions, action);
            return errors;
        }


        /// <summary>
        /// Execute action in parallel with collection by memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            await Parallel.ForAsync(fromInclusive: 0, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: async (memoryIndex, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        await action(memory.Span[memoryIndex]);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with collection by memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, int, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            await Parallel.ForAsync(fromInclusive: 0, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: async (memoryIndex, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        await action(memory.Span[memoryIndex], memoryIndex);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with collection by memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, long, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            await Parallel.ForAsync(fromInclusive: 0L, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: async (memoryIndex, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        await action(memory.Span[(int)memoryIndex], memoryIndex);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection by memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelWithPartitionerAsync<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = await memory.PartitionerHandleAsync(parallelOptions, action);
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection by memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelWithPartitionerAsync<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, int, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = await memory.PartitionerHandleAsync(parallelOptions, action);
            return errors;
        }

        #endregion

        #region ReadOnlyMemory

        /// <summary>
        ///  Execute action in parallel with collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallel<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            Parallel.For(fromInclusive: 0, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: (memoryIndex, state) =>
                {
                    if (state.SkipInteration())
                        return;

                    try
                    {
                        action(memory.Span[memoryIndex]);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        ///  Execute action in parallel with collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallel<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, int> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            Parallel.For(fromInclusive: 0, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: (memoryIndex, state) =>
                {
                    if (state.SkipInteration())
                        return;

                    try
                    {
                        action(memory.Span[memoryIndex], memoryIndex);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        ///  Execute action in parallel with collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallel<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, long> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            Parallel.For(fromInclusive: 0, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: (memoryIndex, state) =>
                {
                    if (state.SkipInteration())
                        return;

                    try
                    {
                        action(memory.Span[memoryIndex], memoryIndex);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallelWithPartitioner<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            var range = Partitioner.Create(0, memory.Length);

            Parallel.ForEach(source: range, parallelOptions: parallelOptions,
                body: (rangePartition, state) =>
                {
                    if (state.SkipInteration())
                        return;

                    try
                    {
                        var current = rangePartition.Item1;
                        do
                        {
                            if (state.SkipInteration())
                                break;

                            action(memory.Span[current]);
                        }
                        while (current < rangePartition.Item2);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallelWithPartitioner<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, int> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            var range = Partitioner.Create(0, memory.Length);

            Parallel.ForEach(source: range, parallelOptions: parallelOptions,
                body: (rangePartition, state) =>
                {
                    if (state.SkipInteration())
                        return;

                    try
                    {
                        var current = rangePartition.Item1;
                        do
                        {
                            if (state.SkipInteration())
                                break;

                            action(memory.Span[current], current++);
                        }
                        while (current < rangePartition.Item2);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static ConcurrentQueue<Exception> ProcessInParallelWithPartitioner<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, long> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            var range = Partitioner.Create(0L, memory.Length);

            Parallel.ForEach(source: range, parallelOptions: parallelOptions,
                body: (rangePartition, state) =>
                {
                    if (state.SkipInteration())
                        return;

                    try
                    {
                        var current = rangePartition.Item1;
                        do
                        {
                            if (state.SkipInteration())
                                break;

                            action(memory.Span[(int)current], current++);
                        }
                        while (current < rangePartition.Item2);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        ///  Execute action in parallel with collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            await Parallel.ForAsync(fromInclusive: 0, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: async (memoryIndex, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        await action(memory.Span[memoryIndex]);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        ///  Execute action in parallel with collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, int, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            await Parallel.ForAsync(fromInclusive: 0, toExclusive: memory.Length, parallelOptions: parallelOptions,
                 body: async (memoryIndex, token) =>
                 {
                     if (token.SkipInteration())
                         return;

                     try
                     {
                         await action(memory.Span[memoryIndex], memoryIndex);
                     }
                     catch (Exception ex)
                     {
                         if (ex is not OperationCanceledException)
                             errors.Enqueue(ex);
                     }
                 });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        ///  Execute action in parallel with collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelAsync<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, long, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            await Parallel.ForAsync(fromInclusive: 0, toExclusive: memory.Length, parallelOptions: parallelOptions,
                body: async (memoryIndex, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        await action(memory.Span[memoryIndex], memoryIndex);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelWithPartitionerAsync<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            var range = Partitioner.Create(0, memory.Length)
                .GetDynamicPartitions()
                .Select<Tuple<int, int>, (int Begin, int End)>(tuple => (tuple.Item1, tuple.Item2))
                .ToAsyncEnumerable();

            await Parallel.ForEachAsync(source: range, parallelOptions: parallelOptions,
                body: async (rangePartition, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        var current = rangePartition.Begin;
                        do
                        {
                            if (token.SkipInteration())
                                break;

                            await action(memory.Span[current]);
                        }
                        while (current < rangePartition.End);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelWithPartitionerAsync<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, int, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            var range = Partitioner.Create(0, memory.Length)
                .GetDynamicPartitions()
                .Select<Tuple<int, int>, (int Begin, int End)>(tuple => (tuple.Item1, tuple.Item2));

            await Parallel.ForEachAsync(source: range, parallelOptions: parallelOptions,
                body: async (rangePartition, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        var current = rangePartition.Begin;
                        do
                        {
                            if (token.SkipInteration())
                                break;

                            await action(memory.Span[current], current++);
                        }
                        while (current < rangePartition.End);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        /// <summary>
        /// Execute action in parallel with partition of collection in readonly memory.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="memory">The collection to interate (in memory, readonly), this is mandatory.</param>
        /// <param name="parallelOptions">The options to parallel process, this is mandatory.</param>
        /// <param name="action">The action to be executed in interation.</param>
        /// <returns>If had errors, retuns all errors.</returns>
        public static async Task<ConcurrentQueue<Exception>> ProcessInParallelWithPartitionerAsync<TEntity>([NotNull] this ReadOnlyMemory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, long, Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(parallelOptions);

            var errors = new ConcurrentQueue<Exception>();
            var range = Partitioner.Create(0L, memory.Length)
                .GetDynamicPartitions()
                .Select<Tuple<long, long>, (long Begin, long End)>(tuple => (tuple.Item1, tuple.Item2));

            await Parallel.ForEachAsync(source: range, parallelOptions: parallelOptions,
                body: async (rangePartition, token) =>
                {
                    if (token.SkipInteration())
                        return;

                    try
                    {
                        var current = rangePartition.Begin;
                        do
                        {
                            if (token.SkipInteration())
                                break;

                            await action(memory.Span[(int)current], current++);
                        }
                        while (current < rangePartition.End);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            errors.Enqueue(ex);
                    }
                });

            FixMemoryLeak();
            return errors;
        }

        #endregion


        /// <summary>
        /// Execute action in parallel asyncronous <see cref="Task.WhenAll"/> to handle bathed executions.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to be batched and interated, this is mandatory.</param>
        /// <param name="batchesInParallel">The count bathes.</param>
        /// <param name="body"> the function that will handle bathes</param>
        public static Task ProcessInParallelWhenAllAsync<TEntity>([NotNull] this IEnumerable<TEntity> collection, int batchesInParallel, Func<TEntity, Task> body, int? degreeOfParallelism = null, ParallelExecutionMode? executionMode = null, ParallelMergeOptions? mergeOptions = null, CancellationToken cancellationToken = default)
        {
            /* bibliografy:
             * https://youtu.be/lHuyl_WTpME?si=nq3vFYwmCmxXhi7q
             */

            ArgumentNullException.ThrowIfNull(collection);
            ArgumentOutOfRangeException.ThrowIfNegative(batchesInParallel);
            ArgumentNullException.ThrowIfNull(body);

            var partitions = Partitioner
                .Create(collection)
                .GetPartitions(batchesInParallel)
                .AsParallelCustom(cancellationToken: cancellationToken, degreeOfParallelism: degreeOfParallelism, executionMode: executionMode, mergeOptions: mergeOptions)
                .Select(s => s.AwaitPartitionAsync(body));

            var result = Task.WhenAll(partitions);

            return result;
        }

        /// <summary>
        /// Execute action in parallel asyncronous <see cref="Task.WhenAll"/> to handle bathed executions.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to be batched and interated, this is mandatory.</param>
        /// <param name="batchesInParallel">The count bathes.</param>
        /// <param name="body"> the function that will handle bathes</param>
        public static Task ProcessInParallelWhenAllAsync<TEntity>([NotNull] this Span<TEntity> collection, int batchesInParallel, Func<TEntity, Task> body, int? degreeOfParallelism = null, ParallelExecutionMode? executionMode = null, ParallelMergeOptions? mergeOptions = null, EnumerablePartitionerOptions partitionerOptions = EnumerablePartitionerOptions.None, CancellationToken cancellationToken = default)
        {
            /* bibliografy:
             * https://youtu.be/lHuyl_WTpME?si=nq3vFYwmCmxXhi7q
             */

            ArgumentOutOfRangeException.ThrowIfNegative(batchesInParallel);
            ArgumentNullException.ThrowIfNull(body);

            var partitions = Partitioner
                .Create([.. collection], partitionerOptions)
                .GetPartitions(batchesInParallel)
                .AsParallelCustom(cancellationToken: cancellationToken, degreeOfParallelism: degreeOfParallelism, executionMode: executionMode, mergeOptions: mergeOptions)
                .Select(s => s.AwaitPartitionAsync(body));

            var result = Task.WhenAll(partitions);

            return result;
        }

        /// <summary>
        /// Execute action in parallel asyncronous <see cref="Task.WhenAll"/> to handle bathed executions.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to be batched and interated, this is mandatory.</param>
        /// <param name="batchesInParallel">The count bathes.</param>
        /// <param name="body"> the function that will handle bathes</param>
        public static Task ProcessInParallelWhenAllAsync<TEntity>([NotNull] this ReadOnlySpan<TEntity> collection, int batchesInParallel, Func<TEntity, Task> body, int? degreeOfParallelism = null, ParallelExecutionMode? executionMode = null, ParallelMergeOptions? mergeOptions = null, EnumerablePartitionerOptions partitionerOptions = EnumerablePartitionerOptions.None, CancellationToken cancellationToken = default)
        {
            /* bibliografy:
             * https://youtu.be/lHuyl_WTpME?si=nq3vFYwmCmxXhi7q
             */

            ArgumentOutOfRangeException.ThrowIfNegative(batchesInParallel);
            ArgumentNullException.ThrowIfNull(body);

            var partitions = Partitioner
                .Create([.. collection], partitionerOptions)
                .GetPartitions(batchesInParallel)
                .AsParallelCustom(cancellationToken: cancellationToken, degreeOfParallelism: degreeOfParallelism, executionMode: executionMode, mergeOptions: mergeOptions)
                .Select(s => s.AwaitPartitionAsync(body));

            var result = Task.WhenAll(partitions);

            return result;
        }

        /// <summary>
        /// Execute action in parallel asyncronous <see cref="Task.WhenAll"/> to handle bathed executions.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to be batched and interated, this is mandatory.</param>
        /// <param name="batchesInParallel">The count bathes.</param>
        /// <param name="body"> the function that will handle bathes</param>
        public static Task ProcessInParallelWhenAllAsync<TEntity>([NotNull] this Memory<TEntity> collection, int batchesInParallel, Func<TEntity, Task> body, int? degreeOfParallelism = null, ParallelExecutionMode? executionMode = null, ParallelMergeOptions? mergeOptions = null, EnumerablePartitionerOptions partitionerOptions = EnumerablePartitionerOptions.None, CancellationToken cancellationToken = default) => collection.Span.ProcessInParallelWhenAllAsync(batchesInParallel, body, degreeOfParallelism, executionMode, mergeOptions, partitionerOptions, cancellationToken);

        /// <summary>
        /// Execute action in parallel asyncronous <see cref="Task.WhenAll"/> to handle bathed executions.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">The collection to be batched and interated, this is mandatory.</param>
        /// <param name="batchesInParallel">The count bathes.</param>
        /// <param name="body"> the function that will handle bathes</param>
        public static Task ProcessInParallelWhenAllAsync<TEntity>([NotNull] this ReadOnlyMemory<TEntity> collection, int batchesInParallel, Func<TEntity, Task> body, int? degreeOfParallelism = null, ParallelExecutionMode? executionMode = null, ParallelMergeOptions? mergeOptions = null, EnumerablePartitionerOptions partitionerOptions = EnumerablePartitionerOptions.None, CancellationToken cancellationToken = default) => collection.Span.ProcessInParallelWhenAllAsync(batchesInParallel, body, degreeOfParallelism, executionMode, mergeOptions, partitionerOptions, cancellationToken);

        private static async Task AwaitPartitionAsync<TEntity>([NotNull] this IEnumerator<TEntity> partition, [NotNull] Func<TEntity, Task> body)
        {
            ArgumentNullException.ThrowIfNull(partition);
            ArgumentNullException.ThrowIfNull(body);

            using (partition)
                while (partition.MoveNext())
                    await body(partition.Current);
        }

        /// <summary>
        /// Sample of usage <see cref="ProcessInParallelWhenAllAsync"/>
        /// </summary>
        /// <param name="batches"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<int>> ProcessInParallelWhenAllSampleAsync(int batches)
        {
            var list = new List<int>();

            var tasks = Enumerable.Range(0, 100/* Interations/execution inside batch processing*/)
                .Select(s => new Func<Task<int>>(() => Task.FromResult(s)/* that could be an API call then return number too.*/))
                .ToList();

            await tasks.ProcessInParallelWhenAllAsync(batches, async func => list.Add(await func()));

            return list;
        }

        private static ConcurrentQueue<Exception> PartitionerHandle<TEntity>([NotNull] this IEnumerable<TEntity> source, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity> action)
        {
            var errors = new ConcurrentQueue<Exception>();
            try
            {
                var range = Partitioner.Create(source);

                Parallel.ForEach(source: range, parallelOptions: parallelOptions,
                body: (entity, state, index) =>
                {
                    if (state.SkipInteration(index))
                        return;

                    try
                    {
                        action(entity);
                    }
                    catch (Exception ex) when (ex is OperationCanceledException canceledException) { }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    errors.Enqueue(ex);
            }
            finally
            {
                FixMemoryLeak();
            }

            return errors;
        }

        private static ConcurrentQueue<Exception> PartitionerHandle<TEntity>([NotNull] this IEnumerable<TEntity> source, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, int> action)
        {
            var errors = new ConcurrentQueue<Exception>();
            try
            {
                OrderablePartitioner<TEntity> range = Partitioner.Create(source);

                Parallel.ForEach(source: range, parallelOptions: parallelOptions,
                body: (entity, state, index) =>
                {
                    if (state.SkipInteration(index))
                        return;

                    try
                    {
                        action(entity, index.Cast<long, int>());
                    }
                    catch (Exception ex) when (ex is OperationCanceledException canceledException) { }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    errors.Enqueue(ex);
            }
            finally
            {
                FixMemoryLeak();
            }

            return errors;
        }

        private static ConcurrentQueue<Exception> PartitionerHandle<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity> action)
        {
            var errors = new ConcurrentQueue<Exception>();
            try
            {
                var range = Partitioner.Create(0, memory.Length)
                .GetDynamicPartitions()
                .Select<Tuple<int, int>, (int Begin, int End)>(tuple => (tuple.Item1, tuple.Item2));

                var actions = range
                    .Where(x => x.Begin >= 0 && x.End > 0 && x.Begin < x.End)
                    .Select(rangePartition =>
                        ParallelEnumerable.Range(rangePartition.Begin, rangePartition.End - rangePartition.Begin)
                        .Select<int, Action>(current => () =>
                        {
                            if (parallelOptions.CancellationToken.IsCancellationRequested)
                                return;

                            try
                            {
                                action(memory.Span[current]);
                            }
                            catch (Exception ex)
                            {
                                if (ex is not OperationCanceledException)
                                    errors.Enqueue(ex);
                            }
                        }))
                    .SelectMany(s => s);

                Parallel.Invoke(parallelOptions: parallelOptions, actions: actions.ToPooledArray());
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    errors.Enqueue(ex);
            }
            finally
            {
                FixMemoryLeak();
            }

            return errors;
        }

        private static ConcurrentQueue<Exception> PartitionerHandle<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Action<TEntity, int> action)
        {
            var errors = new ConcurrentQueue<Exception>();
            try
            {
                var range = Partitioner.Create(0, memory.Length)
                .GetDynamicPartitions()
                .Select<Tuple<int, int>, (int Begin, int End)>(tuple => (tuple.Item1, tuple.Item2));

                var actions = range
                    .Where(x => x.Begin >= 0 && x.End > 0 && x.Begin < x.End)
                    .Select(rangePartition =>
                        ParallelEnumerable.Range(rangePartition.Begin, rangePartition.End - rangePartition.Begin)
                        .Select<int, Action>(current => () =>
                        {
                            if (parallelOptions.CancellationToken.IsCancellationRequested)
                                return;

                            try
                            {
                                action(memory.Span[current], current);
                            }
                            catch (Exception ex)
                            {
                                if (ex is not OperationCanceledException)
                                    errors.Enqueue(ex);
                            }
                        }))
                    .SelectMany(s => s);

                Parallel.Invoke(parallelOptions: parallelOptions, actions: actions.ToPooledArray());
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    errors.Enqueue(ex);
            }
            finally
            {
                FixMemoryLeak();
            }

            return errors;
        }

        private static async Task<ConcurrentQueue<Exception>> PartitionerHandleAsync<TEntity>([NotNull] this IEnumerable<TEntity> source, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, Task> action, [NotNull] EnumerablePartitionerOptions partitionerOptions = default)
        {
            var errors = new ConcurrentQueue<Exception>();
            try
            {
                var range = Partitioner.Create(source, partitionerOptions);

                await Parallel.ForEachAsync(source: range.GetDynamicPartitions(), parallelOptions: parallelOptions,
                 body: async (entity, token) =>
                 {
                     try
                     {
                         if (token.SkipInteration())
                             return;

                         await action(entity);
                     }
                     catch (Exception ex) when (ex is OperationCanceledException canceledException) { }
                     catch (Exception ex)
                     {
                         errors.Enqueue(ex);
                     }
                 });
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    errors.Enqueue(ex);
            }
            finally
            {
                FixMemoryLeak();
            }

            return errors;
        }

        private static async Task<ConcurrentQueue<Exception>> PartitionerHandleAsync<TEntity>([NotNull] this IEnumerable<TEntity> source, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, int, Task> action, [NotNull] EnumerablePartitionerOptions partitionerOptions = default)
        {
            var errors = new ConcurrentQueue<Exception>();
            try
            {
                OrderablePartitioner<TEntity> range = Partitioner.Create(source, partitionerOptions);

                await Parallel.ForEachAsync(source: range.GetOrderableDynamicPartitions(), parallelOptions: parallelOptions,
                 body: async (manager, token) =>
                 {
                     try
                     {
                         if (token.SkipInteration())
                             return;

                         await action(manager.Value, manager.Key.Cast<long, int>());
                     }
                     catch (Exception ex) when (ex is not OperationCanceledException) { errors.Enqueue(ex); }
                 });
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    errors.Enqueue(ex);
            }
            finally
            {
                FixMemoryLeak();
            }

            return errors;
        }

        private static Task<ConcurrentQueue<Exception>> PartitionerHandleAsync<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, Task> action)
        {
            var errors = new ConcurrentQueue<Exception>();
            try
            {
                var range = Partitioner.Create(0, memory.Length)
                .GetOrderableDynamicPartitions()
                .Select<KeyValuePair<long, Tuple<int, int>>, (int Begin, int End)>(order => (order.Value.Item1, order.Value.Item2));

                var actions = range
                    .Where(x => x.Begin >= 0 && x.End > 0 && x.Begin < x.End)
                    .Select(rangePartition =>
                        ParallelEnumerable.Range(rangePartition.Begin, rangePartition.End - rangePartition.Begin)
                        .Select<int, Action>(current => async () =>
                        {
                            if (parallelOptions.CancellationToken.IsCancellationRequested)
                                return;

                            try
                            {
                                await action(memory.Span[current]);
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException) { errors.Enqueue(ex); }
                        }))
                    .SelectMany(s => s);

                Parallel.Invoke(parallelOptions: parallelOptions, actions: actions.ToPooledArray());
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    errors.Enqueue(ex);
            }
            finally
            {
                FixMemoryLeak();
            }

            return Task.FromResult(errors);
        }

        private static Task<ConcurrentQueue<Exception>> PartitionerHandleAsync<TEntity>([NotNull] this Memory<TEntity> memory, [NotNull] ParallelOptions parallelOptions, [NotNull] Func<TEntity, int, Task> action)
        {
            var errors = new ConcurrentQueue<Exception>();
            try
            {
                var range = Partitioner.Create(0, memory.Length)
                .GetDynamicPartitions()
                .Select<Tuple<int, int>, (int Begin, int End)>(tuple => (tuple.Item1, tuple.Item2));

                var actions = range
                    .Where(x => x.Begin >= 0 && x.End > 0 && x.Begin < x.End)
                    .Select(rangePartition =>
                        ParallelEnumerable.Range(rangePartition.Begin, rangePartition.End - rangePartition.Begin)
                        .Select<int, Action>(current => async () =>
                        {
                            if (parallelOptions.CancellationToken.IsCancellationRequested)
                                return;

                            try
                            {
                                await action(memory.Span[current], current);
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException) { errors.Enqueue(ex); }
                        }))
                    .SelectMany(s => s);

                Parallel.Invoke(parallelOptions: parallelOptions, actions: actions.ToPooledArray());
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    errors.Enqueue(ex);
            }
            finally
            {
                FixMemoryLeak();
            }

            return Task.FromResult(errors);
        }
    }
}
