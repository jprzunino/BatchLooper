using BatchLooper.Core.Enumerations;
using BatchLooper.Core.Interfaces.Batcher;
using BatchLooper.Core.Models.Batcher;
using BatchLooper.Infrastructure.Services.Batcher;
using BatchLooper.Infrastructure.Services.Batcher.WithSerilog;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using static BatchLooper.Core.Helpers.SyncTaskHelper;

namespace BatchLooper.Sample
{
    public static class BatcherBuilderSample
    {
        public static class WithCustomsSamples
        {
            private static readonly ConfigureAwaitOptions _defaultConfigureAwait = ConfigureAwaitOptions.None;

            #region Execution

            public static BatcherBuilderResult ExecutionSample<TEntity>([NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<TEntity>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilder<TEntity>(configuration);

                var result = builder.ConsoleActionHandleAsync(configuration, cts,
                    action: builder => builder.Execute(item =>
                    {
                        if (throwError is not null && throwError(item))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            Thread.Sleep(waitTime.Value);
                    }, skipHandle: skipHandle))
                    .Sync(_defaultConfigureAwait);

                return result;
            }

            public static BatcherBuilderResult ExecutionSerilogSample<TEntity>([NotNull] ILogger logger, [NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<TEntity>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilderSerilog<TEntity>(logger, configuration);

                var result = builder.ConsoleActionHandleAsync(configuration, cts,
                    action: bulder => builder.Execute(item =>
                    {
                        if (throwError is not null && throwError(item))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            Thread.Sleep(waitTime.Value);
                    }, skipHandle: skipHandle))
                    .Sync(_defaultConfigureAwait);

                return result;
            }

            public static async Task<BatcherBuilderResult> ExecutionSampleAsync<TEntity>([NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<TEntity>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilder<TEntity>(configuration);

                var result = await builder.ConsoleActionHandleAsync(configuration, cts,
                    action: async (builder, cancellationToken) => await builder.ExecuteAsync(async item =>
                    {
                        if (throwError is not null && throwError(item))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            await Task.Delay(waitTime.Value);
                        else
                            await Task.CompletedTask;
                    }, skipHandle: skipHandle,
                cancellationToken: cancellationToken));

                return result;
            }

            public static async Task<BatcherBuilderResult> ExecutionSerilogSampleAsync<TEntity>([NotNull] ILogger logger, [NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<TEntity>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilderSerilog<TEntity>(logger, configuration);

                var result = await builder.ConsoleActionHandleAsync(configuration, cts,
                    action: async (builder, cancellationToken) => await builder.ExecuteAsync(async item =>
                    {
                        if (throwError is not null && throwError(item))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            await Task.Delay(waitTime.Value);
                        else
                            await Task.CompletedTask;
                    }, skipHandle: skipHandle,
                cancellationToken: cancellationToken));

                return result;
            }

            public static BatcherBuilderResult ExecutionCollectionSample<TEntity>([NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<IEnumerable<TEntity>>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilder<TEntity>(configuration);

                var result = builder.ConsoleActionHandleAsync(configuration, cts,
                    action: builder => builder.ExecuteCollection(batch =>
                    {
                        if (throwError is not null && throwError(batch))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            Thread.Sleep(waitTime.Value);
                    }, skipHandle: skipHandle))
                    .Sync(_defaultConfigureAwait);

                return result;
            }

            public static async Task<BatcherBuilderResult> ExecutionCollectionSampleAsync<TEntity>([NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<IEnumerable<TEntity>>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilder<TEntity>(configuration);

                var result = await builder.ConsoleActionHandleAsync(configuration, cts,
                    action: async (builder, cancellationToken) => await builder.ExecuteCollectionAsync(async batch =>
                    {
                        if (throwError is not null && throwError(batch))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            await Task.Delay(waitTime.Value);
                        else
                            await Task.CompletedTask;
                    }, skipHandle: skipHandle,
                cancellationToken: cancellationToken));

                return result;
            }

            #endregion

            #region ExecuteWithBatchItemInfo

            public static BatcherBuilderResult ExecutionWithBatchItemInfoSample<TEntity>([NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<TEntity>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilder<TEntity>(configuration);

                var result = builder.ConsoleActionHandleAsync(configuration, cts,
                    action: builder => builder.ExecuteWithBatchItemInfo(itemInfo =>
                    {
                        var content = itemInfo.Data!;

                        if (throwError is not null && throwError(content))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            Thread.Sleep(waitTime.Value);
                    }, skipHandle: skipHandle))
                    .Sync(_defaultConfigureAwait);

                return result;
            }

            public static async Task<BatcherBuilderResult> ExecutionWithBatchItemInfoSampleAsync<TEntity>([NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<TEntity>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilder<TEntity>(configuration);

                var result = await builder.ConsoleActionHandleAsync(configuration, cts,
                    action: async (builder, cancellationToken) => await builder.ExecuteWithBatchItemInfoAsync(async itemInfo =>
                    {
                        var content = itemInfo.Data!;

                        if (throwError is not null && throwError(content))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */

                        if (waitTime.HasValue)
                            await Task.Delay(waitTime.Value);
                        else
                            await Task.CompletedTask;
                    }, skipHandle: skipHandle,
                cancellationToken: cancellationToken));

                return result;
            }

            public static BatcherBuilderResult ExecutionWithBatchItemInfoCollectionSample<TEntity>([NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<IEnumerable<TEntity>>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilder<TEntity>(configuration);

                var result = builder.ConsoleActionHandleAsync(configuration, cts,
                    action: builder => builder.ExecuteWithBatchItemInfoCollection(itemsInfo =>
                    {
                        var batch = itemsInfo
                        .Select(s => s.Data!)
                        .ToList();

                        if (throwError is not null && throwError(batch))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            Thread.Sleep(waitTime.Value);
                    }, skipHandle: skipHandle))
                    .Sync(_defaultConfigureAwait);

                return result;
            }

            public static async Task<BatcherBuilderResult> ExecutionWithBatchItemInfoCollectionSampleAsync<TEntity>([NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<IEnumerable<TEntity>>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilder<TEntity>(configuration);

                var result = await builder.ConsoleActionHandleAsync(configuration, cts,
                    action: async (builder, cancellationToken) => await builder.ExecuteWithBatchItemInfoCollectionAsync(async itemsInfo =>
                    {
                        var batch = itemsInfo
                         .Select(s => s.Data!)
                         .ToList();

                        if (throwError is not null && throwError(batch))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            await Task.Delay(waitTime.Value);
                        else
                            await Task.CompletedTask;
                    }, skipHandle: skipHandle,
                cancellationToken: cancellationToken));

                return result;
            }

            #endregion

            #region ExecuteWithBatchInfo

            public static BatcherBuilderResult ExecutionWithBatchInfoCollectionSample<TEntity>([NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<IEnumerable<TEntity>>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilder<TEntity>(configuration);

                var result = builder.ConsoleActionHandleAsync(configuration, cts,
                    action: builder => builder.ExecuteWithBatchInfoCollection(batchInfo =>
                    {
                        List<TEntity> batch = [..batchInfo.Collection
                        .ToArray()
                        .Select(s => s.Data!)];

                        if (throwError is not null && throwError(batch))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            Thread.Sleep(waitTime.Value);
                    }, skipHandle: skipHandle))
                    .Sync(_defaultConfigureAwait);

                return result;
            }

            public static async Task<BatcherBuilderResult> ExecutionWithBatchInfoCollectionSampleAsync<TEntity>([NotNull] IBatcherConfiguration<TEntity> configuration, TimeSpan? waitTime = null, Func<bool>? skipHandle = null, Predicate<IEnumerable<TEntity>>? throwError = null, CancellationTokenSource? cts = null)
            {
                ArgumentNullException.ThrowIfNull(configuration);

                IBatcherBuilder<TEntity> builder = new BatcherBuilder<TEntity>(configuration);

                var result = await builder.ConsoleActionHandleAsync(configuration, cts,
                    action: async (builder, cancellationToken) => await builder.ExecuteWithBatchInfoCollectionAsync(async batchInfo =>
                    {
                        List<TEntity> batch = [.. batchInfo.Collection
                        .ToArray()
                        .Select(s => s.Data!)];

                        if (throwError is not null && throwError(batch))
                            throw new Exception("Test error fired!!");

                        /* Action to Process item */
                        if (waitTime.HasValue)
                            await Task.Delay(waitTime.Value);
                        else
                            await Task.CompletedTask;
                    }, skipHandle: skipHandle,
                cancellationToken: cancellationToken));

                return result;
            }

            #endregion
        }

        /// <summary>
        /// Extension to handle action in console app.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="builder">The Interator collection.</param>
        /// <param name="configuration">The configuration used to interate colletion </param>
        /// <param name="cts"> The cancellation tohen source.</param>
        /// <param name="action">The action taken in interation.</param>
        /// <returns>The results of interation</returns>
        private static async Task<BatcherBuilderResult> ConsoleActionHandleAsync<TEntity>(this IBatcherBuilder<TEntity> builder, IBatcherConfiguration<TEntity> configuration, CancellationTokenSource? cts, Func<IBatcherBuilder<TEntity>, CancellationToken, Task<BatcherBuilderResult>> action)
        {
            BatcherBuilderResult result = new();

            using var actionCrawller = Task.Factory.StartNew(async () =>
            {
                Console.WriteLine("Pressione 'C' para cancelar...");

                if (configuration.PrintProgressManual)
                    Console.WriteLine("Pressione 'I' para informações de Execução.");

                while (cts is not null && !cts.Token.IsCancellationRequested)
                {
                    if (result is { Status: BatcherBuilderStatus.WithErrors or BatcherBuilderStatus.Canceled or BatcherBuilderStatus.Success })
                        break;

                    var key = Console.ReadKey(intercept: true);

                    if (configuration.PrintProgressManual && key.Key == ConsoleKey.I)
                        builder.PrintStaticInfo();

                    if (key.Key == ConsoleKey.C)
                    {
                        await cts.CancelAsync();
                        break;
                    }
                }

                return Task.CompletedTask;
            }, cts?.Token ?? default);

            result = await action(builder, cts?.Token ?? default);

            if (cts is not null)
                await cts.CancelAsync();

            await actionCrawller;

            return result;
        }

        /// <summary>
        /// Extension to handle action in console app.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="builder">The Interator collection.</param>
        /// <param name="configuration">The configuration used to interate colletion </param>
        /// <param name="cts"> The cancellation tohen source.</param>
        /// <param name="action">The action taken in interation.</param>
        /// <returns>The results of interation</returns>
        private static async Task<BatcherBuilderResult> ConsoleActionHandleAsync<TEntity>(this IBatcherBuilder<TEntity> builder, IBatcherConfiguration<TEntity> configuration, CancellationTokenSource? cts, Func<IBatcherBuilder<TEntity>, BatcherBuilderResult> action)
        {
            BatcherBuilderResult result = new();

            using var actionCrawller = Task.Factory.StartNew(async () =>
            {
                Console.WriteLine("Pressione 'C' para cancelar...");

                if (configuration.PrintProgressManual)
                    Console.WriteLine("Pressione 'I' para informações de Execução.");

                while (cts is not null && !cts.Token.IsCancellationRequested)
                {
                    if (result is { Status: BatcherBuilderStatus.WithErrors or BatcherBuilderStatus.Canceled or BatcherBuilderStatus.Success })
                        break;

                    var key = Console.ReadKey(intercept: true);

                    if (configuration.PrintProgressManual && key.Key == ConsoleKey.I)
                        builder.PrintStaticInfo();

                    if (key.Key == ConsoleKey.C)
                    {
                        await cts.CancelAsync();
                        break;
                    }
                }

                return Task.CompletedTask;
            }, cts?.Token ?? default);

            result = action(builder);

            if (cts is not null)
                await cts.CancelAsync();

            await actionCrawller;

            return result;
        }

    }
}
