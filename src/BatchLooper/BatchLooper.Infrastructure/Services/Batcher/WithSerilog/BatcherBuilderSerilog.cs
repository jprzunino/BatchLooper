using BatchLooper.Core.Interfaces.Batcher;
using BatchLooper.Core.Interfaces.Batcher.Engine;
using BatchLooper.Core.Interfaces.Batcher.Printers;
using BatchLooper.Core.Interfaces.Patterns;
using BatchLooper.Core.Models.Batcher;
using BatchLooper.Core.Patterns;
using Serilog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Infrastructure.Services.Batcher.WithSerilog
{
    [DebuggerDisplay("BatcherBuilderSerilog: {_configurator?.BatchCount} x {_configurator?.BatchSize} | Parallelism = {_configurator?.MaxDegreeOfParallelism} | MaxTries = {_configurator?.MaxTries} | WaitTime = {_configurator?.WaitTime} (s)")]
    public class BatcherBuilderSerilog<TEntity> : IBatcherBuilder<TEntity>
    {
        #region Variables

        private protected IBatcherConfiguration<TEntity> _configurator;
        private protected IBatchExecutionEngine<TEntity> _engine;
        private protected IBatchProgressPrinterSerilog<TEntity> _printer;
        private protected IBatchStatistics<TEntity> _statistics;
        private readonly ISemaphorePattern _semaphore;
        private readonly ILogger _logger;
        private readonly IBatcherBuilder<TEntity> _recursive;

        #endregion

        #region Initializers

        private BatcherBuilderSerilog([NotNull] ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurator = default!;
            _engine = default!;
            _printer = default!;
            _statistics = default!;
            _semaphore = new SemaphorePattern();

            _recursive = this;
        }

        public BatcherBuilderSerilog([NotNull] ILogger logger, [NotNull] IBatcherConfiguration<TEntity> configuration)
            : this(logger) => _recursive.WithConfigurations(configuration ?? throw new ArgumentNullException(nameof(configuration)));

        public BatcherBuilderSerilog([NotNull] ILogger logger, [NotNull] IEnumerable<TEntity> source)
            : this(logger, new BatcherConfiguration<TEntity>(source)) { }

        #endregion

        #region Functions And Routines

        #region Publics

        #region Execute

        BatcherBuilderResult IBatchExecutionEngineItem<TEntity>.Execute([NotNull] Action<TEntity> handler, Func<bool>? skipHandle)
        {
            ArgumentNullException.ThrowIfNull(_engine);
            ArgumentNullException.ThrowIfNull(handler);

            var result = _engine.Execute(handler, skipHandle);

            return result;
        }

        async Task<BatcherBuilderResult> IBatchExecutionEngineItem<TEntity>.ExecuteAsync([NotNull] Func<TEntity, Task> handler, Func<bool>? skipHandle, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_engine);
            ArgumentNullException.ThrowIfNull(handler);

            var result = await _engine.ExecuteAsync(handler, skipHandle, cancellationToken);

            return result;
        }

        BatcherBuilderResult IBatchExecutionEngineCollection<TEntity>.ExecuteCollection([NotNull] Action<IEnumerable<TEntity>> handler, Func<bool>? skipHandle)
        {
            ArgumentNullException.ThrowIfNull(_engine);
            ArgumentNullException.ThrowIfNull(handler);

            var result = _engine.ExecuteCollection(handler, skipHandle);

            return result;
        }

        async Task<BatcherBuilderResult> IBatchExecutionEngineCollection<TEntity>.ExecuteCollectionAsync([NotNull] Func<IEnumerable<TEntity>, Task> handler, Func<bool>? skipHandle, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_engine);
            ArgumentNullException.ThrowIfNull(handler);

            var result = await _engine.ExecuteCollectionAsync(handler, skipHandle, cancellationToken);

            return result;
        }

        #endregion

        #region ExecuteWithBatchItemInfo

        BatcherBuilderResult IBatchExecutionEngineItem<TEntity>.ExecuteWithBatchItemInfo([NotNull] Action<BatchItemInfoModel<TEntity>> handler, Func<bool>? skipHandle)
        {
            ArgumentNullException.ThrowIfNull(_engine);
            ArgumentNullException.ThrowIfNull(handler);

            var result = _engine.ExecuteWithBatchItemInfo(handler, skipHandle);

            return result;
        }

        async Task<BatcherBuilderResult> IBatchExecutionEngineItem<TEntity>.ExecuteWithBatchItemInfoAsync([NotNull] Func<BatchItemInfoModel<TEntity>, Task> handler, Func<bool>? skipHandle, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_engine);
            ArgumentNullException.ThrowIfNull(handler);

            var result = await _engine.ExecuteWithBatchItemInfoAsync(handler, skipHandle, cancellationToken);

            return result;
        }

        BatcherBuilderResult IBatchExecutionEngineCollection<TEntity>.ExecuteWithBatchItemInfoCollection([NotNull] Action<IEnumerable<BatchItemInfoModel<TEntity>>> handler, Func<bool>? skipHandle)
        {
            ArgumentNullException.ThrowIfNull(_engine);
            ArgumentNullException.ThrowIfNull(handler);

            var result = _engine.ExecuteWithBatchItemInfoCollection(handler, skipHandle);

            return result;
        }

        async Task<BatcherBuilderResult> IBatchExecutionEngineCollection<TEntity>.ExecuteWithBatchItemInfoCollectionAsync([NotNull] Func<IEnumerable<BatchItemInfoModel<TEntity>>, Task> handler, Func<bool>? skipHandle, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_engine);
            ArgumentNullException.ThrowIfNull(handler);

            var result = await _engine.ExecuteWithBatchItemInfoCollectionAsync(handler, skipHandle, cancellationToken);

            return result;
        }

        #endregion

        #region ExecuteWithBatchInfo

        BatcherBuilderResult IBatchExecutionEngineCollection<TEntity>.ExecuteWithBatchInfoCollection([NotNull] Action<BatchInfoModel<TEntity>> handler, Func<bool>? skipHandle)
        {
            ArgumentNullException.ThrowIfNull(_engine);
            ArgumentNullException.ThrowIfNull(handler);

            var result = _engine.ExecuteWithBatchInfoCollection(handler, skipHandle);

            return result;
        }

        async Task<BatcherBuilderResult> IBatchExecutionEngineCollection<TEntity>.ExecuteWithBatchInfoCollectionAsync([NotNull] Func<BatchInfoModel<TEntity>, Task> handler, Func<bool>? skipHandle, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_engine);
            ArgumentNullException.ThrowIfNull(handler);

            var result = await _engine.ExecuteWithBatchInfoCollectionAsync(handler, skipHandle, cancellationToken);

            return result;
        }

        #endregion

        void IBatcherBuilder<TEntity>.WithConfigurations([NotNull] IBatcherConfiguration<TEntity> configuration)
        {
            _configurator = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _printer = new BatchProgressPrinterSerilog<TEntity>(_logger, _configurator);
            _statistics = new BatchStatisticsSerilog<TEntity>(_printer, new());
            _engine = new BatchExecutionEngineSerilog<TEntity>(_semaphore, _configurator, _printer, _statistics);
        }

        void IBatchExecutionEngine<TEntity>.PrintStaticInfo() => _engine.PrintStaticInfo();

        #endregion

        #region Privates

        #endregion

        #endregion
    }
}
